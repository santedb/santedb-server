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
using SanteDB.Core.Configuration;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
{
    /// <summary>
    /// Represents the configuration for the AGS
    /// </summary>
    [XmlType(nameof(RestConfigurationSection), Namespace = "http://santedb.org/configuration")]
    [JsonObject]
    public class RestConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Construct the AGS configuration
        /// </summary>
        public RestConfigurationSection()
        {
            this.Services = new List<RestServiceConfiguration>();
        }

        /// <summary>
        /// Gets or sets the service configuration
        /// </summary>
        [XmlElement("service"), JsonProperty("service")]
        public List<RestServiceConfiguration> Services { get; set; }
    }
}
