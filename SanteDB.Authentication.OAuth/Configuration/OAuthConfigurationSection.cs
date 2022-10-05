/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2022-5-30
 */
using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace SanteDB.Authentication.OAuth2.Configuration
{
    /// <summary>
    /// OAuth2 configuration
    /// </summary>
    [XmlType(nameof(OAuthConfigurationSection), Namespace = "http://santedb.org/configuration")]
    [ExcludeFromCodeCoverage]
    public class OAuthConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Creates a new instance of the OAuth configuration
        /// </summary>
        public OAuthConfigurationSection()
        {
            this.AllowedClientClaims = new List<string>();
        }

        /// <summary>
        /// Gets the name of the key that is used to sign session
        /// </summary>
        [XmlAttribute("jwtSigningKey")]
        public String JwtSigningKey { get; set; }

        /// <summary>
        /// Gets or sets whether the ACS will validate client claims
        /// </summary>
        [XmlArray("allowedClaims"), XmlArrayItem("add")]
        public List<String> AllowedClientClaims { get; set; }

        /// <summary>
        /// Issuer name. This corresponds to the iss claim in any issued tokens.
        /// </summary>
        [XmlAttribute("issuerName"), ConfigurationRequired]
        public String IssuerName { get; set; }

        /// <summary>
        /// Gets or sets the token type to use
        /// </summary>
        [XmlElement("tokenType")]
        public string TokenType { get; set; }

        /// <summary>
        /// Login asset directory
        /// </summary>
        [XmlElement("inetpub")]
        public String LoginAssetPath { get; set; }


        /// <summary>
        /// When true, allows login using client_credentials without any node authentication
        /// </summary>
        [XmlElement("allowNodelessClientAuth"),
            Description("When enabled, allows clients to authenticate with client_credentials grant with no node authentication")]
        public bool AllowClientOnlyGrant { get; set; }

        /// <summary>
        /// Which applet should be used for login assets on this instance.
        /// </summary>
        [XmlElement("assetSolution"), DisplayName("Login Assets Solution"), Description("When set, this solution's applet assets will be used to construct any pages related to authorization by the OAuth service. The LoginAssetDir setting overrides this. LEAVE BLANK TO USE THE DEFAULT ASSETS PROVIDED BY SANTEDB.")]
        public string LoginAssetSolution { get; set; }
    }
}
