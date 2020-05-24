/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Search parameter type
    /// </summary>
    [XmlType("SearchParamType", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/search-param-type")]
    public enum SearchParamType
    {
        [XmlEnum("number")]
        Number,
        [XmlEnum("date")]
        Date,
        [XmlEnum("string")]
        String,
        [XmlEnum("token")]
        Token,
        [XmlEnum("reference")]
        Reference,
        [XmlEnum("composite")]
        Composite,
        [XmlEnum("quantity")]
        Quantity, 
        [XmlEnum("uri")]
        Uri
    }

    /// <summary>
    /// Search modifiers supported
    /// </summary>
    [XmlType("SearchModifierCode", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/search-modifier-code")]
    public enum SearchModifierCode
    {
        [XmlEnum("missing")]
        Missing,
        [XmlEnum("exact")]
        Exact,
        [XmlEnum("contains")]
        Contains,
        [XmlEnum("not")]
        Not,
        [XmlEnum("text")]
        Text,
        [XmlEnum("in")]
        In,
        [XmlEnum("not-in")]
        NotIn,
        [XmlEnum("below")]
        Below,
        [XmlEnum("above")]
        Above,
        [XmlEnum("type")]
        Type
    }

    /// <summary>
    /// Search parameter
    /// </summary>
    [XmlType("SearchParamDefinition", Namespace = "http://hl7.org/fhir")]
    public class SearchParamDefinition : BackboneElement
    {

        /// <summary>
        /// Search parameter definition
        /// </summary>
        public SearchParamDefinition()
        {
            this.Target = new List<FhirCode<string>>();
            this.Modifier = new List<FhirCode<SearchModifierCode>>();
            this.Chain = new List<FhirString>();
        }

        /// <summary>
        /// Gets or sets the name of the search parameter
        /// </summary>
        [XmlElement("name")]
        [Description("The name of the search parameter")]
        [FhirElement(MinOccurs = 1)]
        public FhirString Name { get; set; }

        /// <summary>
        /// Gets or sets the source of the search parameter definition
        /// </summary>
        [XmlElement("definition")]
        [Description("The source of the search parameter definition")]
        public FhirUri Definition { get; set; }

        /// <summary>
        /// Gets or sets the type of the parameter
        /// </summary>
        [XmlElement("type")]
        [Description("The type of the search parameter")]
        [FhirElement(MinOccurs = 1)]
        public FhirCode<SearchParamType> Type { get; set; }

        /// <summary>
        /// Gets or sets the documentation related to the parameter
        /// </summary>
        [XmlElement("documentation")]
        [Description("Contents and meaning of the parameter")]
        [FhirElement (MinOccurs = 1)]
        public FhirString Documentation { get; set; }

        /// <summary>
        /// Gets or sets the target resources
        /// </summary>
        [XmlElement("target")]
        [Description("Types of resource supported (if reference)")]
        public List<FhirCode<String>> Target { get; set; }

        /// <summary>
        /// Gets or sets modifiers supported
        /// </summary>
        [XmlElement("modifier")]
        [Description("Modifiers supported on this search parameter")]
        public List<FhirCode<SearchModifierCode>> Modifier { get; set; }

        /// <summary>
        /// Chain names supported
        /// </summary>
        [XmlElement("chain")]
        [Description("Chain names supported")]
        public List<FhirString> Chain { get; set; }

        /// <summary>
        /// Write textual output of the search parameter
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {
            w.WriteStartElement("tr");
            base.WriteTableCell(w, this.Name);
            base.WriteTableCell(w, this.Type);
            base.WriteTableCell(w, this.Documentation);
            w.WriteEndElement(); // tr
        }

    }
}
