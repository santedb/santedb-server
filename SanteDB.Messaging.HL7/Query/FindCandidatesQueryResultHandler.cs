﻿/*
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
using NHapi.Model.V25.Message;
using SanteDB.Core;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Services;
using SanteDB.Messaging.HL7.Segments;
using SanteDB.Messaging.HL7.TransportProtocol;
using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Messaging.HL7.Query
{
    /// <summary>
    /// Query result handler
    /// </summary>
    public class FindCandidatesQueryResultHandler : IQueryResultHandler
    {
        /// <summary>
        /// Append query results to the message
        /// </summary>
        public IMessage AppendQueryResult(IEnumerable results, Expression queryDefinition, IMessage currentResponse, Hl7MessageReceivedEventArgs evt, String matchConfiguration = null, int offset = 0)
        {
            var patients = results.OfType<Patient>();
            if (patients.Count() == 0) return currentResponse;
            var retVal = currentResponse as RSP_K21;

            var pidHandler = SegmentHandlers.GetSegmentHandler("PID");
            var pd1Handler = SegmentHandlers.GetSegmentHandler("PD1");
            var nokHandler = SegmentHandlers.GetSegmentHandler("NK1");
            var matchService = ApplicationServiceContext.Current.GetService<IRecordMatchingService>();
            var matchConfigService = ApplicationServiceContext.Current.GetService<IRecordMatchingConfigurationService>();

            // Process results
            int i = offset + 1;
            foreach (var itm in patients)
            {
                var queryInstance = retVal.GetQUERY_RESPONSE(retVal.QUERY_RESPONSERepetitionsUsed);
                pidHandler.Create(itm, queryInstance);
                pd1Handler.Create(itm, queryInstance);
                nokHandler.Create(itm, queryInstance);
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

    }
}
