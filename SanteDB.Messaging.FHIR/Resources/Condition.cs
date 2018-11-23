using SanteDB.Messaging.FHIR.DataTypes;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{

    /// <summary>
    /// Represents the condition's clinical status
    /// </summary>
    [XmlType("ConditionClinicalStatus", Namespace = "http://hl7.org/fhir")]
    public enum ConditionClinicalStatus
    {
        [XmlEnum("active")]
        Active,
        [XmlEnum("recurrence")]
        Recurrence,
        [XmlEnum("inactive")]
        Inactive,
        [XmlEnum("remission")]
        Remission,
        [XmlEnum("resolved")]
        Resolved
    }

    /// <summary>
    /// Condition verification status
    /// </summary>
    [XmlType("ConditionVerificationStatus", Namespace = "http://hl7.org/fhir")]
    public enum ConditionVerificationStatus
    {
        [XmlEnum("provisional")]
        Provisional,
        [XmlEnum("differential")]
        Differential,
        [XmlEnum("confirmed")]
        Confirmed,
        [XmlEnum("refuted")]
        Refuted,
        [XmlEnum("entered-in-error")]
        EnteredInError,
        [XmlEnum("unknown")]
        Unknown
    }

    /// <summary>
    /// Represents a condition
    /// </summary>
    [XmlType("Condition", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("Condition", Namespace = "http://hl7.org/fhir")]
    public class Condition : DomainResourceBase
    {

        /// <summary>
        /// Public ctor
        /// </summary>
        public Condition()
        {
            this.Identifier = new List<FhirIdentifier>();
            this.Category = new List<FhirCodeableConcept>();
            this.BodySite = new List<FhirCodeableConcept>();
        }

        /// <summary>
        /// One or more identifiers for the condition
        /// </summary>
        [XmlElement("identifier")]
        [Description("Identifier for the condition")]
        public List<FhirIdentifier> Identifier { get; set; }

        /// <summary>
        /// Gets or set the clinical status of the condition
        /// </summary>
        [XmlElement("clinicalStatus")]
        [Description("The clinical status of the condition")]
        public FhirCode<ConditionClinicalStatus> ClinicalStatus { get; set; }

        /// <summary>
        /// Gets or sets the verification status
        /// </summary>
        [XmlElement("verificationStatus")]
        [Description("The verification status of the object")]
        public FhirCode<ConditionVerificationStatus> VerificationStatus { get; set; }

        /// <summary>
        /// Gets or sets the category
        /// </summary>
        [XmlElement("category")]
        [Description("The categorization of the condition")]
        public List<FhirCodeableConcept> Category { get; set; }

        /// <summary>
        /// Gets or sets the severity
        /// </summary>
        [XmlElement("severity")]
        [Description("The severity of the condition")]
        public FhirCodeableConcept Severity { get; set; }

        /// <summary>
        /// Gets or sets the codified condition itself
        /// </summary>
        [XmlElement("code")]
        [Description("The codified condition")]
        public FhirCodeableConcept Code { get; set; }

        /// <summary>
        /// Gets or sets the body sites where the condition is present
        /// </summary>
        [XmlElement("bodySite")]
        [Description("The body site where the condition is present")]
        public List<FhirCodeableConcept> BodySite { get; set; }

        /// <summary>
        /// Gets or sets the subject
        /// </summary>
        [XmlElement("subject")]
        [Description("The patient who has the condition")]
        public Reference<Patient> Subject { get; set; }

        /// <summary>
        /// Onset date/time
        /// </summary>
        [XmlElement("onsetDateTime", Type = typeof(FhirDateTime))]
        [XmlElement("onsetPeriod", Type = typeof(FhirPeriod))]
        [XmlElement("onsetString", Type = typeof(FhirString))]
        [Description("The time(s) when the condition was active")]
        public object Onset { get; set; }

        /// <summary>
        /// Gets or sets when the condition was resolved
        /// </summary>
        [XmlElement("abatementDateTime", Type = typeof(FhirDateTime))]
        [XmlElement("abatementBoolean", Type = typeof(FhirBoolean))]
        [XmlElement("abatementPeriod", Type = typeof(FhirPeriod))]
        [XmlElement("abatementString", Type = typeof(FhirString))]
        [Description("When the condition was resolved")]
        public object Abatement { get; set; }

        /// <summary>
        /// Gets or sets the date when the condition was asserted
        /// </summary>
        [XmlElement("assertionDate")]
        [Description("The date when the condition was asserted")]
        public FhirDateTime AssertionDate { get; set; }

        /// <summary>
        /// Gets or sets who asserted the reference
        /// </summary>
        [XmlElement("asserter")]
        [Description("Who asserted the condition")]
        public Reference Asserter { get; set; }

    }
}