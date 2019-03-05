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
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Identity reference type
    /// </summary>
    [XmlType("IdRef", Namespace = "http://hl7.org/fhir")]
    public class IdRef 
    {

        /// <summary>
        /// The value of the IDRef
        /// </summary>
        [XmlAttribute("value")]
        public String Value { get; set; }

        /// <summary>
        /// Resolve reference
        /// </summary>
        public FhirElement ResolveReference(FhirElement context)
        {
            return new FhirElement() { IdRef = this.Value }.ResolveReference(context);
        }
    }
}
