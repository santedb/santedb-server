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
using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Services;
using SanteDB.Server.Core.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;

namespace SanteDB.Server.Core.Security
{
    /// <summary>
    /// Utilities for x509 certificates
    /// </summary>
    public static class SecurityUtils
	{

        // Security utils
		private static Tracer s_tracer = Tracer.GetTracer(typeof(SecurityUtils));

        // Signatoreu configuration
        private static ConcurrentDictionary<String, SecuritySignatureConfiguration> m_signatureConfiguration;

        /// <summary>
        /// Load from config
        /// </summary>
        static SecurityUtils ()
        {
            m_signatureConfiguration = new ConcurrentDictionary<string, SecuritySignatureConfiguration>(ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SanteDB.Core.Security.Configuration.SecurityConfigurationSection>().Signatures.ToDictionary(o => o.KeyName ?? "default", o => o));
        }

        /// <summary>
        /// Create signing credentials
        /// </summary>
        /// <param name="keyName">The name of the key to use in the configuration, or null to use a default key</param>
        public static SigningCredentials CreateSigningCredentials(string keyName)
        {

            if(!m_signatureConfiguration.TryGetValue(keyName ?? "default", out SecuritySignatureConfiguration configuration))
                return null;

            // No specific configuration for the key name is found?
            SigningCredentials retVal = null;

            // Signing credentials
            switch (configuration.Algorithm)
            {
                case SignatureAlgorithm.RS256:
                case SignatureAlgorithm.RS512:
                    var cert = configuration.Certificate;
                    if (cert == null)
                        throw new SecurityException("Cannot find certificate to sign data!");

                    // Signature algorithm
                    string signingAlgorithm = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256",
                        digestAlgorithm = "http://www.w3.org/2001/04/xmlenc#sha256";
                    if (configuration.Algorithm == SignatureAlgorithm.RS512)
                    {
                        signingAlgorithm = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512";
                        digestAlgorithm = "http://www.w3.org/2001/04/xmlenc#sha512";
                    }
                    retVal = new X509SigningCredentials(cert, new SecurityKeyIdentifier(new NamedKeySecurityKeyIdentifierClause("name", configuration.KeyName ?? "0")), signingAlgorithm, digestAlgorithm);
                    break;
                case SignatureAlgorithm.HS256:
                    byte[] secret = configuration.GetSecret().ToArray();
                    while (secret.Length < 16)
                        secret = secret.Concat(secret).ToArray();
                    retVal = new SigningCredentials(
                        new InMemorySymmetricSecurityKey(secret),
                        "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256",
                        "http://www.w3.org/2001/04/xmlenc#sha256",
                        new SecurityKeyIdentifier(new NamedKeySecurityKeyIdentifierClause("name", configuration.KeyName ?? "0"))
                    );
                    break;
                default:
                    throw new SecurityException("Invalid signing configuration");
            }
            return retVal;
        }

        /// <summary>
        /// Add signature credentials
        /// </summary>
        internal static void AddSigningCredentials(string keyId, byte[] keyData, string signatureAlgorithm)
        {
            keyId = keyId ?? "default";
            SecuritySignatureConfiguration configuration = null;
            switch(signatureAlgorithm)
            {
                case "HS256":
                    configuration = new SecuritySignatureConfiguration()
                    {
                        Algorithm = SignatureAlgorithm.HS256,
                        KeyName = keyId
                    };
                    configuration.SetSecret(keyData);
                    break;
                case "RS256":
                case "RS512":
                    var certificate = SecurityUtils.FindCertificate(X509FindType.FindByThumbprint, StoreLocation.LocalMachine, StoreName.My, BitConverter.ToString(keyData).Replace("-", ""));
                    if (certificate == null)
                        throw new KeyNotFoundException($"Cannot find specified X509 Certificate - Please ensure it is installed in the certificiate repository");
                    configuration = new SecuritySignatureConfiguration()
                    {
                        Algorithm = (SignatureAlgorithm)Enum.Parse(typeof(SignatureAlgorithm), signatureAlgorithm),
                        KeyName = keyId,
                        Certificate = certificate,
                        StoreName = StoreName.My,
                        StoreLocation = StoreLocation.LocalMachine,
                        FindType = X509FindType.FindByThumbprint,
                        StoreLocationSpecified = true,
                        StoreNameSpecified = true,
                        FindTypeSpecified = true
                    };
                    break;
            }

            // Now add them
            if (m_signatureConfiguration.TryGetValue(keyId, out SecuritySignatureConfiguration existing))
                throw new SecurityException($"Cannot register {keyId} again as it is already configured");
            else if (!m_signatureConfiguration.TryAdd(keyId, configuration))
                throw new InvalidOperationException($"Adding {keyId} failed");
        }

        /// <summary>
        /// Get key idetnifiers from configuration
        /// </summary>
        public static IEnumerable<string> GetKeyIdentifiers()
        {
            return m_signatureConfiguration.Keys;
        }

        /// <summary>
        /// Find a certiifcate from string values
        /// </summary>
        /// <returns>The certificate.</returns>
        /// <param name="findType">Find type.</param>
        /// <param name="storeLocation">Store location.</param>
        /// <param name="storeName">Store name.</param>
        /// <param name="findValue">Find value.</param>
        public static X509Certificate2 FindCertificate(
			String findType,
			String storeLocation,
			String storeName,
			String findValue)
		{
			X509FindType eFindType = X509FindType.FindByThumbprint;
			StoreLocation eStoreLocation = StoreLocation.CurrentUser;
			StoreName eStoreName = StoreName.My;

			if (!Enum.TryParse (findType, out eFindType))
				s_tracer.TraceWarning ("{0} not valid value for {1}, using {2} as default", findType, eFindType.GetType().Name, eFindType);
			
			if(!Enum.TryParse (storeLocation, out eStoreLocation))
				s_tracer.TraceWarning ("{0} not valid value for {1}, using {2} as default", storeLocation, eStoreLocation.GetType().Name, eStoreLocation);

			if(!Enum.TryParse (storeName, out eStoreName))
				s_tracer.TraceWarning ("{0} not valid value for {1}, using {2} as default", storeName, eStoreName.GetType().Name, eStoreName);

			return FindCertificate (eFindType, eStoreLocation, eStoreName, findValue);
		}

		/// <summary>
		/// Find the specified certificate
		/// </summary>
		/// <returns>The certificate.</returns>
		/// <param name="findType">Find type.</param>
		/// <param name="storeLocation">Store location.</param>
		/// <param name="storeName">Store name.</param>
		/// <param name="findValue">Find value.</param>
		public static X509Certificate2 FindCertificate(
			X509FindType findType,
			StoreLocation storeLocation,
			StoreName storeName,
			String findValue
		)
		{
			X509Store store = new X509Store(storeName, storeLocation);
			try {
				store.Open(OpenFlags.ReadOnly);
				var matches = store.Certificates.Find(findType, findValue, true);
				if(matches.Count == 0)
					throw new InvalidOperationException("Certificate not found");
				else if(matches.Count > 1)
					throw new InvalidOperationException("Too many matches");
				else
					return matches[0];
			} catch (Exception ex) {
				s_tracer.TraceError (ex.ToString ());
				return null;
			}
			finally {
				store.Close ();
			}
		}

	}
}

