using Newtonsoft.Json;
using System;

namespace SanteDB.Authentication.OAuth2.Model
{
    /// <summary>
    /// OAuth token response
    /// </summary>
    [JsonObject]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Serialization class
    public class OAuthSessionResponse
    {

        /// <summary>
        /// Access token
        /// </summary>
        [JsonProperty("access_token")]
        public String AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the identity token
        /// </summary>
        [JsonProperty("id_token")]
        public String IdToken { get; set; }

        /// <summary>
        /// Token type
        /// </summary>
        [JsonProperty("token_type")]
        public String TokenType { get; set; }

        /// <summary>
        /// Expires in
        /// </summary>
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
