using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace SanteDB.Messaging.OpenAPI.Model
{
    [JsonObject(nameof(SwaggerServiceInfo))]
    public class SwaggerServiceInfo
    {

        /// <summary>
        /// Gets or sets the title of the API
        /// </summary>
        [JsonProperty("title")]
        public String Title { get; set; }

        /// <summary>
        /// Gets or sets the definition
        /// </summary>
        [JsonProperty("description")]
        public String Description { get; set; }

        /// <summary>
        /// Gets or sets the version
        /// </summary>
        [JsonProperty("version")]
        public String Version { get; set; }
    }
}