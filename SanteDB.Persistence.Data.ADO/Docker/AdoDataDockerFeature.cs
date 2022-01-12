/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */

using SanteDB.Core.Configuration;
using SanteDB.Docker.Core;
using SanteDB.Persistence.Data.ADO.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Docker
{
    /// <summary>
    /// ADO.NET Database Feature
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AdoDataDockerFeature : IDockerFeature
    {
        /// <summary>
        /// Readwrite connection
        /// </summary>
        public const string ConnectionStringSetting = "RW_CONNECTION";

        /// <summary>
        /// Readonly connection
        /// </summary>
        public const string ReadonlyConnectionSetting = "RO_CONNECTION";

        /// <summary>
        /// Fuzzy counts
        /// </summary>
        public const string FuzzyTotalSetting = "FUZZY_TOTAL";

        /// <summary>
        /// ID of the docker feature
        /// </summary>
        public string Id => "ADO";

        /// <summary>
        /// Gets the settings
        /// </summary>
        public IEnumerable<string> Settings => new String[] { FuzzyTotalSetting, ConnectionStringSetting, ReadonlyConnectionSetting };

        /// <summary>
        /// Configure the service
        /// </summary>
        public void Configure(SanteDBConfiguration configuration, IDictionary<string, string> settings)
        {
            var configSection = configuration.GetSection<AdoPersistenceConfigurationSection>();
            if (configSection == null)
            {
                configSection = new AdoPersistenceConfigurationSection()
                {
                    AutoInsertChildren = true,
                    AutoUpdateExisting = true,
                    PrepareStatements = true,
                    ProviderType = "Npgsql",
                    ReadonlyConnectionString = "MAIN",
                    ReadWriteConnectionString = "MAIN",
                    UseFuzzyTotals = false,
                    Validation = new EntityValidationFlags()
                    {
                        IdentifierFormat = true,
                        IdentifierUniqueness = true,
                        ValidationLevel = Core.BusinessRules.DetectedIssuePriorityType.Warning
                    }
                };
                configuration.AddSection(configSection);
            }

            if (settings.TryGetValue(ReadonlyConnectionSetting, out string roConnection))
            {
                configSection.ReadonlyConnectionString = roConnection;
            }

            if (settings.TryGetValue(ConnectionStringSetting, out string rwConnection))
            {
                configSection.ReadWriteConnectionString = rwConnection;
            }

            if (settings.TryGetValue(FuzzyTotalSetting, out string fuzzyTotal))
            {
                if (!Boolean.TryParse(fuzzyTotal, out bool fuzzyTotalBool))
                {
                    throw new ArgumentException($"{fuzzyTotal} is not a valid Boolean");
                }
                configSection.UseFuzzyTotals = fuzzyTotalBool;
            }

            // Service types
            var serviceTypes = new Type[] {
                typeof(SanteDB.Persistence.Data.ADO.Services.AdoApplicationIdentityProvider),
                typeof(SanteDB.Persistence.Data.ADO.Services.AdoDeviceIdentityProvider),
                typeof(SanteDB.Persistence.Data.ADO.Services.AdoIdentityProvider),
                typeof(SanteDB.Persistence.Data.ADO.Services.AdoSecurityChallengeProvider),
                typeof(SanteDB.Persistence.Data.ADO.Services.AdoSessionProvider),
                typeof(SanteDB.Persistence.Data.ADO.Services.AdoPolicyInformationService),
                typeof(SanteDB.Persistence.Data.ADO.Services.AdoRoleProvider),
                typeof(SanteDB.Persistence.Data.ADO.Services.AdoPersistenceService),
                typeof(SanteDB.Persistence.Data.ADO.Services.AdoSubscriptionExecutor),
                typeof(SanteDB.Persistence.Data.ADO.Services.AdoServiceFactory),
                typeof(SanteDB.Persistence.Data.ADO.Services.AdoFreetextSearchService)
              };
            // Add services
            var serviceConfiguration = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders;
            serviceConfiguration.AddRange(serviceTypes.Where(t => !serviceConfiguration.Any(c => c.Type == t)).Select(t => new TypeReferenceConfiguration(t)));
        }
    }
}