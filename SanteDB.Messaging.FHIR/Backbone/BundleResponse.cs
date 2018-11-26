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
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Represents response transaction control information 
    /// </summary>
    [XmlType("Bundle.Response", Namespace = "http://hl7.org/fhir")]
    public class BundleResponse : BackboneElement
    {

        /// <summary>
        /// Gets or sets the status 
        /// </summary>
        [XmlElement("status")]
        [Description("Status return code for entry")]
        [FhirElement(MinOccurs = 1)]
        public FhirString Status { get; set; }

        /// <summary>
        /// Gets or sets the location of the entry
        /// </summary>
        [XmlElement("location")]
        [Description("The location, if the operation returns a location")]
        public FhirUri Location { get; set; }

        /// <summary>
        /// Gets or sets the etag of the entry
        /// </summary>
        [XmlElement("etag")]
        [Description("The etag for the resource if relevant")]
        public FhirString ETag { get; set; }

        /// <summary>
        /// Gets or sets the last modified time
        /// </summary>
        [XmlElement("lastModified")]
        [Description("The server's date time modified")]
        public FhirInstant LastModified { get; set; }
    }
}
