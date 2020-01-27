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
