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
 * Date: 2018-10-14
 */
using NHapi.Base.Model;
using NHapi.Base.Util;
using NHapi.Model.V25.Datatype;
using NHapi.Model.V25.Message;
using NHapi.Model.V25.Segment;
using SanteDB.Core;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Messaging.HL7.ParameterMap;
using SanteDB.Messaging.HL7.Segments;
using SanteDB.Messaging.HL7.TransportProtocol;
using SanteDB.Messaging.HL7.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using SanteDB.Core.Model;
using SanteDB.Messaging.HL7.Configuration;

namespace SanteDB.Messaging.HL7.Query
{
    /// <summary>
    /// Query result handler
    /// </summary>
    public class CrossReferenceQueryHandler : IQueryHandler
    {
        // Get configuration
        private Hl7ConfigurationSection m_configuration = ApplicationServiceContext.Current?.GetService<IConfigurationManager>().GetSection<Hl7ConfigurationSection>();

        /// <summary>
        /// Append query results to the message
        /// </summary>
        public IMessage AppendQueryResult(IEnumerable results, Expression queryDefinition, IMessage currentResponse, Hl7MessageReceivedEventArgs evt, String matchConfiguration = null, int offset = 0)
        {
            var patients = results.OfType<Patient>();
            if (patients.Count() == 0) return currentResponse;
            var retVal = currentResponse as RSP_K23;
            var rqo = evt.Message as QBP_Q21;

            // Return domains
            List<String> returnDomains = new List<String>();
            foreach (var rt in rqo.QPD.GetField(4).OfType<Varies>())
            {
                var rid = new CX(rqo.Message);
                DeepCopy.copy(rt.Data as GenericComposite, rid);
                returnDomains.Add(rid.AssigningAuthority.NamespaceID.Value);
            }

            var matchService = ApplicationServiceContext.Current.GetService<IRecordMatchingService>();
            var matchConfigService = ApplicationServiceContext.Current.GetService<IRecordMatchingConfigurationService>();

            // Process results
            int i = offset + 1;
            foreach (var itm in patients)
            {
                var queryInstance = retVal.QUERY_RESPONSE;

                // No match?


                foreach(var id in itm.LoadCollection<EntityIdentifier>("Identifiers"))
                    if (returnDomains.Any(o=>o == id.LoadProperty<AssigningAuthority>("Authority").DomainName || returnDomains.Count == 0))
                        queryInstance.PID.GetPatientIdentifierList(queryInstance.PID.PatientIdentifierListRepetitionsUsed).FromModel(id);

                if (returnDomains.Any(rid => rid == this.m_configuration.LocalAuthority.DomainName))
                {
                    int idx = queryInstance.PID.PatientAddressRepetitionsUsed;
                    queryInstance.PID.GetPatientIdentifierList(idx).IDNumber.Value = itm.Key.Value.ToString();
                    queryInstance.PID.GetPatientIdentifierList(idx).AssigningAuthority.NamespaceID.Value = this.m_configuration.LocalAuthority.DomainName;
                    queryInstance.PID.GetPatientIdentifierList(idx).AssigningAuthority.UniversalID.Value = this.m_configuration.LocalAuthority.Oid;
                    queryInstance.PID.GetPatientIdentifierList(idx).AssigningAuthority.UniversalIDType.Value = "ISO";
                }

                if (queryInstance.PID.AlternatePatientIDPIDRepetitionsUsed > 0)
                {
                    queryInstance.PID.SetIDPID.Value = (i++).ToString();
                    queryInstance.PID.GetPatientName(0).NameTypeCode.Value = "S";
                }
                else
                {
                    (currentResponse.GetStructure("QAK") as QAK).HitCount.Value = "0";
                    (currentResponse.GetStructure("QAK") as QAK).HitsRemaining.Value = "0";
                    (currentResponse.GetStructure("QAK") as QAK).ThisPayload.Value = "0";
                    (currentResponse.GetStructure("QAK") as QAK).QueryResponseStatus.Value = "NF";
                }

            }


            return retVal;
        }

        /// <summary>
        /// Rewrite a QPD query to an HDSI query
        /// </summary>
        public NameValueCollection ParseQuery(QPD qpd, Hl7QueryParameterType map)
        {
            NameValueCollection retVal = new NameValueCollection();

            // Query domains
            foreach (var rt in qpd.GetField(3).OfType<Varies>())
            {
                var rid = new CX(qpd.Message);
                DeepCopy.copy(rt.Data as GenericComposite, rid);

                if (String.IsNullOrEmpty(rid.AssigningAuthority.NamespaceID.Value)) // lookup by AA 
                {
                    var aa = ApplicationServiceContext.Current.GetService<IDataPersistenceService<AssigningAuthority>>().Query(o => o.Oid == rid.AssigningAuthority.UniversalID.Value, AuthenticationContext.SystemPrincipal).FirstOrDefault();
                    if (aa == null)
                        throw new InvalidOperationException($"Domain {rid.AssigningAuthority.UniversalID.Value} is unknown");
                    else
                        retVal.Add($"identifier[{aa.DomainName}].value", rid.IDNumber.Value);
                }
                else
                    retVal.Add($"identifier[{rid.AssigningAuthority.NamespaceID.Value}].value", rid.IDNumber.Value);
            }


            // Return domains
            foreach (var rt in qpd.GetField(4).OfType<Varies>())
            {
                var rid = new CX(qpd.Message);
                DeepCopy.copy(rt.Data as GenericComposite, rid);

                if (rid.AssigningAuthority.NamespaceID.Value == this.m_configuration.LocalAuthority.DomainName ||
                    rid.AssigningAuthority.UniversalID.Value == this.m_configuration.LocalAuthority.Oid)
                    continue; 
                if (String.IsNullOrEmpty(rid.AssigningAuthority.NamespaceID.Value)) // lookup by AA 
                {
                    var aa = ApplicationServiceContext.Current.GetService<IDataPersistenceService<AssigningAuthority>>().Query(o => o.Oid == rid.AssigningAuthority.UniversalID.Value, AuthenticationContext.SystemPrincipal).FirstOrDefault();
                    if (aa == null)
                        throw new InvalidOperationException($"Domain {rid.AssigningAuthority.UniversalID.Value} is unknown");
                    else
                        retVal.Add($"identifier[{aa.DomainName}]", "!null");
                }
                else
                    retVal.Add($"identifier[{rid.AssigningAuthority.NamespaceID.Value}]", "!null");
            }


            return retVal;
        }
    }
}
