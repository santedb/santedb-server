/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * User: fyfej
 * Date: 2022-5-30
 */
using SanteDB.Core.Configuration;
using SanteDB.Docker.Core;
using SanteDB.Persistence.Auditing.ADO.Configuration;
using SanteDB.Persistence.Auditing.ADO.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Auditing.ADO.Docker
{
    /// <summary>
    /// Configures the ADO Audit repository
    /// </summary>
    [ExcludeFromCodeCoverage]
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
