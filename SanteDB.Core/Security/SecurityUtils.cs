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
using RestSrvr;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using System;
using System.IdentityModel.Tokens;
using System.Security;
using System.Security.Cryptography.X509Certificates;

namespace SanteDB.Core.Security
{
    /// <summary>
    /// Utilities for x509 certificates
    /// </summary>
    public static class SecurityUtils
	{

		private static Tracer s_tracer = Tracer.GetTracer(typeof(SecurityUtils));

        /// <summary>
        /// Create signing credentials
        /// </summary>
        public static SigningCredentials CreateSigningCredentials(SecuritySignatureConfiguration configuration)
        {
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

                    // If HS256 is used then the client_secret should be used as the secret
                    object secret = RestOperationContext.Current.Data.TryGetValue("symm_secret", out secret) ? secret : configuration.Secret;
                    if (((byte[])secret).Length < 16) // HACK: Requires 128 BIT key
                        secret = configuration.Secret;
                    retVal = new SigningCredentials(
                        new InMemorySymmetricSecurityKey((byte[])secret),
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

