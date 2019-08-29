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
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{

    /// <summary>
    /// Medication administration status
    /// </summary>
    [XmlType("MedicationAdministrationStatus", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/medication-admin-status")]
    public enum MedicationAdministrationStatus
    {
        /// <summary>
        /// The administration of medication is ongoing 
        /// </summary>
        [XmlEnum("in-progress")]
        InProgress, 
        /// <summary>
        /// The administration of medication has been suspended or placed on hold
        /// </summary>
        [XmlEnum("on-hold")]
        OnHold,
        /// <summary>
        /// The administration of medication has been completed
        /// </summary>
        [XmlEnum("completed")]
        Completed,
        /// <summary>
        /// The administration of medication record was entered in error
        /// </summary>
        [XmlEnum("entered-in-error")]
        EnteredInError,
        /// <summary>
        /// The medication administration was stopped for some reason
        /// </summary>
        [XmlEnum("stopped")]
        Stopped
    }

    /// <summary>
    /// Represents a medication administration
    /// </summary>
    [XmlType("MedicationAdministration", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("MedicationAdministration", Namespace = "http://hl7.org/fhir")]
    public class MedicationAdministration : DomainResourceBase
    {
        /// <summary>
        /// Gets or sets the identifier for the administration
        /// </summary>
        [XmlElement("identifier")]
        [Description("External identifier")]
        public List<FhirIdentifier> Identifier { get; set; }

        /// <summary>
        /// Gets or sets the encounter this is part of
        /// </summary>
        [XmlElement("partOf")]
        [Description("Part of refernced event")]
        public Reference<MedicationAdministration> PartOf { get; set; }

        /// <summary>
        /// Status of the administration
        /// </summary>
        [XmlElement("status")]
        [Description("Status of the event")]
        [FhirElement(MinOccurs = 1)]
        public FhirCode<MedicationAdministrationStatus> Status { get; set; }

        /// <summary>
        /// Category of the medication useage
        /// </summary>
        [Description("Type of medication use")]
        [XmlElement("category")]
        public FhirCodeableConcept Category { get; set; }

        /// <summary>
        /// Gets or sets the reference of the medication
        /// </summary>
        [XmlElement("medicationCodeableConcept", Type = typeof(FhirCodeableConcept))]
        [XmlElement("medicationReference", Type = typeof(Reference<Medication>))]
        [FhirElement(MinOccurs = 1)]
        [Description("What was administered")]
        public Object Medication { get; set; }

        /// <summary>
        /// Gets or sets the patient subject of the administration
        /// </summary>
        [XmlElement("subject")]
        [FhirElement(MinOccurs = 1)]
        [Description("Who received the medication")]
        public Reference<Patient> Subject { get; set; }

        /// <summary>
        /// Effective time of the administration
        /// </summary>
        [FhirElement(MinOccurs = 1)]
        [Description("Start and end time of the administration")]
        [XmlElement("effectiveDateTime")]
        public FhirDateTime EffectiveDate { get; set; }

        /// <summary>
        /// Gets or sets the performer of the administration
        /// </summary>
        [XmlElement("performer")]
        [Description("Who administered the substance")]
        public List<MedicationPerformer> Performer { get; set; }

        /// <summary>
        /// True if the medication was not administered
        /// </summary>
        [XmlElement("notGiven")]
        [Description("True if the medication was not administered")]
        public bool NotGiven { get; set; }

        /// <summary>
        /// Gets or sets the reason for not giving the medication
        /// </summary>
        [XmlElement("reasonNotGiven")]
        [Description("Reason administration was not performed")]
        public FhirCodeableConcept ReasonNotGiven { get; set; }

        /// <summary>
        /// Gets or sets the reason for performing the administration
        /// </summary>
        [XmlElement("reasonCode")]
        [Description("The reason for performing the administration")]
        public FhirCodeableConcept ReasonCode { get; set; }

        /// <summary>
        /// Gets or sets why the administration was given
        /// </summary>
        [XmlElement("reasonReference")]
        [Description("An observation that supports why the medication was administered")]
        public Reference<Observation> ReasonReference { get; set; }

        /// <summary>
        /// Gets or sets the dosage information
        /// </summary>
        [XmlElement("dosage")]
        [Description("Details of how the medication was taken")]
        public MedicationDosage Dosage { get; set; }

    }
}
