using SanteDB.Core.Services.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Configuration.Features
{
    /// <summary>
    /// Represents the local applet manager feature
    /// </summary>
    public class LocalAppletManagerFeature : GenericServiceFeature<LocalAppletManagerService>
    {

        /// <summary>
        /// Create a new local applet manager 
        /// </summary>
        public LocalAppletManagerFeature()
        {
            this.Configuration = new AppletConfigurationSection();
        }

        /// <summary>
        /// Create installation tasks
        /// </summary>
        public override IEnumerable<IConfigurationTask> CreateInstallTasks()
        {
            var conf = this.Configuration as AppletConfigurationSection;
            if (conf.TrustedPublishers.Count == 0)
                conf.TrustedPublishers.AddRange(new String[]
                    {
                        "82C63E1E9B87578D0727E871D7613F2F0FAF683B", // SanteDB APPCA Signature (must be installed)
                        "4326A4421216AC254DA93DC61B93160B08925BB1" // SanteDB Community Applications
                    });
            return base.CreateInstallTasks();
        }
        /// <summary>
        /// Auto-setup the applet features
        /// </summary>
        public override FeatureFlags Flags => FeatureFlags.AutoSetup;
    }
}
