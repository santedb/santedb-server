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
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{

    /// <summary>
    /// Represents the adverse event categorization
    /// </summary>
    [XmlType("AdverseEventCategory", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/adverse-event-category")]
    public enum AdverseEventCategory
    {
        [XmlEnum("AE")]
        AdverseEvent,
        [XmlEnum("PAE")]
        PotentialAdverseEvent
    }

   
    /// <summary>
    /// Gets or sets the adverse event information
    /// </summary>
    [XmlType("AdverseEvent", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("AdverseEvent", Namespace = "http://hl7.org/fhir")]
    public class AdverseEvent : DomainResourceBase
    {

        public AdverseEvent()
        {
            this.Reaction = new List<Reference<Condition>>();

        }
        /// <summary>
        /// Gets or sets the business identifier for the AE
        /// </summary>
        [XmlElement("identifier")]
        [Description("Business identifier for the event")]
        public FhirIdentifier Identifier { get; set; }

        /// <summary>
        /// Gets or sets the category of the event
        /// </summary>
        [XmlElement("category")]
        [Description("AE or PAE representing the categorization of the event")]
        public FhirCode<AdverseEventCategory> Category { get; set; }

        /// <summary>
        /// Gets or sets the type of event
        /// </summary>
        [Description("The type of adverse event")]
        [XmlElement("type")]
        public FhirCodeableConcept Type { get; set; }

        /// <summary>
        /// Gets or sets the subject of the reaction
        /// </summary>
        [Description("Subject impacted by the event")]
        [XmlElement("subject")]
        public Reference<Patient> Subject { get; set; }

        /// <summary>
        /// Gets or sets the date of the event
        /// </summary>
        [Description("When the event occurred")]
        [XmlElement("date")]
        public FhirDateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the reactions that ocurred
        /// </summary>
        [Description("The adverse reaction that occurred")]
        [XmlElement("reaction")]
        public List<Reference<Condition>> Reaction { get; set; }

        /// <summary>
        /// Gets or sets the location where the adverse event occurred
        /// </summary>
        [Description("Locaton where the adverse event occurred")]
        [XmlElement("location")]
        public Reference<Location> Location { get; set; }

        /// <summary>
        /// Gets or sets the seriousness of reaction
        /// </summary>
        [Description("The seriousness of the reaction")]
        [XmlElement("seriousness")]
        public FhirCodeableConcept Seriousness { get; set; }

        /// <summary>
        /// Gets or sets the outcome of the reaction
        /// </summary>
        [XmlElement("outcome")]
        [Description("The outcome of the adverese event")]
        public FhirCodeableConcept Outcome { get; set; }

        /// <summary>
        /// Gets or sets who recorded the event
        /// </summary>
        [XmlElement("recorder")]
        [Description("Who recorded the event")]
        public Reference Recorder { get; set; }

        /// <summary>
        /// Gets or sets the description of the event
        /// </summary>
        [XmlElement("description")]
        [Description("Textual description of the event")]
        public FhirString Description { get; set; }

        /// <summary>
        /// Gets or sets one or more entities suspected of causing the reaction
        /// </summary>
        [XmlElement("suspectEntity")]
        [Description("One or more agents which are suspected to have caused the event")]
        public List<AdverseEventSuspectEntity> SuspectEntity { get; set; }

    }
}
