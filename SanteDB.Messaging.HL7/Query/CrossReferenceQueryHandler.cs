/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
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
using SanteDB.Messaging.HL7.Exceptions;

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
        public virtual IMessage AppendQueryResult(IEnumerable results, Expression queryDefinition, IMessage currentResponse, Hl7MessageReceivedEventArgs evt, String matchConfiguration = null, int offset = 0)
        {
            var patients = results.OfType<Patient>();
            if (patients.Count() == 0) return currentResponse;
            var retVal = currentResponse as RSP_K23;
            var rqo = evt.Message as QBP_Q21;

            // Return domains
            List<AssigningAuthority> returnDomains = new List<AssigningAuthority>();
            foreach (var rt in rqo.QPD.GetField(4).OfType<Varies>())
            {
                var rid = new CX(rqo.Message);
                DeepCopy.copy(rt.Data as GenericComposite, rid);
                var domain = rid.AssigningAuthority.ToModel();
                returnDomains.Add(domain);
            }

            var matchService = ApplicationServiceContext.Current.GetService<IRecordMatchingService>();
            var matchConfigService = ApplicationServiceContext.Current.GetService<IRecordMatchingConfigurationService>();

            // Process results
            int i = offset + 1;
            foreach (var itm in patients)
            {
                var queryInstance = retVal.QUERY_RESPONSE;

                // Expose PK?
                if (returnDomains.Count == 0 || returnDomains.Any(d => d.Key == this.m_configuration.LocalAuthority.Key))
                {
                    queryInstance.PID.GetPatientIdentifierList(queryInstance.PID.PatientIdentifierListRepetitionsUsed).FromModel(new EntityIdentifier(this.m_configuration.LocalAuthority, itm.Key.ToString()));
                    queryInstance.PID.GetPatientIdentifierList(queryInstance.PID.PatientIdentifierListRepetitionsUsed - 1).IdentifierTypeCode.Value = "PI";
                }

                foreach (var id in itm.LoadCollection<EntityIdentifier>("Identifiers"))
                    if (returnDomains.Count == 0 || returnDomains.Any(o => o.Key == id.AuthorityKey))
                        queryInstance.PID.GetPatientIdentifierList(queryInstance.PID.PatientIdentifierListRepetitionsUsed).FromModel(id);

                if (returnDomains.Any(rid => rid.Key == this.m_configuration.LocalAuthority.Key))
                {
                    int idx = queryInstance.PID.PatientIdentifierListRepetitionsUsed;
                    queryInstance.PID.GetPatientIdentifierList(idx).IDNumber.Value = itm.Key.Value.ToString();
                    queryInstance.PID.GetPatientIdentifierList(idx).AssigningAuthority.NamespaceID.Value = this.m_configuration.LocalAuthority.DomainName;
                    queryInstance.PID.GetPatientIdentifierList(idx).AssigningAuthority.UniversalID.Value = this.m_configuration.LocalAuthority.Oid;
                    queryInstance.PID.GetPatientIdentifierList(idx).AssigningAuthority.UniversalIDType.Value = "ISO";
                }

                // No identifiers found in the response domains
                if (queryInstance.PID.PatientIdentifierListRepetitionsUsed > 0)
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
        public virtual NameValueCollection ParseQuery(QPD qpd, Hl7QueryParameterType map)
        {
            NameValueCollection retVal = new NameValueCollection();

            // Query domains
            foreach (var rt in qpd.GetField(3).OfType<Varies>())
            {
                try
                {
                    var rid = new CX(qpd.Message);
                    DeepCopy.copy(rt.Data as GenericComposite, rid);
                    var authority = rid.AssigningAuthority.ToModel();

                    if (authority.Key == m_configuration.LocalAuthority.Key)
                        retVal.Add("_id", rid.IDNumber.Value);
                    else
                        retVal.Add($"identifier[{authority.DomainName}].value", rid.IDNumber.Value);
                }
                catch (Exception e)
                {
                    throw new HL7ProcessingException("Error processing patient identity", "QPD", "1", 3, 4, e);
                }
            }


            //// Return domains
            // This just verifies the return domains
            foreach (var rt in qpd.GetField(4).OfType<Varies>())
            {
                try
                {
                    var rid = new CX(qpd.Message);
                    DeepCopy.copy(rt.Data as GenericComposite, rid);
                    var authority = rid.AssigningAuthority.ToModel();
                }
                catch (Exception e)
                {
                    throw new HL7ProcessingException("Error processing what domains returned", "QPD", "1", 4, 4, e);
                }
            }


            return retVal;
        }
    }
}
