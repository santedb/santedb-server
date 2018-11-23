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
    /// Medication batch
    /// </summary>
    [XmlType("MedicationBatch", Namespace = "http://hl7.org/fhir")]
    public class MedicationBatch : BackboneElement
    {

        /// <summary>
        /// Gets or sets the lot number
        /// </summary>
        [XmlElement("lotNumber")]
        [Description("Identifier assigned to the batch")]
        public FhirString LotNumber { get; set; }

        /// <summary>
        /// Gets or sets the expiration time
        /// </summary>
        [XmlElement("expirationDate")]
        [Description("When the batch will expire")]
        public FhirDateTime Expiration { get; set; }
    }
}