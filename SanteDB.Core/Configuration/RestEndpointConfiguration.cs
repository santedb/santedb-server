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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
{
    /// <summary>
    /// Represents an endpoint configuration
    /// </summary>
    [XmlType(nameof(RestEndpointConfiguration), Namespace = "http://santedb.org/configuration")]
    [JsonObject]
    public class RestEndpointConfiguration
    {

        /// <summary>
        /// AGS Endpoint CTOR
        /// </summary>
        public RestEndpointConfiguration()
        {
            this.Behaviors = new List<RestBehaviorConfiguration>();
        }

        /// <summary>
        /// Gets or sets the contract type
        /// </summary>
        [XmlAttribute("contract"), JsonProperty("contract")]
        public String ContractXml { get; set; }

        /// <summary>
        /// Gets or sets the Contract type
        /// </summary>
        [XmlIgnore, JsonIgnore]
        public Type Contract
        {
            get => Type.GetType(this.ContractXml);
            set => this.ContractXml = value.AssemblyQualifiedName;
        }

        /// <summary>
        /// Gets or sets the address
        /// </summary>
        [XmlAttribute("address"), JsonProperty("address")]
        public String Address { get; set; }

        /// <summary>
        /// Gets the bindings 
        /// </summary>
        [XmlArray("behaviors"), XmlArrayItem("add"), JsonProperty("behaviors")]
        public List<RestBehaviorConfiguration> Behaviors { get; set; }

    }
}