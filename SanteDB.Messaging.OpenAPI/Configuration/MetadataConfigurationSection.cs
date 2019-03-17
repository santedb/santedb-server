using Newtonsoft.Json;
using SanteDB.Core.Configuration;
using SanteDB.Core.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.Metadata.Configuration
{
    /// <summary>
    /// Represents the configuration for the OpenApi
    /// </summary>
    [XmlType(nameof(MetadataConfigurationSection), Namespace = "http://santedb.org/configuration")]
    [JsonObject(nameof(MetadataConfigurationSection))]
    public class MetadataConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Gets or sets the service contracts to document
        /// </summary>
        [XmlArray("services"), XmlArrayItem("add"), JsonProperty("services")]
        public List<ServiceEndpointOptions> Services { get; set; }

        /// <summary>
        /// Gets or sets the default host to apply
        /// </summary>
        [XmlElement("host"), JsonProperty("host")]
        public String ApiHost { get; set; }
    }
}
