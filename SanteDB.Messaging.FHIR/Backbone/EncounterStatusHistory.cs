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
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// A backbone element for tracking encounter status history
    /// </summary>
    [XmlType("EncounterStatusHistory", Namespace = "http://hl7.org/fhir")]
    public class EncounterStatusHistory : BackboneElement
    {

        /// <summary>
        /// Gets or sets the history
        /// </summary>
        [XmlElement("status")]
        [FhirElement(MinOccurs = 1)]
        [Description("The status of the encounter at this point in time")]
        public FhirCode<EncounterStatus> Status { get; set; }

        /// <summary>
        /// Gets or sets the period 
        /// </summary>
        [XmlElement("period")]
        [FhirElement(MinOccurs = 1)]
        [Description("The time span when the status was valid")]
        public FhirPeriod Period { get; set; }
    }
}
