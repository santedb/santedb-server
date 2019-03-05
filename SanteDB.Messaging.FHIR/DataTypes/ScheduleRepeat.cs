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
using System;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Schedule repeat
    /// </summary>
    [XmlType("ScheduleRepeat")]
    public class ScheduleRepeat
    {
        /// <summary>
        /// The frequency of per duration
        /// </summary>
        [XmlElement("frequency")]
        public FhirInt Frequency { get; set; }
        /// <summary>
        /// The event occurance duration 
        /// </summary>
        [XmlElement("when")]
        public FhirCode<String> When { get; set; }
        /// <summary>
        /// Repeating or event-related duration
        /// </summary>
        [XmlElement("duration")]
        public Primitive<Decimal> Duration { get; set; }
        /// <summary>
        /// The units of time for the duration
        /// </summary>
        [XmlElement("units")]
        public FhirString Units { get; set; }
        /// <summary>
        /// The number of times to repeat
        /// </summary>
        [XmlElement("count")]
        public FhirInt Count { get; set; }
        /// <summary>
        /// The stop date
        /// </summary>
        [XmlElement("stop")]
        public FhirDateTime Stop { get; set; }
    }
}
