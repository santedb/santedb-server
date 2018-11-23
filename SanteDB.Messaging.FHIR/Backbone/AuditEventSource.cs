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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Represents the source information or reporter for the audit
    /// </summary>
    [XmlType("AuditEventSource", Namespace = "http://hl7.org/fhir")]
    public class AuditEventSource : BackboneElement
    {
        /// <summary>
        /// Gets or sets the site under which the data reported
        /// </summary>
        [XmlElement("site")]
        [Description("The site of the audit reporter")]
        public FhirString Site { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the reporter
        /// </summary>
        [XmlElement("identifier")]
        [Description("The identifier of the reporting source")]
        [FhirElement(MinOccurs = 1)]
        public FhirIdentifier Identifier { get; set; }

        /// <summary>
        /// Gets or sets the type(s) of source 
        /// </summary>
        [XmlElement("type")]
        [Description("Represents the type of source")]
        public List<FhirCoding> Type { get; set; }
    }
}
