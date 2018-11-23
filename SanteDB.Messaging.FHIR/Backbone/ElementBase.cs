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
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Element base identifies the base of an element
    /// </summary>
    [XmlType("ElementBase", Namespace = "http://hl7.org/fhir")]
    public class ElementBase : FhirElement
    {
        /// <summary>
        /// Path that identifies the base element
        /// </summary>
        [XmlElement("path")]
        [Description("Path that identifies the base element")]
        [FhirElement(MinOccurs = 1)]
        public FhirString Path { get; set; }

        /// <summary>
        /// Minimum cardinality of the base element
        /// </summary>
        [XmlElement("min")]
        [Description("Min cardinality of the base element")]
        [FhirElement(MinOccurs = 1)]
        public FhirInt Min { get; set; }

        /// <summary>
        /// Maximum cardinality of the base element
        /// </summary>
        [XmlElement("max")]
        [Description("Max cardinality of the base element")]
        [FhirElement(MinOccurs = 1)]
        public FhirString Max { get; set; }
    }
}