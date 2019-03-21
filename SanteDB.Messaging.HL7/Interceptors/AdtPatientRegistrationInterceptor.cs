/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
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
using SanteDB.Core.Diagnostics;
using System.Diagnostics.Tracing;

namespace SanteDB.Messaging.HL7.Interceptors
{
    /// <summary>
    /// Represents an interceptor that intercepts patient registration events 
    /// </summary>
    public class AdtPatientRegistrationInterceptor : InterceptorBase
    {

        // Tracer
        private Tracer m_tracer = new Tracer(Hl7Constants.TraceSourceName);

        // Coniguration
        private Hl7ConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<Hl7ConfigurationSection>();


        /// <summary>
        /// Export tag name
        /// </summary>
        public const String TagName = "hl7.adt.exp";

        /// <summary>
        /// Represents the ADT patient registration
        /// </summary>
        public AdtPatientRegistrationInterceptor(Hl7InterceptorConfigurationElement configuration) : base(configuration)
        {

        }

        /// <summary>
        /// Attach to the patient objects
        /// </summary>
        public override void Attach()
        {
            ApplicationServiceContext.Current.Started += (o, e) => {
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Inserted += AdtPatientRegistrationInterceptor_Behavior;
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Updated += AdtPatientRegistrationInterceptor_Behavior;
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Obsoleted += AdtPatientRegistrationInterceptor_Behavior;
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Bundle>>().Inserted += AdtPatientRegistrationInterceptor_Bundle;
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Bundle>>().Updated += AdtPatientRegistrationInterceptor_Bundle;
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Bundle>>().Obsoleted += AdtPatientRegistrationInterceptor_Bundle;

            };
        }

        /// <summary>
        /// Represents the bundle operation
        /// </summary>
        protected void AdtPatientRegistrationInterceptor_Bundle(object sender, DataPersistedEventArgs<Bundle> e)
        {
            foreach (var itm in e.Data.Item.Where(o => !e.Data.ExpansionKeys.Contains(o.Key.Value)).OfType<Patient>())
                AdtPatientRegistrationInterceptor_Behavior(sender, new DataPersistedEventArgs<Patient>(itm, e.Principal));
        }

        /// <summary>
        /// Represents when the ADT registration occurs
        /// </summary>
        protected void AdtPatientRegistrationInterceptor_Behavior(object sender, DataPersistedEventArgs<Patient> e)
        {
            ApplicationServiceContext.Current.GetService<IThreadPoolService>().QueueNonPooledWorkItem(
                (p) =>
                {
                    AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);

                    Patient pat = p as Patient;
                    // We want to construct an ADT message if (and only if) the guards are met and if the last ADT was not this version
                    var tag = pat.LoadCollection<EntityTag>("Tags").FirstOrDefault(o => o.TagKey == TagName);

                    if (tag?.Value == e.Data.VersionKey.ToString())
                        return; // No need
                    else if (base.ExecuteGuard(pat))
                    {

                        // Perform notification
                        IMessage notificationMessage;
                        IGroup patientGroup;

                        if (tag?.Value == null)
                        {
                            // Set the tag value and send an ADMIT
                            patientGroup = notificationMessage = new ADT_A01();
                            ApplicationServiceContext.Current.GetService<ITagPersistenceService>().Save(pat.Key.Value, new EntityTag(TagName, pat.VersionKey.ToString()));
                            (notificationMessage.GetStructure("MSH") as MSH).MessageType.TriggerEvent.Value = "A04";
                            (notificationMessage.GetStructure("MSH") as MSH).MessageType.MessageStructure.Value = "ADT_A01";
                        }
                        else if (pat.LoadCollection<EntityRelationship>("Relationships").Any(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Replaces && o.EffectiveVersionSequenceId == pat.VersionSequence))
                        {
                            // Set the tag value and send an ADMIT
                            notificationMessage = new ADT_A39();
                            patientGroup = (notificationMessage as ADT_A39).GetPATIENT();
                            ApplicationServiceContext.Current.GetService<ITagPersistenceService>().Save(pat.Key.Value, new EntityTag(TagName, pat.VersionKey.ToString()));
                            (notificationMessage.GetStructure("MSH") as MSH).MessageType.TriggerEvent.Value = "A40";
                            (notificationMessage.GetStructure("MSH") as MSH).MessageType.MessageStructure.Value = "ADT_A40";

                            foreach (var mrg in pat.LoadCollection<EntityRelationship>("Relationships").Where(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Replaces && o.EffectiveVersionSequenceId == pat.VersionSequence))
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
                                foreach(var extrn in pat.LoadCollection<EntityIdentifier>("Identifiers"))
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
                            ApplicationServiceContext.Current.GetService<ITagPersistenceService>().Save(pat.Key.Value, new EntityTag(TagName, pat.VersionKey.ToString()));
                            (notificationMessage.GetStructure("MSH") as MSH).MessageType.TriggerEvent.Value = "A08";
                            (notificationMessage.GetStructure("MSH") as MSH).MessageType.MessageStructure.Value = "ADT_A08";
                        }

                        if (!String.IsNullOrEmpty(this.Configuration.Version))
                        {
                            (notificationMessage.GetStructure("MSH") as MSH).VersionID.VersionID.Value = this.Configuration.Version;
                        }

                        // Add SFT
                        if(new Version(this.Configuration.Version ?? "2.5") >= new Version(2,4))
                            (notificationMessage.GetStructure("SFT",0) as SFT).SetDefault();

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
                                var response = itm.GetSender().SendAndReceive(notificationMessage) ;

                                if (!(response.GetStructure("MSA") as MSA).AcknowledgmentCode.Value.EndsWith("A"))
                                    throw new HL7Exception("Remote server rejected message");
                            }
                            catch (Exception ex)
                            {
                                this.m_tracer.TraceEvent(EventLevel.Error,  "Error dispatching message {0} to {1}: {2} \r\n {3}", pat, itm.Address, ex, new PipeParser().Encode(notificationMessage));
                            }
                        }

                    }

                }, e.Data
            );
        }

        /// <summary>
        /// Detacth
        /// </summary>
        public override void Detach()
        {
            ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Inserted -= AdtPatientRegistrationInterceptor_Behavior ;
            ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Updated -= AdtPatientRegistrationInterceptor_Behavior;
            ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Obsoleted -= AdtPatientRegistrationInterceptor_Behavior;
        }
    }
}
