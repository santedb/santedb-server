using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Messaging.HAPI.TransportProtocol;
using NHapi.Base.Model;
using NHapi.Model.V25.Message;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Services;
using SanteDB.Messaging.HL7.Segments;

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
        public IMessage AppendQueryResult(IEnumerable results, Expression queryDefinition, IMessage currentResponse, Hl7MessageReceivedEventArgs evt, String matchConfiguration = null)
        {
            var patients = results.OfType<Patient>();
            if (patients.Count() == 0) return currentResponse;
            var retVal = currentResponse as RSP_K21;

            var pidHandler = SegmentHandlers.GetSegmentHandler("PID");
            var pd1Handler = SegmentHandlers.GetSegmentHandler("PD1");
            var nokHandler = SegmentHandlers.GetSegmentHandler("NK1");
            var matchService = ApplicationContext.Current.GetService<IRecordMatchingService>();
            var matchConfigService = ApplicationContext.Current.GetService<IRecordMatchingConfigurationService>();

            // Process results
            foreach (var itm in patients)
            {
                var queryInstance = retVal.GetQUERY_RESPONSE(retVal.QUERY_RESPONSERepetitionsUsed);
                pidHandler.Create(itm, queryInstance);
                pd1Handler.Create(itm, queryInstance);
                nokHandler.Create(itm, queryInstance);

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
