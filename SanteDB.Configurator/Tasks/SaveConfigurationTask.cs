using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Configurator.Tasks
{
    /// <summary>
    /// Represents a configuration task
    /// </summary>
    public class SaveConfigurationTask : IConfigurationTask
    {

        // The name of the backup file
        private String m_backupFile;

        /// <summary>
        /// Save the configuration 
        /// </summary>
        public string Name => "Save SanteDB Configuration";

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description => $"Writes the changes made to the configuration file to {ConfigurationContext.Current.ConfigurationFile}";

        /// <summary>
        /// Gets the feature
        /// </summary>
        public IFeature Feature => ConfigurationContext.Current.Features.OfType<CoreServiceFeatures>().First();

        /// <summary>
        /// Progress has changed
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Execute the configuration
        /// </summary>
        public bool Execute(SanteDBConfiguration configuration)
        {
            // First we backup the configuration
            this.m_backupFile = $"{ConfigurationContext.Current.ConfigurationFile}.bak.{DateTime.Now.ToString("yyyyMMddHHmmss")}";
            if(File.Exists(ConfigurationContext.Current.ConfigurationFile))
                File.Copy(ConfigurationContext.Current.ConfigurationFile, this.m_backupFile, true);

            using (var fs = File.OpenWrite(ConfigurationContext.Current.ConfigurationFile))
                configuration.Save(fs);
            return true;
        }

        /// <summary>
        /// Rollback the changes
        /// </summary>
        public bool Rollback(SanteDBConfiguration configuration)
        {
            if (File.Exists(this.m_backupFile))
            {
                File.Copy(this.m_backupFile, ConfigurationContext.Current.ConfigurationFile);
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Verify that this task can run
        /// </summary>
        public bool VerifyState(SanteDBConfiguration configuration)
        {
            return true;
        }
    }
}
