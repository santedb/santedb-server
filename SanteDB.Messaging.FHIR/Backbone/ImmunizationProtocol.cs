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
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Represents an immunization protocol
    /// </summary>
    [XmlType("Immunization.VaccinationProtocol", Namespace = "http://hl7.org/fhir")]
    public class ImmunizationProtocol : BackboneElement
    {
        /// <summary>
        /// Gets or sets the dose sequence
        /// </summary>
        [XmlElement("doseSequence")]
        [FhirElement(MinOccurs = 1)]
        public FhirInt DoseSequence { get; set; }

        /// <summary>
        /// Gets or sets the description of the protocol
        /// </summary>
        [XmlElement("description")]
        public FhirString Description { get; set; }

        /// <summary>
        /// Gets or sets the authority under which the protocol was created
        /// </summary>
        [XmlElement("authority")]
        public Reference<Organization> Authority { get; set; }

        /// <summary>
        /// Gets or sets the series name
        /// </summary>
        [XmlElement("series")]
        public FhirString Series { get; set; }

        /// <summary>
        /// Gets or sets the total number of doses in the series
        /// </summary>
        [XmlElement("seriesDoses")]
        public FhirInt SeriesDoses { get; set; }

        /// <summary>
        /// Gets or sets the diseases this protocol treats
        /// </summary>
        [XmlElement("targetDisease")]
        public List<FhirCodeableConcept> TargetDisease { get; set; }

        /// <summary>
        /// Gets or sets the dose status
        /// </summary>
        [XmlElement("doseStatus")]
        public FhirCodeableConcept DoseStatus { get; set; }

        /// <summary>
        /// Gets or sets why the dose does not count
        /// </summary>
        [XmlElement("doseStatusReason")]
        public FhirCodeableConcept DoseStatusReason { get; set; }

    }
}
