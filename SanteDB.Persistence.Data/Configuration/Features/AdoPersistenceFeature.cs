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
 * Date: 2022-9-7
 */
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using SanteDB.Core.Services;
using SanteDB.OrmLite.Configuration;
using SanteDB.OrmLite.Migration;
using SanteDB.Persistence.Data.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Configuration.Features
{
    /// <summary>
    /// Represents an ADO persistence service
    /// </summary>
    public class AdoPersistenceFeature : GenericServiceFeature<AdoPersistenceService>
    {
        /// <summary>
        /// Set the default configuration
        /// </summary>
        public AdoPersistenceFeature() : base()
        {
        }

        /// <summary>
        /// Create installation tasks
        /// </summary>
        public override IEnumerable<IConfigurationTask> CreateInstallTasks()
        {
            return base.CreateInstallTasks().Union(new IConfigurationTask[] {
                new RegisterAdoProvidersTask(this)
            });
        }

        /// <summary>
        /// ADO Configuration service
        /// </summary>
        public override Type ConfigurationType => typeof(AdoPersistenceConfigurationSection);

        /// <summary>
        /// Group this feature belongs to
        /// </summary>
        public override string Group => FeatureGroup.Persistence;

        /// <summary>
        /// Register ADO service providers
        /// </summary>
        private class RegisterAdoProvidersTask : IConfigurationTask
        {
            private readonly Type[] SERVICE_TYPES = new Type[]
            {
                typeof(AdoPersistenceService),
                typeof(AdoApplicationIdentityProvider),
                typeof(AdoDeviceIdentityProvider),
                typeof(AdoFreetextSearchService),
                typeof(AdoIdentityProvider),
                typeof(AdoPolicyInformationService),
                typeof(AdoApplicationIdentityProvider),
                typeof(AdoDatasetInstallerService),
                typeof(AdoSecurityChallengeProvider),
                typeof(AdoRoleProvider),
                typeof(AdoSubscriptionExecutor),
                typeof(AdoSessionProvider)
            };

            /// <summary>
            /// Creates a new ado provider task
            /// </summary>
            public RegisterAdoProvidersTask(IFeature feature)
            {
                this.Feature = feature;
            }

            /// <summary>
            /// Gets the description of this task
            /// </summary>
            public string Description => "Configures the core ADO.NET Persistence Services which are required to use a data-based instance of SanteDB";

            /// <summary>
            /// Gets the feature
            /// </summary>
            public IFeature Feature { get; }

            /// <summary>
            /// Get the name
            /// </summary>
            public string Name => "Register ADO Providers";

            /// <summary>
            /// Progress has changed
            /// </summary>
            public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

            /// <summary>
            /// Execute the configuration
            /// </summary>
            public bool Execute(SanteDBConfiguration configuration)
            {
                var serviceConfiguration = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
                foreach (var itm in this.SERVICE_TYPES)
                {
                    if (!serviceConfiguration.ServiceProviders.Any(o => o.Type == itm))
                    {
                        serviceConfiguration.ServiceProviders.Add(new TypeReferenceConfiguration(itm));
                    }
                }
                return true;
            }

            /// <summary>
            /// No rollback is supported
            /// </summary>
            public bool Rollback(SanteDBConfiguration configuration)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Verify the state
            /// </summary>
            public bool VerifyState(SanteDBConfiguration configuration)
            {
                var serviceConfiguration = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
                return !SERVICE_TYPES.All(t => serviceConfiguration.ServiceProviders.Any(s => s.Type == t));
            }
        }

        /// <summary>
        /// Get default configuration
        /// </summary>
        protected override object GetDefaultConfiguration() => new AdoPersistenceConfigurationSection()
        {
            AutoInsertChildren = true,
            AutoUpdateExisting = true,
            Validation = new List<AdoValidationPolicy>()
            {
                new AdoValidationPolicy()
                {
                    Uniqueness = AdoValidationEnforcement.Loose,
                    Authority = AdoValidationEnforcement.Loose,
                    CheckDigit = AdoValidationEnforcement.Loose,
                    Scope = AdoValidationEnforcement.Strict,
                    Format = AdoValidationEnforcement.Loose
                }
            }
        };
    }
}