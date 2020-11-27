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
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents
    /// </summary>
    [XmlType("Schedule", Namespace = "http://hl7.org/fhir")]
    public class FhirSchedule : FhirElement
    {

        /// <summary>
        /// The event that is being scheduled
        /// </summary>
        [XmlElement("event")]
        public FhirPeriod Event { get; set; }

        /// <summary>
        /// Only if there is onw or none events
        /// </summary>
        [XmlElement("repeat")]
        public ScheduleRepeat Repeat { get; set; }

    }
}
