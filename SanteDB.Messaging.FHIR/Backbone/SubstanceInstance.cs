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
using SanteDB.Messaging.FHIR.DataTypes;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Represents a particular instance of a substance
    /// </summary>
    [XmlType("SubstanceInstanec", Namespace = "http://hl7.org/fhir")]
    public class SubstanceInstance : BackboneElement
    {

        /// <summary>
        /// Gets or sets the identifier of the substance instance
        /// </summary>
        [XmlElement("identifier")]
        [Description("Identifier for the package or container")]
        public FhirIdentifier Identifier { get; set; }

        /// <summary>
        /// Gets or sets the expiration time
        /// </summary>
        [XmlElement("expiry")]
        [Description("When no longer valid to use this instance")]
        public FhirDateTime Expiry { get; set; }

        /// <summary>
        /// Gets or sets the quantity
        /// </summary>
        [XmlElement("quantity")]
        [Description("Amount of substance in the package")]
        public FhirQuantity Quantity { get; set; }

    }
}
