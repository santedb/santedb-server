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
using Hl7.Fhir.Model;
using System.Collections.Generic;

namespace SanteDB.Messaging.FHIR
{
    /// <summary>
    /// Query result form a FHIR query
    /// </summary>
    public class FhirQueryResult
    {

        /// <summary>
        /// Gets or sets the results
        /// </summary>
        public List<Resource> Results { get; set; }

        /// <summary>
        /// Gets or sets the query that initiated the action
        /// </summary>
        public FhirQuery Query { get; set; }

        /// <summary>
        /// Gets the total results
        /// </summary>
        public int TotalResults { get; set; }

    }
}
