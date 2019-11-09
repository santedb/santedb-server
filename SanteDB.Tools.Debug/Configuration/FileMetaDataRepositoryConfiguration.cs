using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Tools.Debug.Configuration
{
    /// <summary>
    /// File based metadata repository configuration
    /// </summary>
    [XmlType(nameof(FileMetaDataRepositoryConfiguration), Namespace = "http://santedb.org")]
    public class FileMetaDataRepositoryConfiguration
    {

        /// <summary>
        /// Metadata repository
        /// </summary>
        [XmlAttribute("path")]
        [Description("Sets the base directory for the BI repository")]
        public string BasePath { get; set; }

        /// <summary>
        /// Rescan time
        /// </summary>
        [XmlAttribute("rescan")]
        [Description("The time between scans on the repository ")]
        public int RescanTime { get; set; }

    }
}