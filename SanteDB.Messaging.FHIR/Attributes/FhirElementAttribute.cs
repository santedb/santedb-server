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
            this.MustSupport = true;
            this.IsModifier = false;
        }

        /// <summary>
        /// Gets the type which hosts the attribute
        /// </summary>
        public Type HostType { get; set; }

        /// <summary>
        /// The property being profiled
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// Short description
        /// </summary>
        public string ShortDescription { get; set; }

        /// <summary>
        /// Format definition
        /// </summary>
        public string FormalDefinition { get; set; }

        /// <summary>
        /// Comment
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// True if the implementer must support
        /// </summary>
        public bool MustSupport { get; set; }

        /// <summary>
        /// True if the implementer must understand
        /// </summary>
        public bool IsModifier { get; set; }

        /// <summary>
        /// Identifies the binding (value set)
        /// </summary>
        public Type Binding { get; set; }

        /// <summary>
        /// Identifies the remote binding
        /// </summary>
        public String RemoteBinding { get; set; }

        /// <summary>
        /// Sets the fixed value type
        /// </summary>
        public Type ValueType { get; set; }

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
