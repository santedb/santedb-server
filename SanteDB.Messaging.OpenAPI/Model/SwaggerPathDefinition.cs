using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SanteDB.Messaging.OpenAPI.Model
{
    /// <summary>
    /// Represents a swagger path definition
    /// </summary>
    [JsonObject(nameof(SwaggerPathDefinition))]
    public class SwaggerPathDefinition
    {

        /// <summary>
        /// Default ctor
        /// </summary>
        public SwaggerPathDefinition()
        {
            this.Tags = new List<string>();
            this.Produces = new List<string>();
            this.Consumes = new List<string>();
            this.Parameters = new List<SwaggerParameter>();
            this.Security = new List<SwaggerPathSecurity>();
            this.Responses = new Dictionary<int, SwaggerSchemaElement>();
        }

        /// <summary>
        /// Gets or sets the tags
        /// </summary>
        [JsonProperty("tags")]
        public List<String> Tags { get; set; }

        /// <summary>
        /// Gets or sets a summary description
        /// </summary>
        [JsonProperty("summary")]
        public String Summary { get; set; }

        /// <summary>
        /// Gets or sets the long form description
        /// </summary>
        [JsonProperty("description")]
        public String Description { get; set; }

        /// <summary>
        /// Gets or sets the produces option
        /// </summary>
        [JsonProperty("produces")]
        public List<String> Produces { get; set; }

        /// <summary>
        /// Gets or sets the consumption options
        /// </summary>
        [JsonProperty("consumes")]
        public List<String> Consumes { get; set; }

        /// <summary>
        /// Gets or sets the parameters 
        /// </summary>
        [JsonProperty("parameters")]
        public List<SwaggerParameter> Parameters { get; set; }

        /// <summary>
        /// Gets or sets the responses
        /// </summary>
        [JsonProperty("responses")]
        public Dictionary<Int32, SwaggerSchemaElement> Responses { get; set; }

        /// <summary>
        /// Gets or sets the security definition
        /// </summary>
        [JsonProperty("security")]
        public List<SwaggerPathSecurity> Security { get; set; }

    }
}