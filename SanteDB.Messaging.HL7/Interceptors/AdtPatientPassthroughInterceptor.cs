/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: justin
 * Date: 2018-12-6
 */
using SanteDB.Core;
using SanteDB.Core.Event;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Services;
using SanteDB.Messaging.HL7.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;
using NHapi.Base.Model;
using NHapi.Model.V25.Message;
using SanteDB.Messaging.HL7.Utils;
using SanteDB.Messaging.HL7.Segments;
using System.Diagnostics;
using NHapi.Base.Parser;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Constants;
using NHapi.Model.V25.Segment;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Security;
using NHapi.Base;
using System.Reflection;
using System.IO;
using System.Xml.Serialization;
using SanteDB.Messaging.HL7.ParameterMap;
using SanteDB.Core.Model.Query;
using NHapi.Base.Util;

namespace SanteDB.Messaging.HL7.Interceptors
{
    /// <summary>
    /// Represents an interceptor that intercepts patient registration events 
    /// however prevents further processing
    /// </summary>
    public class AdtPatientPassthroughInterceptor : InterceptorBase
    {

        // Tracer
        private TraceSource m_tracer = new TraceSource(Hl7Constants.TraceSourceName);

        // Coniguration
        private Hl7ConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<Hl7ConfigurationSection>();
        
        // Loaded query parameter map
        private static Hl7QueryParameterMap s_map;

        /// <summary>
        /// Open the mapping
        /// </summary>
        static AdtPatientPassthroughInterceptor()
        {
            OpenMapping(typeof(AdtPatientPassthroughInterceptor).Assembly.GetManifestResourceStream("SanteDB.Messaging.HL7.ParameterMap.xml"));

            if (!String.IsNullOrEmpty(Assembly.GetEntryAssembly()?.Location))
            {
                var externMap = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "ParameterMap.Hl7.xml");

                if (File.Exists(externMap))
                    using (var s = File.OpenRead(externMap))
                        OpenMapping(s);
            }
        }

        /// <summary>
        /// Open the specified mapping
        /// </summary>
        private static void OpenMapping(Stream stream)
        {
            XmlSerializer xsz = new XmlSerializer(typeof(Hl7QueryParameterMap));

            if (s_map == null)
                s_map = xsz.Deserialize(stream) as Hl7QueryParameterMap;
            else
            {
                // Merge
                var map = xsz.Deserialize(stream) as Hl7QueryParameterMap;
                s_map.Merge(map);
            }

        }

        /// <summary>
        /// Represents the ADT patient registration
        /// </summary>
        public AdtPatientPassthroughInterceptor(Hl7InterceptorConfigurationElement configuration) : base(configuration)
        {

        }

        /// <summary>
        /// Attach to the patient objects
        /// </summary>
        public override void Attach()
        {
            ApplicationServiceContext.Current.Started += (o, e) =>
            {
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Inserting += AdtPatientRegistrationInterceptor_Behavior;
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Updating += AdtPatientRegistrationInterceptor_Behavior;
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Obsoleting += AdtPatientRegistrationInterceptor_Behavior;
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Retrieving += AdtPatientPassthroughInterceptor_Retrieving;
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Querying += AdtPatientPassthroughInterceptor_Querying; ;

                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Bundle>>().Inserting += AdtPatientRegistrationInterceptor_Bundle;
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Bundle>>().Updating += AdtPatientRegistrationInterceptor_Bundle;
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Bundle>>().Obsoleting += AdtPatientRegistrationInterceptor_Bundle;
            };
        }

