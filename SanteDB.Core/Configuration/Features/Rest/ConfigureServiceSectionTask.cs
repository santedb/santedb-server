using System;
using System.ComponentModel;
using SanteDB.Core.Services;

namespace SanteDB.Core.Configuration.Rest
{
    /// <summary>
    /// Enables configuration of the service section
    /// </summary>
    public class ConfigureServiceSectionTask : IConfigurationTask
    {
        // The feature being configured
        private RestServiceFeatureBase m_feature;

        /// <summary>
        /// Creates a new configuration service task
        /// </summary>
        public ConfigureServiceSectionTask(RestServiceFeatureBase restServiceFeatureBase)
        {
            this.m_feature = restServiceFeatureBase;
        }

        /// <summary>
        /// Gets the name of the service
        /// </summary>
        public string Name => $"Configure {this.m_feature.Name}";

        /// <summary>
        /// Description
        /// </summary>
        public string Description => $"Configures the service {this.m_feature.Name}";

        /// <summary>
        /// Gets the feature which this configures
        /// </summary>
        public IFeature Feature => this.m_feature;

        /// <summary>
        /// Fired when progress has changed
        /// </summary>
        public event EventHandler<Services.ProgressChangedEventArgs> ProgressChanged;

        event EventHandler<Services.ProgressChangedEventArgs> IReportProgressChanged.ProgressChanged
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        public bool Execute(SanteDBConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        public bool Rollback(SanteDBConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        public bool VerifyState(SanteDBConfiguration configuration)
        {
            throw new NotImplementedException();
        }
    }
}