using SanteDB.Core.Configuration;
using SanteDB.Core.PubSub.Broker;
using SanteDB.Docker.Core;
using SanteDB.Persistence.PubSub.ADO.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.PubSub.ADO.Docker
{
    /// <summary>
    /// ADO PubSub feature
    /// </summary>
    public class PubSubDockerFeature : IDockerFeature
    {

        /// <summary>
        /// Connection string setting
        /// </summary>
        public const string ConnectionStringSetting = "RW_CONNECTION";

        /// <summary>
        /// Identifier of the feature
        /// </summary>
        public string Id => "PUBSUB_ADO";

        /// <summary>
        /// Settings
        /// </summary>
        public IEnumerable<string> Settings => new String[] { ConnectionStringSetting };

        /// <summary>
        /// Configure the setting
        /// </summary>
        public void Configure(SanteDBConfiguration configuration, IDictionary<string, string> settings)
        {
            var configSection = configuration.GetSection<AdoPubSubConfigurationSection>();
            if (configSection == null)
            {
                configSection = new AdoPubSubConfigurationSection()
                {
                    ProviderType = "Npgsql",
                    ReadonlyConnectionString = "MAIN",
                    ReadWriteConnectionString = "MAIN"
                };
                configuration.AddSection(configSection);
            }

            if (settings.TryGetValue(ConnectionStringSetting, out string rwConnection))
            {
                configSection.ReadonlyConnectionString = configSection.ReadWriteConnectionString = rwConnection;
            }

            // Add service for persisting
            var serviceConfiguration = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders;
            if (!serviceConfiguration.Any(s => s.Type == typeof(AdoPubSubManager)))
            {
                serviceConfiguration.Add(new TypeReferenceConfiguration(typeof(AdoPubSubManager)));
            }
            // Add broker
            if (!serviceConfiguration.Any(s => s.Type == typeof(PubSubBroker)))
            {
                serviceConfiguration.Add(new TypeReferenceConfiguration(typeof(PubSubBroker)));
            }
        }
    }
}
