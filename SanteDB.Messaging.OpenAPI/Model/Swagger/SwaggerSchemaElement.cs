using Newtonsoft.Json;
using SanteDB.Messaging.Metadata.Composer;
using System;
using System.Collections.Generic;
using System.Reflection;
using SanteDB.Core.Model;
using System.Linq;
using System.Xml.Serialization;
using System.Collections;

namespace SanteDB.Messaging.Metadata.Model.Swagger
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
        date,
        /// <summary>
        /// Data is a timespan
        /// </summary>
        timespan,
        /// <summary>
        /// Data is an array
        /// </summary>
        array,
        /// <summary>
        /// Data is an object
        /// </summary>
        @object
    }


    /// <summary>
    /// Represents a base class for swagger schema elements
    /// </summary>
    [JsonObject(nameof(SwaggerSchemaElement))]
    public class SwaggerSchemaElement
    {

        /// <summary>
        /// Type mapping
        /// </summary>
        internal static readonly Dictionary<Type, SwaggerSchemaElementType> m_typeMap = new Dictionary<Type, SwaggerSchemaElementType>()
        {
            {  typeof(int), SwaggerSchemaElementType.number },
            {  typeof(float), SwaggerSchemaElementType.number },
            {  typeof(double), SwaggerSchemaElementType.number },
            {  typeof(long), SwaggerSchemaElementType.number },
            {  typeof(short), SwaggerSchemaElementType.number },
            {  typeof(byte), SwaggerSchemaElementType.number },
            {  typeof(decimal), SwaggerSchemaElementType.number },
            {  typeof(DateTime), SwaggerSchemaElementType.date },
            {  typeof(DateTimeOffset), SwaggerSchemaElementType.date },
            {  typeof(TimeSpan), SwaggerSchemaElementType.timespan },
            {  typeof(bool), SwaggerSchemaElementType.boolean },
            {  typeof(String), SwaggerSchemaElementType.@string },
            {  typeof(Guid), SwaggerSchemaElementType.@string }
        };

        /// <summary>
        /// Create a new schema element
        /// </summary>
        public SwaggerSchemaElement()
        {

        }

        /// <summary>
        /// Copy a schema element
        /// </summary>
        public SwaggerSchemaElement(SwaggerSchemaElement copy)
        {
            this.Description = copy.Description;
            this.Type = copy.Type;
            this.Required = copy.Required;

            if(copy.Enum != null)
                this.Enum = new List<string>(copy.Enum);

            if (copy.Schema != null)
                this.Schema = new SwaggerSchemaDefinition(copy.Schema);
        }

        /// <summary>
        /// Create a schema element from a property
        /// </summary>
        public SwaggerSchemaElement(PropertyInfo property)
        {
            this.Description = MetadataComposerUtil.GetElementDocumentation(property);

            SwaggerSchemaElementType type = SwaggerSchemaElementType.@string;
            if (property.PropertyType.StripNullable().IsEnum)
            {
                this.Enum = property.PropertyType.StripNullable().GetFields().Select(f => f.GetCustomAttributes<XmlEnumAttribute>().FirstOrDefault()?.Name).Where(o => !string.IsNullOrEmpty(o)).ToList();
                this.Type = SwaggerSchemaElementType.@string;
            }
            else if(typeof(IList).IsAssignableFrom(property.PropertyType) || property.PropertyType.IsArray) // List or array {
            {
                this.Type = SwaggerSchemaElementType.array;

                Type elementType = null;
                if (property.PropertyType.IsArray)
                    elementType = property.PropertyType.GetElementType();
                else if(property.PropertyType.IsConstructedGenericType)
                    elementType = property.PropertyType.GetGenericArguments()[0];

                if(elementType == null || !m_typeMap.TryGetValue(elementType.StripNullable(), out type)) 
                    this.Items = new SwaggerSchemaDefinition()
                    {
                        Type = SwaggerSchemaElementType.@object,
                        Reference = elementType != null ? $"#/definitions/{MetadataComposerUtil.CreateSchemaReference(elementType)}" : null,
                        NetType = elementType
                    };
                else
                    this.Items = new SwaggerSchemaDefinition()
                    {
                        Type = type
                    };
            }
            else if (!m_typeMap.TryGetValue(property.PropertyType.StripNullable(), out type))
            {
                this.Schema = new SwaggerSchemaDefinition()
                {
                    Reference = $"#/definitions/{MetadataComposerUtil.CreateSchemaReference(property.PropertyType)}",
                    NetType = property.PropertyType
                };
            }
            else
                this.Type = type;

            // XML info
            var xmlElement = property.GetCustomAttributes<XmlElementAttribute>().FirstOrDefault();
            var xmlAttribute = property.GetCustomAttribute<XmlAttributeAttribute>();
            if (xmlElement != null)
                this.Xml = new SwaggerXmlInfo(xmlElement);
            else if (xmlAttribute != null)
                this.Xml = new SwaggerXmlInfo(xmlAttribute);

        }

        /// <summary>
        /// Gets or sets the XML information
        /// </summary>
        [JsonProperty("xml")]
        public SwaggerXmlInfo Xml { get; set; }

        /// <summary>
        /// Gets or sets the description 
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the type of the element
        /// </summary>
        [JsonProperty("type")]
        public SwaggerSchemaElementType? Type { get; set; }

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

        /// <summary>
        /// Items for reference
        /// </summary>
        [JsonProperty("items")]
        public SwaggerSchemaDefinition Items { get; set; }

    }
}