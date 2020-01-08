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
using Newtonsoft.Json;
using RestSrvr.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
{

    /// <summary>
    /// Represents configuration of a single AGS service
    /// </summary>
    [XmlType(nameof(RestServiceConfiguration), Namespace = "http://santedb.org/configuration")]
    [XmlRoot(nameof(RestServiceConfiguration), Namespace = "http://santedb.org/configuration")]
    [JsonObject]
    public class RestServiceConfiguration
    {
        // Configuration
        private static XmlSerializer s_serializer;

        /// <summary>
        /// AGS Service Configuration
        /// </summary>
        public RestServiceConfiguration()
        {
            this.Behaviors = new List<RestBehaviorConfiguration>();
            this.Endpoints = new List<RestEndpointConfiguration>();
        }

        /// <summary>
        /// Creates a service configuration from the specified type
        /// </summary>
        internal RestServiceConfiguration(Type type) : this()
        {
            this.Name = type.GetCustomAttribute<ServiceBehaviorAttribute>()?.Name ?? type.FullName;
            this.ServiceType = type;
        }

        /// <summary>
        /// Gets or sets the name of the service
        /// </summary>
        [XmlAttribute("name"), JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the behavior
        /// </summary>
        [XmlAttribute("serviceBehavior"), JsonProperty("serviceBehavior")]
        public String ServiceTypeXml { get; set; }

        /// <summary>
        /// Service ignore
        /// </summary>
        [XmlIgnore, JsonIgnore, Browsable(false)]
        public Type ServiceType { get => this.ServiceTypeXml != null ? Type.GetType(this.ServiceTypeXml) : null; set => this.ServiceTypeXml = value?.AssemblyQualifiedName; }

        /// <summary>
        /// Gets or sets the behavior of the AGS endpoint
        /// </summary>
        [XmlArray("behaviors"), XmlArrayItem("add"), JsonProperty("behaviors")]
        public List<RestBehaviorConfiguration> Behaviors { get; set; }

        /// <summary>
        /// Gets or sets the endpoints 
        /// </summary>
        [XmlElement("endpoint"), JsonProperty("endpoint")]
        public List<RestEndpointConfiguration> Endpoints { get; set; }

        /// <summary>
        /// Load from the specified stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        internal static RestServiceConfiguration Load(Stream stream)
        {
            if (s_serializer == null)
                s_serializer = new XmlSerializer(typeof(RestServiceConfiguration));
            return s_serializer.Deserialize(stream) as RestServiceConfiguration;
        }
    }
}