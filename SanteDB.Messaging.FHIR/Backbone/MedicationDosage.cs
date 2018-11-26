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
using SanteDB.Messaging.FHIR.DataTypes;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Represents the dosage of medication given to a patient
    /// </summary>
    [XmlType("MedicationDosage", Namespace = "http://hl7.org/fhir")]
    public class MedicationDosage : BackboneElement
    {

        /// <summary>
        /// Gets or sets the textual description of the dosage
        /// </summary>
        [XmlElement("text")]
        [Description("Free text dosing instructions")]
        public FhirString Text { get; set; }

        /// <summary>
        /// Gets or sets the site of administration
        /// </summary>
        [XmlElement("site")]
        [Description("Body site administered to")]
        public FhirCodeableConcept Site { get; set; }

        /// <summary>
        /// Route of administration
        /// </summary>
        [XmlElement("route")]
        [Description("Route of administration")]
        public FhirCodeableConcept Route { get; set; }

        /// <summary>
        /// Gets or sets the method of administration
        /// </summary>
        [XmlElement("method")]
        [Description("Method of administration of drug")]
        public FhirCodeableConcept Method { get; set; }

        /// <summary>
        /// Gets or sets the dose amount
        /// </summary>
        [XmlElement("dose")]
        [Description("Dose quantity of medication")]
        public FhirQuantity Dose { get; set; }
        
    }
}
