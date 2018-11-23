/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: justin
 * Date: 2018-11-23
 */
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{

    /// <summary>
    /// Represents the condition's clinical status
    /// </summary>
    [XmlType("AllergyIntoleranceClinicalStatus", Namespace = "http://hl7.org/fhir")]
    public enum AllergyIntoleranceClinicalStatus
    {
        [XmlEnum("active")]
        Active,
        [XmlEnum("inactive")]
        Inactive,
        [XmlEnum("resolved")]
        Resolved
    }

    /// <summary>
    /// Condition verification status
    /// </summary>
    [XmlType("AllergyIntoleranceVerificationStatus", Namespace = "http://hl7.org/fhir")]
    public enum AllergyIntoleranceVerificationStatus
    {
        [XmlEnum("confirmed")]
        Confirmed,
        [XmlEnum("unconfirmed")]
        UnConfirmed,
        [XmlEnum("refuted")]
        Refuted,
        [XmlEnum("entered-in-error")]
        EnteredInError
    }

    /// <summary>
    /// Gets or sets the allergy or intolerance type
    /// </summary>
    [XmlType("AllergyIntoleranceType", Namespace = "http://hl7.org/fhir")]
    public enum AllergyIntoleranceType
    {
        [XmlEnum("allergy")]
        Allergy,
        [XmlEnum("intolerance")]
        Intolerance
    }

    /// <summary>
    /// Category of the allergy or intolerance
    /// </summary>
    [XmlType("AllergyIntoleranceCategory", Namespace = "http://hl7.org/fhir")]
    public enum AllergyIntoleranceCategory
    {
        [XmlEnum("food")]
        Food,
        [XmlEnum("drug")]
        Drug,
        [XmlEnum("environment")]
        Environmental,
        [XmlEnum("biologic")]
        Biologic
    }

    /// <summary>
    /// Represents the criticality of an allergy
    /// </summary>
    [XmlType("AllergyIntoleranceCriticality", Namespace = "http://hl7.org/fhir")]
    public enum AllergyIntoleranceCriticality
    {
        [XmlEnum("low")]
        Low,
        [XmlEnum("high")]
        High,
        [XmlEnum("unable-to-assess")]
        Unknown
    }

    /// <summary>
    /// Represents an allergy or intolerance
    /// </summary>
    [XmlType("AllergyIntolerance", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("AllergyIntolerance", Namespace = "http://hl7.org/fhir")]
    public class AllergyIntolerance : DomainResourceBase
    {

        /// <summary>
        /// Allergy intolerance reaction
        /// </summary>
        public AllergyIntolerance()
        {
            this.Identifier = new List<FhirIdentifier>();
            this.Reaction = new List<AllergyIntoleranceReaction>();
        }

        /// <summary>
        /// Gets or sets the identifier
        /// </summary>
        [XmlElement("identifier")]
        [Description("Identifiers for the allergy")]
        public List<FhirIdentifier> Identifier { get; set; }

        /// <summary>
        /// Gets or sets the clinical status
        /// </summary>
        [XmlElement("clinicalStatus")]
        [Description("Clinical status of the allergy")]
        public FhirCode<AllergyIntoleranceClinicalStatus> ClinicalStatus { get; set; }

        /// <summary>
        /// Gets or sets the verification status
        /// </summary>
        [XmlElement("verificationStatus")]
        [Description("Verification status of the allergy")]
        public FhirCode<AllergyIntoleranceClinicalStatus> VerificationStatus { get; set; }

        /// <summary>
        /// The type of intolerance
        /// </summary>
        [XmlElement("type")]
        [Description("The type of the intolerance or allergy")]
        public FhirCode<AllergyIntoleranceType> Type { get; set; }

        /// <summary>
        /// Gets or sets the category
        /// </summary>
        [XmlElement("category")]
        [Description("The category of the intolerance or allergy")]
        public FhirCode<AllergyIntoleranceCategory> Category { get; set; }

        /// <summary>
        /// The criticality of the allergy or intolerance
        /// </summary>
        [XmlElement("criticality")]
        [Description("The criticality of the allergy or intolerance")]
        public FhirCode<AllergyIntoleranceCriticality> Criticality { get; set; }

        /// <summary>
        /// Gets or sets the code of the allergy 
        /// </summary>
        [XmlElement("code")]
        [Description("The code which identifies the allergy")]
        public FhirCodeableConcept Code { get; set; }

        /// <summary>
        /// Gets or sets the patient 
        /// </summary>
        [XmlElement("patient")]
        [Description("Identifies")]
        public Reference<Patient> Patient { get; set; }

        /// <summary>
        /// Onset date/time
        /// </summary>
        [XmlElement("onsetDateTime", Type = typeof(FhirDateTime))]
        [XmlElement("onsetPeriod", Type = typeof(FhirPeriod))]
        [XmlElement("onsetString", Type = typeof(FhirString))]
        [Description("The time(s) when the condition was active")]
        public object Onset { get; set; }

        /// <summary>
        /// Gets or sets the date when the condition was asserted
        /// </summary>
        [XmlElement("assertedDate")]
        [Description("The date when the condition was asserted")]
        public FhirDateTime AssertedDate { get; set; }

        /// <summary>
        /// Gets or sets who asserted the reference
        /// </summary>
        [XmlElement("recorder")]
        [Description("Who recorded the condition")]
        public Reference<Practitioner> Recorder { get; set; }

        /// <summary>
        /// Gets or sets who asserted the reference
        /// </summary>
        [XmlElement("asserter")]
        [Description("Who asserted the condition")]
        public Reference Asserter { get; set; }

        /// <summary>
        /// Gets or sets the reaction information
        /// </summary>
        public List<AllergyIntoleranceReaction> Reaction { get; set; }
    }
}
