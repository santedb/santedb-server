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
using SanteDB.Core.Security;
using SanteDB.Core.Security.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
{
    /// <summary>
    /// Represents claims authorization configuration
    /// </summary>
    [XmlType(nameof(ClaimsAuthorizationConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class ClaimsAuthorizationConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Creates a new claims
        /// </summary>
        public ClaimsAuthorizationConfigurationSection()
        {
            this.Audiences = new ObservableCollection<string>();
        }

        /// <summary>
        /// Custom validator
        /// </summary>
        [XmlAttribute("customValidator")]
        public String CustomValidatorXml { get; set; }

        /// <summary>
        /// Custom validator type
        /// </summary>
        [XmlIgnore]
        public Type CustomValidator {
            get => Type.GetType(this.CustomValidatorXml);
            set => this.CustomValidatorXml = value?.AssemblyQualifiedName;
        }

        // Keys
        private Dictionary<String, SecurityKey> m_keys;

        /// <summary>
        /// Issuer keys
        /// </summary>
        [XmlIgnore]
        public Dictionary<String, SecurityKey> IssuerKeys
        {
            get
            {
                if(this.m_keys == null)
                    this.m_keys = this.IssuerKeysXml.ToDictionary(
                        o => o.IssuerName,
                        o => o.Algorithm == SignatureAlgorithm.HS256 ?
                            (SecurityKey)new InMemorySymmetricSecurityKey(o.GetSecret()) :
                            new X509AsymmetricSecurityKey(o.Certificate)
                        );
                return this.m_keys;
            }
        }

        /// <summary>
        /// Represents the issuer key 
        /// </summary>
        [XmlArray("issuer"), XmlArrayItem("add")]
        public List<SecuritySignatureConfiguration> IssuerKeysXml { get; set; }

        /// <summary>
        /// Gets or sets the allowed audiences 
        /// </summary>
        [XmlArray("audiences"), XmlArrayItem("add")]
        public ObservableCollection<String> Audiences { get; set; }

        /// <summary>
        /// Gets or sets the realm
        /// </summary>
        [XmlAttribute("realm")]
        public string Realm { get; set; }


        /// <summary>
        /// Convert this to a STS handler config
        /// </summary>
        public TokenValidationParameters ToConfigurationObject()
        {

            TokenValidationParameters retVal = new TokenValidationParameters();

            retVal.ValidIssuers = this.IssuerKeys.Select(o => o.Key);
            retVal.RequireExpirationTime = true;
            retVal.RequireSignedTokens = true;
            retVal.ValidAudiences = this.Audiences;
            retVal.ValidateLifetime = true;
            retVal.ValidateIssuerSigningKey = true;
            retVal.ValidateIssuer = true;
            retVal.ValidateAudience = true;
            retVal.IssuerSigningTokens = this.IssuerKeys.Where(o => o.Value is X509SecurityKey).Select(o => new X509SecurityToken((o.Value as X509SecurityKey).Certificate));
            retVal.IssuerSigningKeys = this.IssuerKeys.Select(o => o.Value);
            retVal.IssuerSigningKeyResolver = (s, securityToken, identifier, parameters) =>
            {

                if (identifier.Count > 0)
                    return identifier.Select(o =>
                    {
                        // Lookup by thumbprint
                        SecurityKey candidateKey = null;

                        if (o is X509ThumbprintKeyIdentifierClause)
                                candidateKey = this.IssuerKeys.SingleOrDefault(ik => (ik.Value as X509SecurityKey).Certificate.Thumbprint == BitConverter.ToString((o as X509ThumbprintKeyIdentifierClause).GetX509Thumbprint()).Replace("-","")).Value;

                        return candidateKey;
                    }).First(o => o != null);
                else
                {
                    SecurityKey candidateKey = null;
                    this.IssuerKeys.TryGetValue((securityToken as JwtSecurityToken).Issuer, out candidateKey);
                    return candidateKey;
                }
            };
           
            // Custom validator
            if (this.CustomValidator != null)
            {
                ConstructorInfo ci = this.CustomValidator.GetConstructor(Type.EmptyTypes);
                if (ci == null)
                    throw new ConfigurationException("No constructor found for custom validator");
                retVal.CertificateValidator = ci.Invoke(null) as X509CertificateValidator;
            }

            
            return retVal;
        }
    }
}