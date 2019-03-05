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
    /// Used to identify an enumeration's FHIR valueset
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum)]
    public class FhirValueSetAttribute : Attribute
    {
        /// <summary>
        /// The URI to the FHIR valueset
        /// </summary>
        public String Uri { get; set; }
    }
}
