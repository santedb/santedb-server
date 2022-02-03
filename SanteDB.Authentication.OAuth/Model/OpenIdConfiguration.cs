/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Authentication.OAuth2.Model
{
    /// <summary>
    /// Serialized open id configuration
    /// </summary>
    [JsonObject(nameof(OpenIdConfiguration))]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Serialization class
    public class OpenIdConfiguration
    {
        /// <summary>
        /// Gets or sets the issuer of the token
        /// </summary>
        [JsonProperty("issuer")]
        public string Issuer { get; set; }

        /// <summary>
        /// Gets or sets the auth endont
        /// </summary>
        [JsonProperty("authorization_endpoint")]
        public string AuthorizationEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the token endpoint
        /// </summary>
        [JsonProperty("token_endpoint")]
        public string TokenEndpoint { get; set; }

        /// <summary>
        /// Get the user information endpoint
        /// </summary>
        [JsonProperty("userinfo_endpoint")]
        public String UserInfoEndpoint { get; set; }

        /// <summary>
        /// The JWKS URI
        /// </summary>
        [JsonProperty("jwks_uri")]
        public string SigningKeyEndpoint { get; set; }

        /// <summary>
        /// Gets the scopes supported
        /// </summary>
        [JsonProperty("scopes_supported")]
        public List<String> ScopesSupported { get; set; }

        /// <summary>
        /// Gets or sets the response types supported
        /// </summary>
        [JsonProperty("response_types_supported")]
        public List<String> ResponseTypesSupported { get; set; }

        /// <summary>
        /// Grant types supported
        /// </summary>
        [JsonProperty("grant_types_supported")]
        public List<String> GrantTypesSupported { get; set; }

        /// <summary>
        /// Gets the subject types supported
        /// </summary>
        [JsonProperty("subject_types_supported")]
        public List<String> SubjectTypesSupported { get; set; }

        /// <summary>
        /// Gets the signing algorithms
        /// </summary>
        [JsonProperty("id_token_signing_alg_values_supported")]
        public List<String> IdTokenSigning { get; set; }
        
    }
}
