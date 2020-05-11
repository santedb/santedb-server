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
using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using System.Xml.Serialization;

namespace SanteDB.Authentication.OAuth2.Configuration
{
    /// <summary>
    /// OAuth2 configuration
    /// </summary>
    [XmlType(nameof(OAuthConfigurationSection), Namespace = "http://santedb.org/configuration")]
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
        /// Gets or sets whether the ACS will validate client claims
        /// </summary>
        [XmlArray("allowedClaims"), XmlArrayItem("add")]
        public List<String> AllowedClientClaims { get; set; }
        
        /// <summary>
        /// Issuer name
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
        [XmlElement("inetpub"), 
            Editor(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public String LoginAssetDir { get; set; }


        /// <summary>
        /// When true, allows login using client_credentials without any node authentication
        /// </summary>
        [XmlElement("allowNodelessClientAuth"), 
            Description("When enabled, allows clients to authenticate with client_credentials grant with no node authentication")]
        public bool AllowClientOnlyGrant { get; set; }
    }
}
