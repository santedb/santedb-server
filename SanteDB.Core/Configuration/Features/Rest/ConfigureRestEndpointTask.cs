using System;
using SanteDB.Core.Configuration;
using SanteDB.Core.Services;

namespace SanteDB.Core.Configuration.Rest
{
    /// <summary>
    /// Configures the rest endpoint in the SanteDB configuration subsystem
    /// </summary>
    public class ConfigureRestEndpointTask : IConfigurationTask
    {
        // The feature which is to be configured
        private RestServiceFeatureBase m_feature;

        /// <summary>
        /// Creates a new installation task
        /// </summary>
        /// <param name="restServiceFeatureBase">The feature for which the service is being configured</param>
        public ConfigureRestEndpointTask(RestServiceFeatureBase restServiceFeatureBase)
        {
            this.m_feature = restServiceFeatureBase;
        }

        /// <summary>
        /// Gets the name of the service
        /// </summary>
        public string Name => $"Enable REST {this.m_feature.Name}";

        /// <summary>
        /// Description
        /// </summary>
        public string Description => $"Remove the rest configuration feature for {this.m_feature.Name}";

        /// <summary>
        /// Gets the feature which this configures
        /// </summary>
        public IFeature Feature => this.m_feature;

        /// <summary>
        /// Fired when progress has changed
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Executes the configuration
        /// </summary>
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
            return true;
        }
    }
}