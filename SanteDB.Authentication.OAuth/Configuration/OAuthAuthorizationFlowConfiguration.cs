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
 * Date: 2018-6-22
 */
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Authentication.OAuth2.Configuration
{

    /// <summary>
    /// Identifies the OAuth flows 
    /// </summary>
    [XmlType(nameof(OAuthAuthorizationFlowType), Namespace = "http://santedb.org/configuration")]
    public enum OAuthAuthorizationFlowType
    {
        /// <summary>
        /// Identifies the client_credentials grant
        /// </summary>
        [XmlEnum(OAuthConstants.GrantNameClientCredentials)]
        ClientCredentials,
        /// <summary>
        /// Identifies the authorization_code grant
        /// </summary>
        [XmlEnum(OAuthConstants.GrantNameAuthorizationCode)]
        AuthorizationCode,
        /// <summary>
        /// Identifies the password grant
        /// </summary>
        [XmlEnum(OAuthConstants.GrantNamePassword)]
        Password,
        /// <summary>
        /// Identifies the refresh grant
        /// </summary>
        [XmlEnum(OAuthConstants.GrantNameRefresh)]
        Refresh
    }

    /// <summary>
    /// Represents a configuration for authorization flows
    /// </summary>
    [XmlType(nameof(OAuthAuthorizationFlowConfiguration), Namespace = "http://santedb.org/configuration")]
    public class OAuthAuthorizationFlowConfiguration
    {

        /// <summary>
        /// Gets or sets the flow type
        /// </summary>
        [XmlAttribute("flow")]
        public OAuthAuthorizationFlowType Flow { get; set; }

        /// <summary>
        /// Gets or sets the allowed clients of the flow
        /// </summary>
        [XmlArray("client_ids"), XmlArrayItem("add"), Description("Identifies the clients which are allowed to initiate the flow"), DisplayName("Allowed Clients")]
        public List<string> AllowedClients { get; set; }


    }
}