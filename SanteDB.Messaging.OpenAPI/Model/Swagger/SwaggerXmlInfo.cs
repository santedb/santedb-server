using Newtonsoft.Json;
using System.Xml.Serialization;

namespace SanteDB.Messaging.Metadata.Model.Swagger
{
    /// <summary>
    /// Represents swagger xml information 
    /// </summary>
    [JsonObject(nameof(SwaggerXmlInfo))]
    public class SwaggerXmlInfo
    {

        /// <summary>
        /// Default ctor
        /// </summary>
        public SwaggerXmlInfo()
        {

        }

        /// <summary>
        /// Creates a new xml structure from xml attribute
        /// </summary>
        public SwaggerXmlInfo(XmlTypeAttribute typeInfo)
        {
            this.Namespace = typeInfo.Namespace;
        }

        /// <summary>
        /// Creates a new xml structure from element
        /// </summary>
        public SwaggerXmlInfo(XmlElementAttribute elementInfo)
        {
            this.Name = elementInfo.ElementName;
            this.Namespace = elementInfo.Namespace;
        }

        /// <summary>
        /// Creates new xml structure from attribute info
        /// </summary>
        public SwaggerXmlInfo(XmlAttributeAttribute attributeInfo)
        {

            this.IsAttribute = true;
            this.Name = attributeInfo.AttributeName;
            this.Namespace = attributeInfo.Namespace;
        }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Attribute
        /// </summary>
        [JsonProperty("attribute")]
        public bool IsAttribute { get; set; }

        /// <summary>
        /// Gets or sets the namespace
        /// </summary>
        [JsonProperty("namespace")]
        public string Namespace { get; set; }


    }
}