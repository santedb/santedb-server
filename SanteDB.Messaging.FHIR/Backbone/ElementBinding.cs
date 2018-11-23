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