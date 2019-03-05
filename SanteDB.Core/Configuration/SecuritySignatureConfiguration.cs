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
using System.Security.Cryptography.X509Certificates;
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
{

    /// <summary>
    /// Represents the type of signature algorithms
    /// </summary>
    [XmlType(nameof(SignatureAlgorithm), Namespace = "http://santedb.org/configuration")]
    public enum SignatureAlgorithm
    {
        [XmlEnum("rsa")]
        RS256,
        [XmlEnum("hmac")]
        HS256
    }

    /// <summary>
    /// Represents a signature collection
    /// </summary>
    [XmlType(nameof(SecuritySignatureConfiguration), Namespace = "http://santedb.org/configuration")]
    public class SecuritySignatureConfiguration : X509ConfigurationElement
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


    }
}