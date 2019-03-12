using Newtonsoft.Json;

namespace SanteDB.Messaging.OpenAPI.Model
{

    /// <summary>
    /// Represents the swagger parameter location
    /// </summary>
    public enum SwaggerParameterLocation
    {
        /// <summary>
        /// Location is in the body
        /// </summary>
        body, 
        /// <summary>
        /// Location is in the path
        /// </summary>
        path, 
        /// <summary>
        /// Location is in the query
        /// </summary>
        query
    }
    /// <summary>
    /// Represents the swagger parameter
    /// </summary>
    [JsonObject(nameof(SwaggerParameter))]
    public class SwaggerParameter : SwaggerSchemaElement
    {

        /// <summary>
        /// Gets or sets the name of the parameter
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets location of the parameter
        /// </summary>
        [JsonProperty("in")]
        public SwaggerParameterLocation Location { get; set; }


    }
}