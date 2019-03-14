using Newtonsoft.Json;
using System.Xml.Serialization;
using System.Collections.Generic;
using System;

namespace SanteDB.Messaging.Metadata.Model.Swagger
{

    /// <summary>
    /// Swagger security type
    /// </summary>
    public enum SwaggerSecurityType
    {
        basic,
        oauth2
    }

    /// <summary>
    /// Represents the swagger security flow
    /// </summary>
    public enum SwaggerSecurityFlow
    {
        password,
        client_credentials,
        authorization_code,
        refresh
    }

    /// <summary>
    /// Represents a security definition
    /// </summary>
    [JsonObject(nameof(SwaggerSecurityDefinition))]
    public class SwaggerSecurityDefinition 
    {

        /// <summary>
        /// Gets or sets the type
        /// </summary>
        [JsonProperty("type")]
        public SwaggerSecurityType Type { get; set; }

        /// <summary>
        /// Gets or sets the flow control
        /// </summary>
        [JsonProperty("flow")]
        public SwaggerSecurityFlow Flow { get; set; }

        /// <summary>
        /// Gets or sets the token url
        /// </summary>
        [JsonProperty("tokenUrl")]
        public String TokenUrl { get; set; }

        /// <summary>
        /// Gets or set sthe scopes
        /// </summary>
        [JsonProperty("scopes")]
        public Dictionary<String, String> Scopes { get; set; }


    }
}