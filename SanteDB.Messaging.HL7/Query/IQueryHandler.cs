/*
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
using NHapi.Model.V25.Segment;
using SanteDB.Core.Model.Query;
using SanteDB.Messaging.HL7.ParameterMap;
using SanteDB.Messaging.HL7.TransportProtocol;
using System;
using System.Collections;
using System.Linq.Expressions;

namespace SanteDB.Messaging.HL7.Query
{
    /// <summary>
    /// Represents a query result handler
    /// </summary>
    public interface IQueryHandler
    {

        /// <summary>
        /// Append query results for the specified response
        /// </summary>
        /// <param name="results"></param>
        /// <param name="queryDefinition"></param>
        /// <param name="currentResponse"></param>
        /// <param name="evt"></param>
        /// <returns></returns>
        IMessage AppendQueryResult(IEnumerable results, Expression queryDefinition, IMessage currentResponse, Hl7MessageReceivedEventArgs evt, int offset = 0);

        /// <summary>
        /// Rewrite the specified query
        /// </summary>
        NameValueCollection ParseQuery(QPD qpd, Hl7QueryParameterType map);


    }
}
