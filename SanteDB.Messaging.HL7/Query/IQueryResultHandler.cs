using MARC.HI.EHRS.SVC.Messaging.HAPI.TransportProtocol;
using NHapi.Base.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.HL7.Query
{
    /// <summary>
    /// Represents a query result handler
    /// </summary>
    public interface IQueryResultHandler
    {

        /// <summary>
        /// Append query results for the specified response
        /// </summary>
        /// <param name="results"></param>
        /// <param name="queryDefinition"></param>
        /// <param name="currentResponse"></param>
        /// <param name="evt"></param>
        /// <returns></returns>
        IMessage AppendQueryResult(IEnumerable results, Expression queryDefinition, IMessage currentResponse, Hl7MessageReceivedEventArgs evt, String scoreConfiguration = null, int offset = 0);

    }
}
