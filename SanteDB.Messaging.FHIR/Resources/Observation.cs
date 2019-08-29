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
using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Filter operators
    /// </summary>
    [XmlType("ObservationStatus", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/observationStatus")]
    public enum ObservationStatus
    {
        /// <summary>
        /// The observation has been registered
        /// </summary>
        [XmlEnum("registered")]
        Registered,
        /// <summary>
        /// The observation is unconfirmed / preliminary
        /// </summary>
        [XmlEnum("preliminary")]
        Preliminary,
        /// <summary>
        /// The observation is a final observation made
        /// </summary>
        [XmlEnum("final")]
        Final,
        /// <summary>
        /// The observation has been amended
        /// </summary>
        [XmlEnum("amended")]
        Amended,
        /// <summary>
        /// The observation has been corrected
        /// </summary>
        [XmlEnum("corrected")]
        Corrected,
        /// <summary>
        /// The observation was cancelled
        /// </summary>
        [XmlEnum("cancelled")]
        Cancelled,
        /// <summary>
        /// The observation was entered in error
        /// </summary>
        [XmlEnum("entered-in-error")]
        EnteredInError,
        /// <summary>
        /// The status of the observation is unknown
        /// </summary>
        [XmlEnum("unknown")]
        Unknown
    }
    /// <summary>
    /// Observation 
    /// </summary>
    [XmlType("Observation", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("Observation", Namespace = "http://hl7.org/fhir")]
    public class Observation : DomainResourceBase
    {

        /// <summary>
        /// Default ctor
        /// </summary>
        public Observation()
        {
            this.Identifier = new List<FhirIdentifier>();
        }

        /// <summary>
        /// Gets or sets the identifiers for the observation
        /// </summary>
        [XmlElement("identifier")]
        public List<FhirIdentifier> Identifier { get; set; }

        /// <summary>
        /// Gets or sets the status of the observation
        /// </summary>
        [XmlElement("status")]
        public FhirCode<ObservationStatus> Status { get; set; }

        /// <summary>
        /// Gets or sets the category
        /// </summary>
        [XmlElement("category")]
        public FhirCodeableConcept Category { get; set; }

        /// <summary>
        /// Gets or sets the code or type of observation
        /// </summary>
        [XmlElement("code")]
        public FhirCodeableConcept Code { get; set; }

        /// <summary>
        /// Gets or sets the subject of the observation
        /// </summary>
        [XmlElement("subject")]
        public Reference<Patient> Subject { get; set; }

        //[XmlElement("context")]
        //public Reference<Encounter> Context { get; set; }

            /// <summary>
            /// Gets or sets the time that the observation was made
            /// </summary>
        [XmlElement("effectiveDateTime")]
        public FhirDateTime EffectiveDateTime { get; set; }

        /// <summary>
        /// Gets or sets the date or time that the observation became available
        /// </summary>
        [XmlElement("issued")]
        public FhirInstant Issued { get; set; }

        /// <summary>
        /// Gets or sets the performer of the observation
        /// </summary>
        [XmlElement("performer")]
        public Reference<Practitioner> Performer { get; set; }

        /// <summary>
        /// Gets or sets the value of the observation
        /// </summary>
        [XmlElement("valueQuantity", typeof(FhirQuantity))]
        [XmlElement("valueCodeableConcept", typeof(FhirCodeableConcept))]
        [XmlElement("valueString", typeof(FhirString))]
        public Object Value { get; set; }

        /// <summary>
        /// Gets or sets the reason why data is not present
        /// </summary>
        [XmlElement("dataAbsentReason")]
        public FhirCodeableConcept DataAbsentReason { get; set; }

        /// <summary>
        /// Gets or sets the interpretation of the observation
        /// </summary>
        [XmlElement("interpretation")]
        public FhirCodeableConcept Interpretation { get; set; }

        /// <summary>
        /// Gets or sets the comment related to the observation
        /// </summary>
        [XmlElement("comment")]
        public String Comment { get; set; }

    }
}
