using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Tasks;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Configurator.Tasks
{
    /// <summary>
    /// Represents a task that restarts the SanteDB configured service
    /// </summary>
    public class RestartServiceTask : IConfigurationTask
    {
        // Feature reference
        private WindowsServiceFeature m_feature;
        // Configuration references
        private WindowsServiceFeature.Options m_configuration;

        /// <summary>
        /// Restart the service task
        /// </summary>
        public RestartServiceTask()
        {
            this.m_feature = ConfigurationContext.Current.Features.OfType<WindowsServiceFeature>().First();
            this.m_configuration = this.m_feature.Configuration as WindowsServiceFeature.Options;
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        public string Name => $"Restart {this.m_configuration.ServiceName}";

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description => $"Restarts the {this.m_configuration.ServiceName} windows service";

        /// <summary>
        /// Gets the feature
        /// </summary>
        public IFeature Feature => this.m_feature;

        /// <summary>
        /// Progress has changed
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Execute the operation
        /// </summary>
        public bool Execute(SanteDBConfiguration configuration)
        {
            this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(0.0f, $"Stopping {this.m_configuration.ServiceName}"));
            ServiceTools.ServiceInstaller.StopService(this.m_configuration.ServiceName);
            this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(0.5f, $"Starting {this.m_configuration.ServiceName}"));
            ServiceTools.ServiceInstaller.StartService(this.m_configuration.ServiceName);
            this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(1.0f, null));
            return true;
        }

        /// <summary>
        /// Rollback the changes
        /// </summary>
        public bool Rollback(SanteDBConfiguration configuration)
        {
            return false;
        }

        /// <summary>
        /// Verify state
        /// </summary>
        public bool VerifyState(SanteDBConfiguration configuration)
        {
            return ServiceTools.ServiceInstaller.ServiceIsInstalled(this.m_configuration.ServiceName);
        }
    }
}
