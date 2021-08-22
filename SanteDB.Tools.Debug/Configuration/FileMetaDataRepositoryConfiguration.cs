using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Tools.Debug.Configuration
{
    /// <summary>
    /// File based metadata repository configuration
    /// </summary>
    [XmlType(nameof(FileMetaDataRepositoryConfiguration), Namespace = "http://santedb.org/configuration")]
    public class FileMetaDataRepositoryConfiguration
    {

        /// <summary>
        /// Metadata repository
        /// </summary>
        [XmlArray("paths")]
        [XmlArrayItem("add")]
        [Description("Sets the base directory for the BI repository")]
        public List<string> Paths { get; set; }

        /// <summary>
        /// Rescan time
        /// </summary>
        [XmlAttribute("rescan")]
        [Description("The time between scans on the repository ")]
        public int RescanTime { get; set; }

    }
}