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