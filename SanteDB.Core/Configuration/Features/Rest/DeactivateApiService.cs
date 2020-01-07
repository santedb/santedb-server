using System;
using SanteDB.Core.Services;

namespace SanteDB.Core.Configuration.Rest
{
    /// <summary>
    /// De-activate the API service daemon
    /// </summary>
    public class DeactivateApiService : IConfigurationTask
    {
        private RestServiceFeatureBase m_feature;

        /// <summary>
        /// Creates a new deactivation of the api daemon
        /// </summary>
        /// <param name="restServiceFeatureBase"></param>
        public DeactivateApiService(RestServiceFeatureBase restServiceFeatureBase)
        {
            this.m_feature = restServiceFeatureBase;
        }


        /// <summary>
        /// Gets the name of the service
        /// </summary>
        public string Name => $"De-Activate Daemon for {this.m_feature.Name}";

        /// <summary>
        /// Description
        /// </summary>
        public string Description => $"Removes and de-activates the service daemon for {this.m_feature.Name}";

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