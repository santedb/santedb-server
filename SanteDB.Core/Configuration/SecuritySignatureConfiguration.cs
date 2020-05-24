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
using System.ComponentModel;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
{

    /// <summary>
    /// Represents the type of signature algorithms
    /// </summary>
    [XmlType(nameof(SignatureAlgorithm), Namespace = "http://santedb.org/configuration")]
    public enum SignatureAlgorithm
    {
        [XmlEnum("rs256")]
        RS256,
        [XmlEnum("hmac")]
        HS256,
        [XmlEnum("rs512")]
        RS512
    }

    /// <summary>
    /// Represents a signature collection
    /// </summary>
    [XmlType(nameof(SecuritySignatureConfiguration), Namespace = "http://santedb.org/configuration")]
    public class SecuritySignatureConfiguration : X509ConfigurationElement
    {

        // Algorithm
        private SignatureAlgorithm m_algorithm = SignatureAlgorithm.HS256;

        /// <summary>
        /// Gets or sets the key name
        /// </summary>
        [XmlAttribute("id")]
        [DisplayName("Key ID")]
        [Description("The identifier for the signature key")]
        public string KeyName { get; set; }

        /// <summary>
        /// The unique name for the signer
        /// </summary>
        [XmlAttribute("name")]
        [DisplayName("Issuer")]
        [Description("The name of the signature authority this represents")]
        public string IssuerName { get; set; }

        /// <summary>
        /// Signature mode
        /// </summary>
        [XmlAttribute("alg")]
        [DisplayName("Signing Algorithm")]
        [Description("The type of signature algorithm to use")]
        public SignatureAlgorithm Algorithm {
            get => this.m_algorithm;
            set {
                this.m_algorithm = value;
                this.FindTypeSpecified = this.StoreLocationSpecified = this.StoreNameSpecified = this.m_algorithm == SignatureAlgorithm.RS256;
                if(value == SignatureAlgorithm.HS256)
                {
                    this.FindValue = null;
                }
                else
                {
                    this.HmacSecret = null;
                }
            }
        }

        /// <summary>
        /// When using HMAC256 signing this represents the server's secret
        /// </summary>
        [XmlAttribute("hmacKey")]
        [DisplayName("HMAC256 Key")]
        [ReadOnly(true)]
        public byte[] Secret { get; set; }

        /// <summary>
        /// Plaintext editor for secret
        /// </summary>
        [XmlIgnore]
        [Description("When using HS256 signing the secret to use")]
        [DisplayName("HMAC256 Secret")]
        [PasswordPropertyText(true)]
        public string HmacSecret { get => "none"; set => this.Secret = value == null ? null : SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(value)); }

    }
}