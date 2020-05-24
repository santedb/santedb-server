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
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{

    /// <summary>
    /// Represents the condition's clinical status
    /// </summary>
    [XmlType("AllergyIntoleranceClinicalStatus", Namespace = "http://hl7.org/fhir")]
    public enum AllergyIntoleranceClinicalStatus
    {
        /// <summary>
        /// The allergy / intolerance is active
        /// </summary>
        [XmlEnum("active")]
        Active,
        /// <summary>
        /// The allergy / intolerance is no longer active
        /// </summary>
        [XmlEnum("inactive")]
        Inactive,
        /// <summary>
        /// The event has been resolved
        /// </summary>
        [XmlEnum("resolved")]
        Resolved
    }

    /// <summary>
    /// Condition verification status
    /// </summary>
    [XmlType("AllergyIntoleranceVerificationStatus", Namespace = "http://hl7.org/fhir")]
    public enum AllergyIntoleranceVerificationStatus
    {
        /// <summary>
        /// The intolerance / allergy has been confirmed
        /// </summary>
        [XmlEnum("confirmed")]
        Confirmed,
        /// <summary>
        /// The intolerance / allergy is suspected
        /// </summary>
        [XmlEnum("unconfirmed")]
        UnConfirmed,
        /// <summary>
        /// The intolerance has been refuted (confirmed not true)
        /// </summary>
        [XmlEnum("refuted")]
        Refuted,
        /// <summary>
        /// The intolerance was entered in error
        /// </summary>
        [XmlEnum("entered-in-error")]
        EnteredInError
    }

    /// <summary>
    /// Gets or sets the allergy or intolerance type
    /// </summary>
    [XmlType("AllergyIntoleranceType", Namespace = "http://hl7.org/fhir")]
    public enum AllergyIntoleranceType
    {
        /// <summary>
        /// The event is an allergy
        /// </summary>
        [XmlEnum("allergy")]
        Allergy,
        /// <summary>
        /// The event is an intolerance
        /// </summary>
        [XmlEnum("intolerance")]
        Intolerance
    }

    /// <summary>
    /// Category of the allergy or intolerance
    /// </summary>
    [XmlType("AllergyIntoleranceCategory", Namespace = "http://hl7.org/fhir")]
    public enum AllergyIntoleranceCategory
    {
        /// <summary>
        /// The intolerance is to a food substance
        /// </summary>
        [XmlEnum("food")]
        Food,
        /// <summary>
        /// The intolerance is to a drug substance
        /// </summary>
        [XmlEnum("drug")]
        Drug,
        /// <summary>
        /// The intolerance is to an environmental substance
        /// </summary>
        [XmlEnum("environment")]
        Environmental,
        /// <summary>
        /// The intolerance is to a biological substance
        /// </summary>
        [XmlEnum("biologic")]
        Biologic
    }

    /// <summary>
    /// Represents the criticality of an allergy
    /// </summary>
    [XmlType("AllergyIntoleranceCriticality", Namespace = "http://hl7.org/fhir")]
    public enum AllergyIntoleranceCriticality
    {
        /// <summary>
        /// The criticality of the record is low
        /// </summary>
        [XmlEnum("low")]
        Low,
        /// <summary>
        /// The criticality of the record is severe
        /// </summary>
        [XmlEnum("high")]
        High,
        /// <summary>
        /// The criticality was unable to be assessed
        /// </summary>
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
