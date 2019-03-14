using Newtonsoft.Json;
using System.Xml.Serialization;
using System.Collections.Generic;
using System;
using System.Linq;
using SanteDB.Messaging.Metadata.Composer;
using System.Reflection;
using SanteDB.Core.Model;

namespace SanteDB.Messaging.Metadata.Model.Swagger
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
        /// Copy constructor
        /// </summary>
        public SwaggerSchemaDefinition(SwaggerSchemaDefinition copy) : this()
        {
            this.Type = copy.Type;
            this.Reference = copy.Reference;
            this.Description = copy.Description;
            if (copy.Properties != null)
                this.Properties = copy.Properties.ToDictionary(o => o.Key, o => new SwaggerSchemaElement(o.Value));

            if (copy.AllOf != null)
                this.AllOf = copy.AllOf.Select(o => new SwaggerSchemaDefinition(o)).ToList();

            this.NetType = copy.NetType;
        }

        /// <summary>
        /// Create a schema definition based on a type
        /// </summary>
        public SwaggerSchemaDefinition(Type schemaType)
        {
            this.Description = MetadataComposerUtil.GetElementDocumentation(schemaType, MetaDataElementType.Summary);
            this.NetType = schemaType;
            this.Type = SwaggerSchemaElementType.@object;

            this.Properties = schemaType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetCustomAttributes<XmlElementAttribute>().Any() || p.GetCustomAttribute<JsonPropertyAttribute>() != null)
                .ToDictionary(o => o.GetSerializationName(), o => new SwaggerSchemaElement(o));
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

        /// <summary>
        /// Gets the .net datatype
        /// </summary>
        [JsonIgnore]
        public Type NetType { get; set; }

        /// <summary>
        /// Gets the description of the datatype
        /// </summary>
        [JsonProperty("description")]
        public String Description { get; set; }

        /// <summary>
        /// Gets the type
        /// </summary>
        [JsonProperty("type")]
        public SwaggerSchemaElementType Type { get; internal set; }
    }
}