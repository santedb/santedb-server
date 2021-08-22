using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Tools.Debug.Configuration
{
    /// <summary>
    /// Represents debug tooling configuration
    /// </summary>
    [XmlType(nameof(DebugToolsConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class DebugToolsConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Metadata repository configuration
        /// </summary>
        [XmlElement("biFileRepository")]
        public FileMetaDataRepositoryConfiguration BiMetadataRepository { get; set; }

    }
}
