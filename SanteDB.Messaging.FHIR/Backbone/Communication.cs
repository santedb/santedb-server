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
using System.Linq;
using System.Text;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;
using System.ComponentModel;
using SanteDB.Messaging.FHIR.Attributes;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Represents a language of communication
    /// </summary>
    [XmlType("Patient.Communication", Namespace = "http://hl7.org/fhir")]
    public class Communication : BackboneElement
    {
        /// <summary>
        /// Gets or sets the language code
        /// </summary>
        [XmlElement("language")]
        [Description("Language with optional region")]
        [FhirElement(MinOccurs = 1, RemoteBinding = "http://tools.ietf.org/html/bcp47")]
        public FhirCodeableConcept Value { get; set; }
        /// <summary>
        /// Gets or sets the preference indicator
        /// </summary>
        [XmlElement("preferred")]
        [Description("Language preference indicator")]
        public FhirBoolean Preferred { get; set; }
    }
}
