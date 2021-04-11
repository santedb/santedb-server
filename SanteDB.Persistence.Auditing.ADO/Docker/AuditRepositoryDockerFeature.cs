﻿using SanteDB.Core.Configuration;
using SanteDB.Docker.Core;
using SanteDB.Persistence.Auditing.ADO.Configuration;
using SanteDB.Persistence.Auditing.ADO.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Auditing.ADO.Docker
{
    /// <summary>
    /// Configures the ADO Audit repository
    /// </summary>
    public class AuditRepositoryDockerFeature : IDockerFeature
    {

        /// <summary>
        /// Connection string setting
        /// </summary>
        public const string ConnectionStringSetting = "RW_CONNECTION";

        /// <summary>
        /// Identifier of the feature
        /// </summary>
        public string Id => "AUDIT_REPO";

        /// <summary>
        /// Settings
        /// </summary>
        public IEnumerable<string> Settings => new String[] { ConnectionStringSetting };

        /// <summary>
        /// Configure the setting
        /// </summary>
        public void Configure(SanteDBConfiguration configuration, IDictionary<string, string> settings)
        {
            var configSection = configuration.GetSection<AdoAuditConfigurationSection>();
            if (configSection == null)
            {
                configSection = new AdoAuditConfigurationSection()
                {
                    ProviderType = "Npgsql",
                    ReadonlyConnectionString = "AUDIT",
                    ReadWriteConnectionString = "AUDIT"
                };
                configuration.AddSection(configSection);
            }

            if (settings.TryGetValue(ConnectionStringSetting, out string rwConnection))
            {
                configSection.ReadonlyConnectionString = configSection.ReadWriteConnectionString = rwConnection;
            }

            // Add service for persisting
            var serviceConfiguration = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders;
            if (!serviceConfiguration.Any(s => s.Type == typeof(AdoAuditRepositoryService)))
            {
                serviceConfiguration.Add(new TypeReferenceConfiguration(typeof(AdoAuditRepositoryService)));
            }
        }

    }
}