        /// <summary>
        /// Interceptor for querying
        /// </summary>
        private void AdtPatientPassthroughInterceptor_Querying(object sender, QueryRequestEventArgs<Patient> e)
        {
            e.Cancel = true;
            var parmMap = s_map.Map.FirstOrDefault(o => o.Trigger == "Q22");
            var nvc = QueryExpressionBuilder.BuildQuery(e.Query);

            // Map reverse
            List<KeyValuePair<Hl7QueryParameterMapProperty, object>> parameters = new List<KeyValuePair<Hl7QueryParameterMapProperty, object>>();
            foreach(var kv in nvc)
            {
                var rmap = parmMap.Parameters.Find(o => o.ModelName == kv.Key);
                if (rmap == null)
                    continue;
                else
                    parameters.Add(new KeyValuePair<Hl7QueryParameterMapProperty, object>(rmap, kv.Value));
            }

            if (parameters.Count == 0)
                parameters.Add(new KeyValuePair<Hl7QueryParameterMapProperty, object>(parmMap.Parameters.FirstOrDefault(o=>o.Hl7Name=="@PID.33"), DateTime.MinValue));

            // Construct the basic QBP_Q22
            QBP_Q21 queryRequest = new QBP_Q21();
            var endpoint = this.Configuration.Endpoints.First();
            queryRequest.MSH.SetDefault(endpoint.ReceivingDevice, endpoint.ReceivingFacility, endpoint.SecurityToken);
            queryRequest.MSH.MessageType.MessageStructure.Value = "QBP_Q21";
            queryRequest.MSH.MessageType.TriggerEvent.Value = "Q22";
            queryRequest.MSH.MessageType.MessageCode.Value = "QBP";

            queryRequest.GetSFT(0).SetDefault();
            queryRequest.RCP.QuantityLimitedRequest.Units.Identifier.Value = "RD";
            queryRequest.RCP.QuantityLimitedRequest.Quantity.Value = (e.Count ?? 100).ToString();
            queryRequest.QPD.MessageQueryName.Identifier.Value = "Q22";
            queryRequest.QPD.MessageQueryName.Text.Value = "Find Candidates";
            queryRequest.QPD.MessageQueryName.NameOfCodingSystem.Value = "HL7";

            Terser tser = new Terser(queryRequest);

            int q = 0;
            foreach(var qp in parameters)
            {
                List<String> filter = qp.Value as List<String> ?? new List<String>() { qp.Value.ToString() };
                foreach(var val in filter)
                {
                    
                    Terser.Set(queryRequest.QPD, 3, q, 1, 1, qp.Key.Hl7Name);
                    string dval = val;
                    while (new String[] { "<", ">", "!", "=", "~" }.Any(o => dval.StartsWith(o)))
                        dval = dval.Substring(1);

                    switch(qp.Key.ParameterType)
                    {
                        case "date":
                            var dt = DateTime.Parse(dval);
                            switch(dval.Length)
                            {
                                case 4:
                                    Terser.Set(queryRequest.QPD, 3, q, 2, 1, dt.Year.ToString());
                                    break;
                                case 7:
                                    Terser.Set(queryRequest.QPD, 3, q, 2, 1, dt.ToString("yyyyMM"));
                                    break;
                                case 10:
                                    Terser.Set(queryRequest.QPD, 3, q, 2, 1, dt.ToString("yyyyMMdd"));
                                    break;
                                default:
                                    Terser.Set(queryRequest.QPD, 3, q, 2, 1, dt.ToString("yyyyMMddHHmmss.fffzzzz").Replace(":",""));
                                    break;
                            }
                            break;
                        default:
                            Terser.Set(queryRequest.QPD, 3, q, 2, 1, dval);
                            break;
                    }
                    q++;
                }
            }

            // TODO: Send the query and then maps results
            try
            {
                RSP_K21 response = endpoint.GetSender().SendAndReceive(queryRequest) as RSP_K21;
                // Iterate and create responses
                e.TotalResults = Int32.Parse(response.QAK.HitCount.Value ?? response.QUERY_RESPONSERepetitionsUsed.ToString());
                List<Patient> overr = new List<Patient>();
                // Query response
                for(int i = 0; i < response.QUERY_RESPONSERepetitionsUsed; i++)
                {
                    var ar = response.GetQUERY_RESPONSE(i);
                    // Create patient
                    Bundle patientData = MessageUtils.Parse(ar);
                    patientData.Reconstitute();

                    // Now we extract the patient
                    var pat = patientData.Item.OfType<Patient>().First();
                    pat.VersionKey = pat.Key;
                    overr.Add(pat);

                }
                e.Results = overr;
            }
            catch (Exception ex)
            {
                this.m_tracer.TraceEvent(TraceEventType.Error, ex.HResult, "Error dispatching HL7 query {0}", ex);
            }
        }

        /// <summary>
        /// Interceptor for retrieving
        /// </summary>
        private void AdtPatientPassthroughInterceptor_Retrieving(object sender, DataRetrievingEventArgs<Patient> e)
        {
            e.Cancel = true;
            var qryParms = new QueryRequestEventArgs<Patient>(o => o.Key == e.Id.Value, 0, 1, Guid.NewGuid(), e.Principal);
            AdtPatientPassthroughInterceptor_Querying(sender, qryParms);
            e.Result = qryParms.Results.FirstOrDefault();
        }

        /// <summary>
        /// Represents the bundle operation
        /// </summary>
        protected void AdtPatientRegistrationInterceptor_Bundle(object sender, DataPersistingEventArgs<Bundle> e)
        {
            foreach (var itm in e.Data.Item.OfType<Patient>())
                AdtPatientRegistrationInterceptor_Behavior(sender, new DataPersistingEventArgs<Patient>(itm, e.Principal));
            e.Cancel = true;
        }

        /// <summary>
        /// Represents when the ADT registration occurs
        /// </summary>
        protected void AdtPatientRegistrationInterceptor_Behavior(object sender, DataPersistingEventArgs<Patient> e)
        {
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);

            e.Cancel = true;

            Patient pat = e.Data;

            // Perform notification
            IMessage notificationMessage;
            IGroup patientGroup;

