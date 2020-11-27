/*
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
using SanteDB.Core.Auditing;
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
    /// Represents an immunization
    /// </summary>
    [XmlType(nameof(Immunization), Namespace = "http://hl7.org/fhir")]
    [XmlRoot(nameof(Immunization), Namespace = "http://hl7.org/fhir")]
    [ParticipantObjectMap(IdType = AuditableObjectIdType.EncounterNumber, Role = AuditableObjectRole.Resource, Type = AuditableObjectType.Other)]
    public class Immunization : DomainResourceBase
    {

        /// <summary>
        /// Creates a new immunization
        /// </summary>
        public Immunization()
        {
            this.Identifier = new List<FhirIdentifier>();
            this.VaccinationProtocol = new List<ImmunizationProtocol>();
        }

        /// <summary>
        /// Gets or sets the identifier of the immunization
        /// </summary>
        [XmlElement("identifier")]
        [Description("An identifier for the person as this immunization")]
        [FhirElement(MinOccurs = 1, MaxOccurs = -1)]
        public List<FhirIdentifier> Identifier { get; set; }

        /// <summary>
        /// Represents the status of the resource
        /// </summary>
        [XmlElement("status")]
        [Description("in-progress | on-hold | completed | entered-in-error | stopped")]
        [FhirElement(MinOccurs = 1, MaxOccurs = 1, RemoteBinding = "http://hl7.org/fhir/ValueSet/medication-admin-status")]
        public FhirCode<String> Status { get; set; }

        /// <summary>
        /// Gets or sets the date that the vaccine was administered
        /// </summary>
        [XmlElement("date")]
        [Description("Vaccination date")]
        [FhirElement(MinOccurs = 1, MaxOccurs = 1)]
        public FhirDate Date { get; set; }

        /// <summary>
        /// Gets or sets the vaccine administered
        /// </summary>
        [XmlElement("vaccineCode")]
        [Description("Gets or sets the product administered")]
        [FhirElement(MinOccurs = 1, MaxOccurs = 1)]
        public FhirCodeableConcept VaccineCode { get; set; }

        /// <summary>
        /// Gets or sets the patient to whom the action was done
        /// </summary>
        [XmlElement("patient")]
        [Description("The patient to whom the product was administered")]
        [FhirElement(MinOccurs = 1)]
        public Reference<Patient> Patient { get; set; }

        /// <summary>
        /// Indicates whether the vaccination was given
        /// </summary>
        [XmlElement("wasNotGiven")]
        [Description("Indicates whether the vaccination was performed")]
        [FhirElement(MinOccurs = 1)]
        public FhirBoolean WasNotGiven { get; set; }

        /// <summary>
        /// Gets or sets whether the vaccination was self reported
        /// </summary>
        [XmlElement("selfReported")]
        [Description("Indicates whether the vaccination record was reported by the patient")]
        [FhirElement(MinOccurs = 1)]
        public FhirBoolean SelfReported { get; set; }

        /// <summary>
        /// Gets or sets the practitioner who performed the vaccination
        /// </summary>
        [XmlElement("performer")]
        [Description("Indicates the practitioner who performed the vaccination")]
        public Reference<Practitioner> Performer { get; set; }

        /// <summary>
        /// Gets or sets the practitioner who requested the vaccination
        /// </summary>
        [XmlElement("requester")]
        [Description("Indicates the practitioner who requested the vaccination")]
        public Reference<Practitioner> Requester { get; set; }

        /// <summary>
        /// Gets or sets the encounter during which the vaccination was performed
        /// </summary>
        [XmlElement("encounter")]
        [Description("Indicates the encounter in which the vaccination occurred")]
        public Reference<Encounter> Encounter { get; set; }

        /// <summary>
        /// Gets or sets the manufacturer of the vaccine administered
        /// </summary>
        [XmlElement("manufacturer")]
        [Description("Indicates the manufacturer of the vaccine administered")]
        public Reference<Organization> Manufacturer { get; set; }

        /// <summary>
        /// Gets or sets the location where the immunization was performed
        /// </summary>
        [XmlElement("location")]
        [Description("Identifies the location where the immunization occurred")]
        public Reference<Location> Location { get; set; }

        /// <summary>
        /// Gets or sets the lot number of the vaccination
        /// </summary>
        [XmlElement("lotNumber")]
        [Description("Identifies the lot number administered to the patient")]
        public FhirString LotNumber { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the object administered
        /// </summary>
        [XmlElement("expirationDate")]
        [Description("Identifies the expiration date of the vaccine administered")]
        public FhirDate ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the site where the administration occurred
        /// </summary>
        [XmlElement("site")]
        [Description("Identifies the site where the administration occurred")]
        public FhirCodeableConcept Site { get; set; }

        /// <summary>
        /// Gets or sets the route for which the vaccination was administered
        /// </summary>
        [XmlElement("route")]
        [Description("Identified the route in which the vaccine was administered")]
        public FhirCodeableConcept Route { get; set; }

        /// <summary>
        /// Gets or sets the dose quantity 
        /// </summary>
        [XmlElement("doseQuantity")]
        [Description("Identifies the quantity of substance administered")]
        public FhirQuantity DoseQuantity { get; set; }

        // TODO: These elements need to be implemented and are commented out 

        /// <summary>
        /// Gets or sets annotations related to the vaccination
        /// </summary>
        //[XmlElement("note")]
        //[Description("Notes about the vaccination")]
        //public List<Annotation> Annotation { get; set; }

        /// <summary>
        /// Represents an explanation of why the vaccine was not administered
        /// </summary>
        //[XmlElement("explanation")]
        //[Description("Gets or sets the explanation of why the vaccination did not occur")]
        //public ImmunizationRefusal Explanation { get; set; }

        /// <summary>
        /// Gets or sets the list of reactions
        /// </summary>
        //[XmlElement("reaction")]
        //[Description("Indicates the reactions that occurred after giving the vaccination")]
        //public List<ImmunizationReaction> Reaction { get; set; }

        /// <summary>
        /// Gets or sets the vaccination protocol
        /// </summary>
        [XmlElement("vaccinationProtocol")]
        [Description("Indicates the protocol which was followed ")]
        public List<ImmunizationProtocol> VaccinationProtocol { get; set; }

    }
}
