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
using SanteDB.Messaging.HL7.Configuration;
using SanteDB.Messaging.HL7.Exceptions;
using SanteDB.Messaging.HL7.ParameterMap;
using SanteDB.Messaging.HL7.Segments;
using SanteDB.Messaging.HL7.TransportProtocol;
using SanteDB.Messaging.HL7.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Messaging.HL7.Query
{
    /// <summary>
    /// Query result handler
    /// </summary>
    public class FindCandidatesQueryHandler : IQueryHandler
    {

        // Configuration
        private Hl7ConfigurationSection m_configuration = ApplicationServiceContext.Current?.GetService<IConfigurationManager>().GetSection<Hl7ConfigurationSection>();

        /// <summary>
        /// Append query results to the message
        /// </summary>
        public virtual IMessage AppendQueryResult(IEnumerable results, Expression queryDefinition, IMessage currentResponse, Hl7MessageReceivedEventArgs evt, String matchConfiguration = null, int offset = 0)
        {
            var patients = results.OfType<Patient>();
            if (patients.Count() == 0) return currentResponse;
            var retVal = currentResponse as RSP_K21;

            var pidHandler = SegmentHandlers.GetSegmentHandler("PID");
            var pd1Handler = SegmentHandlers.GetSegmentHandler("PD1");
            var nokHandler = SegmentHandlers.GetSegmentHandler("NK1");
            var matchService = ApplicationServiceContext.Current.GetService<IRecordMatchingService>();
            var matchConfigService = ApplicationServiceContext.Current.GetService<IRecordMatchingConfigurationService>();

            // Return domains
            var rqo = evt.Message as QBP_Q21;
            List<AssigningAuthority> returnDomains = new List<AssigningAuthority>();
            foreach (var rt in rqo.QPD.GetField(8).OfType<Varies>())
            {
                var rid = new CX(rqo.Message);
                DeepCopy.copy(rt.Data as GenericComposite, rid);
                var authority = rid.AssigningAuthority.ToModel();
                returnDomains.Add(authority);
            }
            if (returnDomains.Count == 0)
                returnDomains = null;
            
            // Process results
            int i = offset + 1;
            foreach (var itm in patients)
            {
                var queryInstance = retVal.GetQUERY_RESPONSE(retVal.QUERY_RESPONSERepetitionsUsed);
                pidHandler.Create(itm, queryInstance, returnDomains?.ToArray());
                pd1Handler.Create(itm, queryInstance, null);
                nokHandler.Create(itm, queryInstance, null);
                queryInstance.PID.SetIDPID.Value = (i++).ToString();
                // QRI?
                if(matchService != null && !String.IsNullOrEmpty(matchConfiguration))
                {
                    var score = matchService.Score<Patient>(itm, queryDefinition as Expression<Func<Patient, bool>>, matchConfiguration);
                    queryInstance.QRI.CandidateConfidence.Value = score.Score.ToString();
                    queryInstance.QRI.AlgorithmDescriptor.Identifier.Value = matchConfiguration;
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

            // Control of strength
            String strStrength = (qpd.GetField(4, 0) as Varies)?.Data.ToString(),
                algorithm = (qpd.GetField(5, 0) as Varies)?.Data.ToString();
            Double? strength = String.IsNullOrEmpty(strStrength) ? null : (double?)Double.Parse(strStrength);

            // Query parameters
            foreach (var itm in MessageUtils.ParseQueryElement(qpd.GetField(3).OfType<Varies>(), map, algorithm, strength))
                try
                {
                    retVal.Add(itm.Key, itm.Value);
                }
                catch(Exception e)
                {
                    throw new HL7ProcessingException("Error processing query parameter", "QPD", "1", 3, 0, e);
                }

            // Return domains
            foreach (var rt in qpd.GetField(8).OfType<Varies>())
            {
                try
                {
                    var rid = new CX(qpd.Message);
                    DeepCopy.copy(rt.Data as GenericComposite, rid);
                    var authority = rid.AssigningAuthority.ToModel();

                    if (authority.Key == this.m_configuration.LocalAuthority.Key)
                        retVal.Add("_id", rid.IDNumber.Value);
                    else 
                        retVal.Add($"identifier[{authority.DomainName}]", "!null");
                }
                catch(Exception e)
                {
                    throw new HL7ProcessingException("Error processing return domains", "QPD", "1", 8, 0, e);
                }
            }


            return retVal;
        }
    }
}
