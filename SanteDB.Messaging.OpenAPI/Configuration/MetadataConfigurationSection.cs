﻿/*
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
using SanteDB.Core.Configuration;
using SanteDB.Core.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.Metadata.Configuration
{
    /// <summary>
    /// Represents the configuration for the OpenApi
    /// </summary>
    [XmlType(nameof(MetadataConfigurationSection), Namespace = "http://santedb.org/configuration")]
    [JsonObject(nameof(MetadataConfigurationSection))]
    public class MetadataConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Gets or sets the service contracts to document
        /// </summary>
        [XmlArray("services"), XmlArrayItem("add"), JsonProperty("services")]
        public List<ServiceEndpointOptions> Services { get; set; }

        /// <summary>
        /// Gets or sets the default host to apply
        /// </summary>
        [XmlElement("host"), JsonProperty("host")]
        public String ApiHost { get; set; }
    }
}
