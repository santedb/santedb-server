using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Persistence.Diagnostics.Trello.Configuration
{
    /// <summary>
    /// Trello configuration section
    /// </summary>
    [XmlType(nameof(TrelloConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class TrelloConfigurationSection : IEncryptedConfigurationSection
    {

        /// <summary>
        /// API key for authentication
        /// </summary>
        [XmlElement("api-key")]
        public string ApiKey { get; set; }

        /// <summary>
        /// API token
        /// </summary>
        [XmlElement("api-token")]
        public string ApiToken { get; set; }

        /// <summary>
        /// Gets or sets the board where tickets should be created
        /// </summary>
        [XmlElement("board-name")]
        public String BoardName { get; set; }

        /// <summary>
        /// Gets or sets the list where cards should be created
        /// </summary>
        [XmlElement("list-name")]
        public String ListName { get; set; }

        /// <summary>
        /// Gets or sets the labels to add
        /// </summary>
        [XmlArray("labels"), XmlArrayItem("add")]
        public List<String> Labels { get; set; }
    }
}
