using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Persistence.Diagnostics.OpenProject.Configuration
{
    /// <summary>
    /// Represents a configuration section for open project configuration
    /// </summary>
    [XmlType(nameof(OpenProjectConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class OpenProjectConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Gets or sets the API key
        /// </summary>
        [XmlElement("apiKey")]
        public String ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the API endpoint
        /// </summary>
        [XmlElement("apiEndpoint")]
        public String ApiEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the project on which to create the API objects
        /// </summary>
        [XmlElement("project")]
        public String ProjectKey { get; set; }

        /// <summary>
        /// Gets or sets the bug type to create
        /// </summary>
        [XmlElement("bugType")]
        public int BugType { get; set; }
    }
}
