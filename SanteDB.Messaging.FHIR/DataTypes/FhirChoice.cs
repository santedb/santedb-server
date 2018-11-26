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
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{

    /// <summary>
    /// Option
    /// </summary>
    [XmlType("Option", Namespace = "http://hl7.org/fhir")]
    public class Option
    {
        /// <summary>
        /// Gets or sets the code
        /// </summary>
        [XmlElement("code")]
        public FhirCode<String> Code { get; set; }

        /// <summary>
        /// Gets or sets the display name
        /// </summary>
        [XmlElement("display")]
        public FhirCode<String> Display { get; set; }
    }

    /// <summary>
    /// Choice element
    /// </summary>
    [XmlType("Choice", Namespace = "http://hl7.org/fhir")]
    public class FhirChoice : FhirElement
    {

        /// <summary>
        /// Gets or sets the primary selected code
        /// </summary>
        [XmlElement("code")]
        public FhirCode<String> Code { get; set; }

        /// <summary>
        /// Gets or sets the alternate options 
        /// </summary>
        [XmlElement("option")]
        public List<Option> Option { get; set; }

        /// <summary>
        /// Gets or sets whether the options are ordered
        /// </summary>
        [XmlElement("isOrdered")]
        public FhirBoolean IsOrdered { get; set; }
    }
}
