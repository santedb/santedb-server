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
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// A measured amount of data
    /// </summary>
    [XmlType("Quantity", Namespace = "http://hl7.org/fhir")]
    public class FhirQuantity : FhirElement
    {
        /// <summary>
        /// Gets or sets the primitive value of the quantity
        /// </summary>
        [XmlElement("value")]
        public FhirDecimal Value { get; set; }

        /// <summary>
        /// Gets or sets the relationship of the stated value and the real value
        /// </summary>
        [XmlElement("comparator")]
        public FhirCode<String> Comparator { get; set; }

        /// <summary>
        /// Gets or sets the units of measure
        /// </summary>
        [XmlElement("unit")]
        public FhirString Units { get; set; }

        /// <summary>
        /// Gets or sets the system of the units of measure
        /// </summary>
        [XmlElement("system")]
        public FhirUri System { get; set; }

        /// <summary>
        /// Gets or sets the code
        /// </summary>
        [XmlElement("code")]
        public FhirCode<String> Code { get; set; }

    }
}
