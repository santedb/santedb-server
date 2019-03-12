using Newtonsoft.Json;
using System.Xml.Serialization;
using System.Collections.Generic;
using System;

namespace SanteDB.Messaging.OpenAPI.Model
{
    /// <summary>
    /// Represents a swagger schema definition
    /// </summary>
    [JsonObject(nameof(SwaggerSchemaDefinition))]
    public class SwaggerSchemaDefinition
    {
        /// <summary>
        /// Default ctor
        /// </summary>
        public SwaggerSchemaDefinition()
        {
            this.Properties = new Dictionary<string, SwaggerSchemaElement>();
        }

        /// <summary>
        /// Gets or set a reference to another object
        /// </summary>
        [JsonProperty("$ref")]
        public string Reference { get; set; }

        /// <summary>
        /// Represents a property
        /// </summary>
        [JsonProperty("properties")]
        public Dictionary<String, SwaggerSchemaElement> Properties { get; set; }

        /// <summary>
        /// Represents an all-of relationship
        /// </summary>
        [JsonProperty("allOf")]
        public List<SwaggerSchemaDefinition> AllOf { get; set; }
    }
}