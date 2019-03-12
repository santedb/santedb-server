using Newtonsoft.Json;
using System.Collections.Generic;

namespace SanteDB.Messaging.OpenAPI.Model
{

    /// <summary>
    /// Represents the swagger parameter type
    /// </summary>
    public enum SwaggerSchemaElementType
    {
        /// <summary>
        /// Parameter is a string
        /// </summary>
        @string,
        /// <summary>
        /// Parmaeter is a number
        /// </summary>
        number,
        /// <summary>
        /// Parameter is a boolean
        /// </summary>
        boolean,
        /// <summary>
        /// Parameter is a date
        /// </summary>
        date
    }


    /// <summary>
    /// Represents a base class for swagger schema elements
    /// </summary>
    [JsonObject(nameof(SwaggerSchemaElement))]
    public class SwaggerSchemaElement
    {

        /// <summary>
        /// Gets or sets the description 
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the type of the element
        /// </summary>
        [JsonProperty("type")]
        public SwaggerSchemaElementType Type { get; set; }

        /// <summary>
        /// Gets whether the parameter is required
        /// </summary>
        [JsonProperty("required")]
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets the schema of the element
        /// </summary>
        [JsonProperty("schema")]
        public SwaggerSchemaDefinition Schema { get; set; }

        /// <summary>
        /// Enumerated types
        /// </summary>
        [JsonProperty("enum")]
        public List<string> Enum { get; set; }

    }
}