using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.DataTypes;
using System.ComponentModel;
using SanteDB.Messaging.FHIR.Attributes;

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
