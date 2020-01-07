using System;
using SanteDB.Core.Services;

namespace SanteDB.Core.Configuration.Rest
{
    /// <summary>
    /// Represents a task which removes the service section
    /// </summary>
    public class RemoveServiceSectionTask : IConfigurationTask
    {
        private RestServiceFeatureBase m_feature;

        /// <summary>
        /// Creates a new remove service task function
        /// </summary>
        public RemoveServiceSectionTask(RestServiceFeatureBase restServiceFeatureBase)
        {
            this.m_feature = restServiceFeatureBase;
        }

        /// <summary>
        /// Gets the name of the service
        /// </summary>
        public string Name => $"Removes service configuration for {this.m_feature.Name}";

        /// <summary>
        /// Description
        /// </summary>
        public string Description => $"Removes the service configuration for {this.m_feature.Name}";

        /// <summary>
        /// Gets the feature which this configures
        /// </summary>
        public IFeature Feature => this.m_feature;

        /// <summary>
        /// Fired when progress has changed
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