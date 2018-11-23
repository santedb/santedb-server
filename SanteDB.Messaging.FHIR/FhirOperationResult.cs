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
 * Date: 2018-11-23
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SanteDB.Messaging.FHIR.Resources;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.Issues;

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
            this.Issues = new List<DetectedIssue>();
            this.Results = new List<DomainResourceBase>();
        }

        /// <summary>
        /// Gets the overall outcome of the operation
        /// </summary>
        public ResultCode Outcome { get; set; }

        /// <summary>
        /// Represents the results of the operation
        /// </summary>
        public List<DomainResourceBase> Results { get; set; }

        /// <summary>
        /// Business violations
        /// </summary>
        public List<DetectedIssue> Issues { get; set; }

        /// <summary>
        /// Gets the list of details
        /// </summary>
        public List<IResultDetail> Details { get; set; }


    }
}
