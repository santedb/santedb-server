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
using System;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
{
    /// <summary>
    /// SanteDB Security configuration
    /// </summary>
    [XmlType(nameof(SecurityConfigurationSection), Namespace = "http://santedb.org/configuration/security")]
    public class SecurityConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Password regex
        /// </summary>
        [XmlAttribute("passwordRegex")]
        public string PasswordRegex { get; set; }

        /// <summary>
        /// Allow unsigned applets to be installed
        /// </summary>
        [XmlAttribute("allowUnsignedApplets")]
        public bool AllowUnsignedApplets { get; set; }

        /// <summary>
        /// Basic authentication configuration
        /// </summary>
        [XmlElement("basicAuth")]
        public SanteDBServerConfiguration BasicAuth { get; set; }

        /// <summary>
        /// Gets or sets the claims auth
        /// </summary>
        [XmlElement("claimsAuth")]
        public ClaimsAuthorizationConfigurationSection ClaimsAuth { get; set; }

        /// <summary>
        /// Trusted publishers
        /// </summary>
        [XmlArray("trust"), XmlArrayItem("add")]
        public ObservableCollection<string> TrustedPublishers { get; set; }

        /// <summary>
        /// Signature configuration
        /// </summary>
        [XmlElement("signing")]
        public SecuritySignatureConfigurationSection Signatures { get; set; }

        
    }
}