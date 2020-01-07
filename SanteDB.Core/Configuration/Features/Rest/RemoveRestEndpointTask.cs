using System;
using SanteDB.Core.Services;

namespace SanteDB.Core.Configuration.Rest
{
    /// <summary>
    /// Removes the specified REST endpoint task
    /// </summary>
    public class RemoveRestEndpointTask : IConfigurationTask
    {
        private RestServiceFeatureBase m_feature;

        /// <summary>
        /// Remove the rest endpoint service
        /// </summary>
        /// <param name="restServiceFeatureBase"></param>
        public RemoveRestEndpointTask(RestServiceFeatureBase restServiceFeatureBase)
        {
            this.m_feature = restServiceFeatureBase;
        }

        /// <summary>
        /// Gets the name of the service
        /// </summary>
        public string Name => $"Removes REST endpoint for {this.m_feature.Name}";

        /// <summary>
        /// Description
        /// </summary>
        public string Description => $"Adds the rest configuration feature for {this.m_feature.Name}";

        /// <summary>
        /// Gets the feature which this configures
        /// </summary>
        public IFeature Feature => this.m_feature;

        /// <summary>
        /// Fired when progress has changed
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        event EventHandler<ProgressChangedEventArgs> IReportProgressChanged.ProgressChanged
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