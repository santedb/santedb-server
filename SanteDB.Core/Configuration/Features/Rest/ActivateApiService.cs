using System;
using SanteDB.Core.Services;

namespace SanteDB.Core.Configuration.Rest
{
    /// <summary>
    /// Create a task which activates the api service
    /// </summary>
    public class ActivateApiService : IConfigurationTask
    {
        private RestServiceFeatureBase m_feature;

        /// <summary>
        /// Creates a new service activation task
        /// </summary>
        public ActivateApiService(RestServiceFeatureBase restServiceFeatureBase)
        {
            this.m_feature = restServiceFeatureBase;
        }


        /// <summary>
        /// Gets the name of the service
        /// </summary>
        public string Name => $"Activate Daemon for {this.m_feature.Name}";

        /// <summary>
        /// Description
        /// </summary>
        public string Description => $"Activates the service daemon for {this.m_feature.Name}";

        /// <summary>
        /// Gets the feature which this configures
        /// </summary>
        public IFeature Feature => this.m_feature;

        /// <summary>
        /// Progress has changed
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

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