            if (pat.PreviousVersionKey == null)
            {
                // Set the tag value and send an ADMIT
                patientGroup = notificationMessage = new ADT_A01();
                (notificationMessage.GetStructure("MSH") as MSH).MessageType.TriggerEvent.Value = "A04";
                (notificationMessage.GetStructure("MSH") as MSH).MessageType.MessageStructure.Value = "ADT_A01";
            }
            else if (pat.Relationships.Any(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Replaces && o.EffectiveVersionSequenceId == pat.VersionSequence))
            {
                // Set the tag value and send an ADMIT
                notificationMessage = new ADT_A39();
                patientGroup = (notificationMessage as ADT_A39).GetPATIENT();
                (notificationMessage.GetStructure("MSH") as MSH).MessageType.TriggerEvent.Value = "A40";
                (notificationMessage.GetStructure("MSH") as MSH).MessageType.MessageStructure.Value = "ADT_A40";

                foreach (var mrg in pat.Relationships.Where(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Replaces && o.EffectiveVersionSequenceId == pat.VersionSequence))
                {
                    var seg = patientGroup.GetStructure("MRG", patientGroup.GetAll("MRG").Length) as MRG;

                    if (this.Configuration.ExportDomains.Contains(this.m_configuration.LocalAuthority.DomainName))
                    {
                        var key = seg.PriorAlternatePatientIDRepetitionsUsed;
                        seg.GetPriorAlternatePatientID(key).IDNumber.Value = mrg.TargetEntityKey.Value.ToString();
                        seg.GetPriorAlternatePatientID(key).IdentifierTypeCode.Value = "PI";
                        seg.GetPriorAlternatePatientID(key).AssigningAuthority.NamespaceID.Value = this.m_configuration.LocalAuthority.DomainName;
                        seg.GetPriorAlternatePatientID(key).AssigningAuthority.UniversalID.Value = this.m_configuration.LocalAuthority.Oid;
                        seg.GetPriorAlternatePatientID(key).AssigningAuthority.UniversalIDType.Value = "ISO";
                    }

                    // Alternate identifiers
                    foreach (var extrn in pat.Identifiers)
                    {
                        var key = seg.PriorAlternatePatientIDRepetitionsUsed;
                        if (this.Configuration.ExportDomains.Contains(extrn.LoadProperty<AssigningAuthority>("Authority").DomainName))
                        {
                            seg.GetPriorAlternatePatientID(key).IDNumber.Value = extrn.Value;
                            seg.GetPriorAlternatePatientID(key).IdentifierTypeCode.Value = "PT";
                            seg.GetPriorAlternatePatientID(key).AssigningAuthority.NamespaceID.Value = extrn.LoadProperty<AssigningAuthority>("Authority")?.DomainName;
                            seg.GetPriorAlternatePatientID(key).AssigningAuthority.UniversalID.Value = extrn.LoadProperty<AssigningAuthority>("Authority")?.Oid;
                            seg.GetPriorAlternatePatientID(key).AssigningAuthority.UniversalIDType.Value = "ISO";
                        }
                    }
                }
            }
            else
            {
                // Set the tag value and send an ADMIT
                patientGroup = notificationMessage = new ADT_A01();
                (notificationMessage.GetStructure("MSH") as MSH).MessageType.TriggerEvent.Value = "A08";
                (notificationMessage.GetStructure("MSH") as MSH).MessageType.MessageStructure.Value = "ADT_A08";
            }

            if (!String.IsNullOrEmpty(this.Configuration.Version))
            {
                (notificationMessage.GetStructure("MSH") as MSH).VersionID.VersionID.Value = this.Configuration.Version;
            }

            // Add SFT
            if (new Version(this.Configuration.Version ?? "2.5") >= new Version(2, 4))
                (notificationMessage.GetStructure("SFT", 0) as SFT).SetDefault();

            // Create the PID segment
            SegmentHandlers.GetSegmentHandler("PID").Create(e.Data, patientGroup, this.Configuration.ExportDomains.ToArray());
            SegmentHandlers.GetSegmentHandler("PD1").Create(e.Data, patientGroup, this.Configuration.ExportDomains.ToArray());
            SegmentHandlers.GetSegmentHandler("NK1").Create(e.Data, patientGroup, this.Configuration.ExportDomains.ToArray());
            //SegmentHandlers.GetSegmentHandler("EVN").Create(e.Data, patientGroup, this.Configuration.ExportDomains.ToArray());

            foreach (var itm in this.Configuration.Endpoints)
            {
                try
                {
                    // TODO: Create an HL7 Queue
                    (notificationMessage.GetStructure("MSH") as MSH).SetDefault(itm.ReceivingDevice, itm.ReceivingFacility, itm.SecurityToken);
                    var response = itm.GetSender().SendAndReceive(notificationMessage);

                    if (!(response.GetStructure("MSA") as MSA).AcknowledgmentCode.Value.EndsWith("A"))
                        throw new HL7Exception("Remote server rejected message");
                }
                catch (Exception ex)
                {
                    this.m_tracer.TraceEvent(TraceEventType.Error, ex.HResult, "Error dispatching message {0} to {1}: {2} \r\n {3}", pat, itm.Address, ex, new PipeParser().Encode(notificationMessage));
                }
            }

        }

        /// <summary>
        /// Detacth
        /// </summary>
        public override void Detach()
        {
            ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Inserting -= AdtPatientRegistrationInterceptor_Behavior;
            ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Updating -= AdtPatientRegistrationInterceptor_Behavior;
            ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Obsoleting -= AdtPatientRegistrationInterceptor_Behavior;
        }
    }
}
