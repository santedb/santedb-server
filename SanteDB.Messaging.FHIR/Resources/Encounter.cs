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
using SanteDB.Core.Model.Entities;
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Identifies the status of the encounter
    /// </summary>
    [XmlType("EncounterStatus", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/encounter-status")]
    public enum EncounterStatus
    {
        [XmlEnum("planned")]
        Planned,
        [XmlEnum("arrived")]
        Arrived,
        [XmlEnum("triaged")]
        Triaged,
        [XmlEnum("in-progress")]
        InProgress,
        [XmlEnum("onleave")]
        OnLeave,
        [XmlEnum("finished")]
        Finished,
        [XmlEnum("cancelled")]
        Cancelled
    }

    /// <summary>
    /// Represents an encounter  or link of acts in care
    /// </summary>
    [XmlType("Encounter", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("Encounter", Namespace = "http://hl7.org/fhir")]
    public class Encounter : DomainResourceBase
    {

        public Encounter()
        {
            this.Identifier = new List<FhirIdentifier>();
            this.Location = new List<EncounterLocation>();
            this.Participant = new List<EncounterParticipant>();
            this.StatusHistory = new List<EncounterStatusHistory>();

        }
        /// <summary>
        /// Gets or sets the identifier for the encounter
        /// </summary>
        [XmlElement("identifier")]
        [Description("The identifiers by which the encounter is known")]
        public List<FhirIdentifier> Identifier { get; set; }

        /// <summary>
        /// Gets or sets the status of the encounter
        /// </summary>
        [XmlElement("status")]
        [Description("Status of the encounter")]
        public FhirCode<EncounterStatus> Status { get; set; }

        /// <summary>
        /// Gets or sets the history of this encounter status
        /// </summary>
        [XmlElement("statusHistory")]
        [Description("History of encounter status")]
        public List<EncounterStatusHistory> StatusHistory { get; set; }

        /// <summary>
        /// Gets or sets the class of encounter
        /// </summary>
        [XmlElement("class")]
        [Description("Class of encounter")]
        public FhirCoding Class { get; set; }

        /// <summary>
        /// Gets or sets the type of encounter
        /// </summary>
        [XmlElement("type")]
        [Description("Type of encounter")]
        public FhirCodeableConcept Type { get; set; }

        /// <summary>
        /// Gets or sets the priority of the encounter
        /// </summary>
        [XmlElement("priority")]
        [Description("Priority of the encounter")]
        public FhirCodeableConcept Priority { get; set; }

        /// <summary>
        /// Gets or sets the subject of care
        /// </summary>
        [XmlElement("subject")]
        [Description("The primary subject of the encounter")]
        public Reference<Patient> Subject { get; set; }

        /// <summary>
        /// Gets or sets the participants
        /// </summary>
        [XmlElement("participant")]
        [Description("The participants that were involved in the encounter")]
        public List<EncounterParticipant> Participant { get; set; }

        /// <summary>
        /// Gets or sets the period of time
        /// </summary>
        [XmlElement("period")]
        [Description("The period of time over which the encounter took place")]
        public FhirPeriod Period { get; set; }

        /// <summary>
        /// Gets or sets the reason for the encounter
        /// </summary>
        [XmlElement("reason")]
        [Description("The reason for the encounter occurring")]
        public FhirCodeableConcept Reason { get; set; }

        /// <summary>
        /// Gets or sets the locations involved in the encounter
        /// </summary>
        [XmlElement("location")]
        [Description("Locations involved in the carrying out of the encounter")]
        public List<EncounterLocation> Location { get; set; }

        /// <summary>
        /// Gets or sets the encounter to which this encounter belongs
        /// </summary>
        [XmlElement("partOf")]
        [Description("Another encounter this encounter is a part of")]
        public Reference<Encounter> PartOf { get; set; }

        /// <summary>
        /// Gets the service provider of the encounter
        /// </summary>
        [XmlElement("serviceProvider")]
        [Description("Identifies the service provider for the encounter")]
        public Reference<Organization> ServiceProvider { get; internal set; }
    }
}
