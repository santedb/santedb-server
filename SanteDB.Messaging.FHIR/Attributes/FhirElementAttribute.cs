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
using System;

namespace SanteDB.Messaging.FHIR.Attributes
{
    /// <summary>
    /// Represents a profile on a resource property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true)]
    public class FhirElementAttribute : Attribute
    {

        /// <summary>
        /// Profile attribute
        /// </summary>
        public FhirElementAttribute()
        {
            this.MaxOccurs = 1;
            this.MinOccurs = 0;
        }

        /// <summary>
        /// Comment
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Identifies the remote binding
        /// </summary>
        public String RemoteBinding { get; set; }

        /// <summary>
        /// Min-occurs
        /// </summary>
        public int MinOccurs { get; set; }

        /// <summary>
        /// Max-occurs
        /// </summary>
        public int MaxOccurs { get; set; }

    }
}
