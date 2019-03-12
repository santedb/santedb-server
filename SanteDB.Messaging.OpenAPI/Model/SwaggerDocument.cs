using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.OpenAPI.Model
{
    /// <summary>
    /// Represents the root swagger document
    /// </summary>
    [JsonObject(nameof(SwaggerDocument))]
    public class SwaggerDocument
    {
        /// <summary>
        /// Gets the version of the swagger document
        /// </summary>
        public SwaggerDocument()
        {
            this.Version = "2.0";
            this.Tags = new List<SwaggerTag>();
            this.Definitions = new Dictionary<String, SwaggerSchemaDefinition>();
            this.SecurityDefinitions = new Dictionary<String, SwaggerSecurityDefinition>();
            this.Paths = new Dictionary<string, SwaggerPath>();
        }

        /// <summary>
        /// Gets or sets the service info
        /// </summary>
        [JsonProperty("info")]
        public SwaggerServiceInfo Info { get; set; }

        /// <summary>
        /// Gets or sets the base-path
        /// </summary>
        [JsonProperty("basePath")]
        public String BasePath { get; set; }

        /// <summary>
        /// Gets or sets the version
        /// </summary>
        [JsonProperty("swagger")]
        public String Version { get; set; }

        /// <summary>
        /// Gets or sets the paths
        /// </summary>
        [JsonProperty("paths")]
        public Dictionary<String, SwaggerPath> Paths { get; set; }

        /// <summary>
        /// Gets or sets the definitions
        /// </summary>
        [JsonProperty("definitions")]
        public Dictionary<String, SwaggerSchemaDefinition> Definitions { get; set; }

        /// <summary>
        /// Gets or sets the security definitions
        /// </summary>
        [JsonProperty("securityDefinitions")]
        public Dictionary<String, SwaggerSecurityDefinition> SecurityDefinitions { get; set; }

        /// <summary>
        /// Gets or sets the tags
        /// </summary>
        [JsonProperty("tags")]
        public List<SwaggerTag> Tags { get; set; }

    }
}
