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
using SanteDB.Messaging.FHIR.Resources;
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
        public List<ResourceBase> Results { get; set; }

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
