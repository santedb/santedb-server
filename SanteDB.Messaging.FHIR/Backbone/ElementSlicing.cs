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
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{

    /// <summary>
    /// Slicing rules
    /// </summary>
    [XmlType("SlicingRules", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/resource-slicing-rules")]
    public enum SlicingRules
    {
        [XmlEnum("closed")]
        Closed,
        [XmlEnum("open")]
        Open,
        [XmlEnum("openAtEnd")]
        OpenAtEnd
    }

    /// <summary>
    /// Identifies the sliced
    /// </summary>
    [XmlType("ElementSlicing", Namespace = "http://hl7.org/fhir")]
    public class ElementSlicing : FhirElement
    {

        /// <summary>
        /// Constructs a new element slice
        /// </summary>
        public ElementSlicing()
        {
            this.Discriminator = new List<FhirString>();
        }

        /// <summary>
        /// Gets or sets element values that are used to distinguish the slices
        /// </summary>
        [XmlElement("discriminator")]
        [Description("Element values that used to distinguis the slices")]
        public List<FhirString> Discriminator { get; set; }

        /// <summary>
        /// Gets or sets the text description of how this slicing works
        /// </summary>
        [XmlElement("description")]
        [Description("Text description of how slicing works")]
        public FhirString Description { get; set; }

        /// <summary>
        /// Gets or sets if elements must be in the same order as slices
        /// </summary>
        [XmlElement("ordered")]
        [Description("If elements must be in same order as slices")]
        public FhirBoolean Ordered { get; set; }

        /// <summary>
        /// Gets or sets the rules
        /// </summary>
        [XmlElement("rules")]
        [Description("Rules for slice")]
        [FhirElement(MinOccurs = 1)]
        public FhirCode<SlicingRules> Rules { get; set; }

    }
}