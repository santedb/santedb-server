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
        /// <summary>
        /// The condition is active and represents a danger to the patient
        /// </summary>
        [XmlEnum("active")]
        Active,
        /// <summary>
        /// The condition is recurring
        /// </summary>
        [XmlEnum("recurrence")]
        Recurrence,
        /// <summary>
        /// The condition is inactive
        /// </summary>
        [XmlEnum("inactive")]
        Inactive,
        /// <summary>
        /// The condition is in remission
        /// </summary>
        [XmlEnum("remission")]
        Remission,
        /// <summary>
        /// The condition has been treated
        /// </summary>
        [XmlEnum("resolved")]
        Resolved
    }

    /// <summary>
    /// Condition verification status
    /// </summary>
    [XmlType("ConditionVerificationStatus", Namespace = "http://hl7.org/fhir")]
    public enum ConditionVerificationStatus
    {
        /// <summary>
        /// The condition has been recorded but not yet confirmed
        /// </summary>
        [XmlEnum("provisional")]
        Provisional,
        /// <summary>
        /// The condition is a differential report
        /// </summary>
        [XmlEnum("differential")]
        Differential,
        /// <summary>
        /// The condition has been confirmed accurate
        /// </summary>
        [XmlEnum("confirmed")]
        Confirmed,
        /// <summary>
        /// The condition record has been refuted
        /// </summary>
        [XmlEnum("refuted")]
        Refuted,
        /// <summary>
        /// The condition was entered in error
        /// </summary>
        [XmlEnum("entered-in-error")]
        EnteredInError,
        /// <summary>
        /// The verification status of the condition isunknown
        /// </summary>
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