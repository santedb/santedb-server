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
using System.Security.Cryptography.X509Certificates;
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
{

    /// <summary>
    /// Represents the type of signature algorithms
    /// </summary>
    [XmlType(nameof(SignatureAlgorithm), Namespace = "http://santedb.org/configuration/security")]
    public enum SignatureAlgorithm
    {
        [XmlEnum("RSA_256")]
        RS256,
        [XmlEnum("HMAC_256")]
        HS256
    }

    /// <summary>
    /// Represents a signature collection
    /// </summary>
    [XmlType(nameof(SecuritySignatureConfigurationSection), Namespace = "http://santedb.org/configuration/security")]
    public class SecuritySignatureConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// The unique name for the signer
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Signature mode
        /// </summary>
        [XmlAttribute("alg")]
        public SignatureAlgorithm Algorithm { get; set; }

        /// <summary>
        /// When using HMAC256 signing this represents the server's secret
        /// </summary>
        [XmlAttribute("hmacKey")]
        public byte[] Secret { get; set; }

        /// <summary>
        /// The find type
        /// </summary>
        [XmlAttribute("findType")]
        public X509FindType FindType { get; set; }

        /// <summary>
        /// The store name
        /// </summary>
        [XmlAttribute("storeName")]
        public StoreName StoreName { get; set; }

        /// <summary>
        /// The store location
        /// </summary>
        [XmlAttribute("storeLocation")]
        public StoreLocation StoreLocation { get; set; }

        /// <summary>
        /// The find value
        /// </summary>
        [XmlAttribute("findValue")]
        public string FindValue { get; set; }

    }
}