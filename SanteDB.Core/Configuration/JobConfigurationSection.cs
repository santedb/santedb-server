using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
{
    /// <summary>
    /// Represents a simple job configuration
    /// </summary>
    [XmlType(nameof(JobConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class JobConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Add job
        /// </summary>
        [XmlArray("jobs"), XmlArrayItem("add")]
        public List<JobItemConfiguration> Jobs { get; set; }

    }

    /// <summary>
    /// Represents the configuration of a single job
    /// </summary>
    public class JobItemConfiguration
    {

        /// <summary>
        /// The type as expressed in XML
        /// </summary>
        [XmlAttribute("type")]
        public String TypeXml { get; set; }

        /// <summary>
        /// Gets or sets the job type
        /// </summary>
        [XmlIgnore]
        public Type Type {
            get => Type.GetType(this.TypeXml);
            set => this.TypeXml = value?.AssemblyQualifiedName;
        }

        /// <summary>
        /// Gets or sets the timeout of the job
        /// </summary>
        [XmlAttribute("interval")]
        public int Interval { get; set; }
    }
}
