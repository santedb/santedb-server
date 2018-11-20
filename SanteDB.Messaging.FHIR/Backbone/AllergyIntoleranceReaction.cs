using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{

    /// <summary>
    /// Allergy or intolerance severity
    /// </summary>
    [XmlType("AllergyIntoleranceSeverity", Namespace = "http://hl7.org/fhir")]
    public enum AllergyIntoleranceSeverity
    {
        [XmlEnum("severe")]
        Severe, 
        [XmlEnum("mild")]
        Mild,
        [XmlEnum("moderate")]
        Moderate
    }

    /// <summary>
    /// Allergy or intolerance reaction
    /// </summary>
    [XmlType("AllergyIntoleranceReaction", Namespace = "http://hl7.org/fhir")]
    public class AllergyIntoleranceReaction : BackboneElement
    {

        /// <summary>
        /// Gets or sets the substance code
        /// </summary>
        [XmlElement("substance")]
        [Description("Specific substance which is responsible for the event")]
        public FhirCodeableConcept Substance { get; set; }

        /// <summary>
        /// Manifestation
        /// </summary>
        [XmlElement("manifestation")]
        [Description("How the allergy manifested itself")]
        public List<FhirCodeableConcept> Manifestation { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        [XmlElement("description")]
        [Description("A textual description of the reaction")]
        public FhirString Description { get; set; }

        /// <summary>
        /// Gets or sets the onset date
        /// </summary>
        [XmlElement("onset")]
        [Description("The date of onset")]
        public FhirDateTime Onset { get; set; }

        /// <summary>
        /// The severity of the allergy reaction
        /// </summary>
        [XmlElement("severity")]
        [Description("The severity of the reaction")]
        public FhirCode<AllergyIntoleranceSeverity> Severity { get; set; }

    }
}
