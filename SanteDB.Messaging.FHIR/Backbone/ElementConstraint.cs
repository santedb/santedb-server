using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Constraint severity value set
    /// </summary>
    [XmlType("ConstraintSeverity")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/constraint-severity")]
    public enum ConstraintSeverity
    {
        [XmlEnum("error")]
        Error,
        [XmlEnum("warning")]
        Warning
    }

    /// <summary>
    /// Element constraint dictates how the element is constrained
    /// </summary>
    [XmlType("ElementConstraint", Namespace = "http://hl7.org/fhir")]
    public class ElementConstraint : FhirElement
    {
        /// <summary>
        /// Target condition reference
        /// </summary>
        [XmlElement("key")]
        [Description("Target condition reference")]
        [FhirElement(MinOccurs = 1)]
        public FhirId Key { get; set; }

        /// <summary>
        /// Why this constraint is necessary or appropriate
        /// </summary>
        [XmlElement("requirements")]
        [Description("Why this constraint is necessary")]
        public FhirString Requirements { get; set; }

        /// <summary>
        /// Gets or sets the severity of constraint
        /// </summary>
        [XmlElement("severity")]
        [Description("Severity of the constraint")]
        [FhirElement(MinOccurs = 1)]
        public FhirCode<ConstraintSeverity> Severity { get; set; }

        /// <summary>
        /// Gets or sets human description of constraint
        /// </summary>
        [XmlElement("human")]
        [Description("Human description of constraint")]
        [FhirElement(MinOccurs = 1)]
        public FhirString Human { get; set; }

        /// <summary>
        /// XPath expression of constraint
        /// </summary>
        [XmlElement("xpath")]
        [Description("Xpath expression of constraint")]
        [FhirElement(MinOccurs = 1)]
        public FhirString Xpath { get; set; }
    }
}