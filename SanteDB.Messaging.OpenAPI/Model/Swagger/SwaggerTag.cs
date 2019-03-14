using Newtonsoft.Json;
using System.Xml.Serialization;
using System.Collections.Generic;
using System;

namespace SanteDB.Messaging.Metadata.Model.Swagger
{
    /// <summary>
    /// Represents a swagger tag
    /// </summary>
    [JsonObject(nameof(SwaggerTag))]
    public class SwaggerTag
    {

        /// <summary>
        /// Default constructor for the serialization
        /// </summary>
        public SwaggerTag()
        {

        }

        /// <summary>
        /// Creates a new swagger tag
        /// </summary>
        public SwaggerTag(String name, String description)
        {
            this.Name = name;
            this.Description = description;
        }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        [JsonProperty("description")]
        public String Description { get; set; }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [JsonProperty("name")]
        public String Name { get; set; }
    }
}