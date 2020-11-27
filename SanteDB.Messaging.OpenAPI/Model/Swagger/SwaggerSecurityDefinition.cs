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
using System.Xml.Serialization;
using System.Collections.Generic;
using System;

namespace SanteDB.Messaging.Metadata.Model.Swagger
{

    /// <summary>
    /// Swagger security type
    /// </summary>
    public enum SwaggerSecurityType
    {
        basic,
        oauth2
    }

    /// <summary>
    /// Represents the swagger security flow
    /// </summary>
    public enum SwaggerSecurityFlow
    {
        password,
        client_credentials,
        authorization_code,
        refresh
    }

    /// <summary>
    /// Represents a security definition
    /// </summary>
    [JsonObject(nameof(SwaggerSecurityDefinition))]
    public class SwaggerSecurityDefinition 
    {

        /// <summary>
        /// Gets or sets the type
        /// </summary>
        [JsonProperty("type")]
        public SwaggerSecurityType Type { get; set; }

        /// <summary>
        /// Gets or sets the flow control
        /// </summary>
        [JsonProperty("flow")]
        public SwaggerSecurityFlow Flow { get; set; }

        /// <summary>
        /// Gets or sets the token url
        /// </summary>
        [JsonProperty("tokenUrl")]
        public String TokenUrl { get; set; }

        /// <summary>
        /// Gets or set sthe scopes
        /// </summary>
        [JsonProperty("scopes")]
        public Dictionary<String, String> Scopes { get; set; }


    }
}