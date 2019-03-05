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
using SanteDB.Messaging.FHIR.Resources;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{

    /// <summary>
    /// Binding strength
    /// </summary>
    [XmlType("BindingStrength", Namespace = "http://hl7.org/fhir")]
    public enum BindingStrength
    {
        [XmlEnum("required")]
        Required,
        [XmlEnum("extensible")]
        Exstensible,
        [XmlEnum("preferred")]
        Preferred,
        [XmlEnum("example")]
        Example
    }

    /// <summary>
    /// Element binding
    /// </summary>
    [XmlType("ElementBinding", Namespace = "http://hl7.org/fhir")]
    public class ElementBinding : FhirElement
    {

        /// <summary>
        /// Gets or sets the strength of the binding
        /// </summary>
        [XmlElement("strength")]
        [Description("Strength of the binding")]
        public FhirCode<BindingStrength> Strength { get; set; }

        /// <summary>
        /// Gets or sets the human explanation of the binding
        /// </summary>
        [XmlElement("description")]
        [Description("Human explanation of the binding")]
        public FhirString Description { get; set; }

        /// <summary>
        /// Gets or sets the value set reference
        /// </summary>
        [XmlElement("valueSetUri", typeof(FhirUri))]
        [XmlElement("valueSetReference", typeof(Reference<ValueSet>))]
        [Description("Value set reference")]
        public FhirElement ValueSet { get; set; }
    }
}