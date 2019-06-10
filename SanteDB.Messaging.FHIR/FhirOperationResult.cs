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
using MARC.Everest.Connectors;
using SanteDB.Core.Services;
using SanteDB.Messaging.FHIR.Resources;
using System.Collections.Generic;

namespace SanteDB.Messaging.FHIR
{
    /// <summary>
    /// Represents the outcome of a FHIR operation
    /// </summary>
    public class FhirOperationResult
    {

        /// <summary>
        /// Constructor
        /// </summary>
        public FhirOperationResult()
        {
            this.Details = new List<IResultDetail>();
            this.Results = new List<ResourceBase>();
        }

        /// <summary>
        /// Gets the overall outcome of the operation
        /// </summary>
        public ResultCode Outcome { get; set; }

        /// <summary>
        /// Represents the results of the operation
        /// </summary>
        public List<ResourceBase> Results { get; set; }

        /// <summary>
        /// Gets the list of details
        /// </summary>
        public List<IResultDetail> Details { get; set; }


    }
}
