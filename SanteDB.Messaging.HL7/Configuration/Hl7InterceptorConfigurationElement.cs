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
using Newtonsoft.Json;
using SanteDB.Core.Model.DataTypes;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Messaging.HL7.Configuration
{
    /// <summary>
    /// HL7 Notifications Configuration Element
    /// </summary>
    [XmlType(nameof(Hl7InterceptorConfigurationElement), Namespace = "http://santedb.org/configuration")]
    public class Hl7InterceptorConfigurationElement
    {

        /// <summary>
        /// Gets the XML type name of the notification
        /// </summary>
        [XmlAttribute("type"), JsonProperty("type")]
        public string InterceptorClassXml { get; set; }

        /// <summary>
        /// Gets or sets the notifier
        /// </summary>
        [XmlIgnore, JsonIgnore]
        public Type InterceptorClass { get => Type.GetType(this.InterceptorClassXml); set => this.InterceptorClassXml = value?.GetType().AssemblyQualifiedName; }

        /// <summary>
        /// Guards to filter the incoming data
        /// </summary>
        [XmlArray("guards"), XmlArrayItem("add"), JsonProperty("guards")]
        public List<String> Guards { get; set; }

        /// <summary>
        /// Represents endpoints
        /// </summary>
        [XmlArray("endpoints"), XmlArrayItem("add"), JsonProperty("endpoints")]
        public List<Hl7RemoteEndpointConfiguration> Endpoints { get; set; }

        /// <summary>
        /// Gets or sets the identity domains to notify the remote target of
        /// </summary>
        [XmlArray("domains"), XmlArrayItem("add"), JsonProperty("domains")]
        public List<AssigningAuthority> ExportDomains { get; set; }

        /// <summary>
        /// Sets the version
        /// </summary>
        [XmlAttribute("hl7version"), JsonProperty("hl7version")]
        public string Version { get; set; }
    }
}