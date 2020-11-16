/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            this.Behaviors = new List<RestEndpointBehaviorConfiguration>();
        }

        /// <summary>
        /// Gets or sets the contract type
        /// </summary>
        [XmlAttribute("contract"), JsonProperty("contract"), Browsable(false)]
        public String ContractXml { get; set; }

        /// <summary>
        /// Gets or sets the Contract type
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [DisplayName("Service Contract"), Description("The service contract this endpoint implements")]
        [Browsable(true)]
        public Type Contract
        {
            get => this.ContractXml != null ? Type.GetType(this.ContractXml) : null;
            set => this.ContractXml = value?.AssemblyQualifiedName;
        }

        /// <summary>
        /// Gets or sets the address
        /// </summary>
        [XmlAttribute("address"), JsonProperty("address")]
        [DisplayName("Address"), Description("The address where the endpoint should accept messages")]
        public String Address { get; set; }

        /// <summary>
        /// Gets the bindings 
        /// </summary>
        [XmlArray("behaviors"), XmlArrayItem("add"), JsonProperty("behaviors")]
        [DisplayName("Endpoint Behaviors"), Description("The behaviors to attach to the endpoint")]
        public List<RestEndpointBehaviorConfiguration> Behaviors { get; set; }

    }
}