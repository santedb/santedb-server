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
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Represents a participant who actively cooperates in teh carrying out of the encounter
    /// </summary>
    [XmlType("EncounterParticipant", Namespace = "http://hl7.org/fhir")]
    public class EncounterParticipant : BackboneElement
    {

        /// <summary>
        /// Gets or sets the type of role
        /// </summary>
        [Description("Role of the participant in the encounter")]
        [XmlElement("type")]
        [FhirElement(MinOccurs = 0)]
        public List<FhirCodeableConcept> Type { get; set; }

        /// <summary>
        /// Gets or sets the period of involvement
        /// </summary>
        [XmlElement("period")]
        [Description("The period that this person was involved")]
        public FhirPeriod Period { get; set; }

        /// <summary>
        /// Gets or sets the person involved
        /// </summary>
        [XmlElement("individual")]
        [Description("The person who was involved")]
        public Reference<Practitioner> Individual { get; set; }
    }
}
