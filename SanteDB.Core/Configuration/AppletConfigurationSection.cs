using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
{
    /// <summary>
    /// Represents a local applet configuration section
    /// </summary>
    [XmlType(nameof(AppletConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class AppletConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Applet configuration section
        /// </summary>
        public AppletConfigurationSection()
        {
            this.AppletDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "applets");
            this.TrustedPublishers = new ObservableCollection<string>()
            {
                "82C63E1E9B87578D0727E871D7613F2F0FAF683B", // SanteDB APPCA Signature (must be installed)
                "4326A4421216AC254DA93DC61B93160B08925BB1" // SanteDB Community Applications
            };

#if DEBUG
            this.AllowUnsignedApplets = true;
#endif
        }

        /// <summary>
        /// Gets or sets the directory for applets to be loaded from
        /// </summary>
        [XmlAttribute("appletDirectory")]
        public String AppletDirectory { get; set; }

        /// <summary>
        /// Allow unsigned applets to be installed
        /// </summary>
        [XmlAttribute("allowUnsignedApplets")]
        public bool AllowUnsignedApplets { get; set; }

        /// <summary>
        /// Trusted publishers
        /// </summary>
        [XmlArray("trustedPublishers"), XmlArrayItem("add")]
        public ObservableCollection<string> TrustedPublishers { get; set; }

    }
}
