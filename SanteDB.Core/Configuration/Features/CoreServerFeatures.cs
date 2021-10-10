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
using SanteDB.Server.Core.Persistence;
using SanteDB.Server.Core.Rest;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Privacy;
using SanteDB.Core.Services;
using SanteDB.Core.Services.Impl;
using SanteDB.Server.Core.Security.Privacy;
using SanteDB.Server.Core.Services.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.PubSub.Broker;
using SanteDB.BI.Services.Impl;
using SanteDB.Core.Notifications;
using SanteDB.Core.Applets.Services.Impl;

namespace SanteDB.Server.Core.Configuration.Features
{
    /// <summary>
    /// Represents a feature for core server implementation
    /// </summary>
    public class CoreServerFeatures : IFeature
    {
        /// <summary>
        /// Services in this configuration
        /// </summary>
        internal static readonly Type[] SERVICE_TYPES =
        {
            typeof(DefaultPolicyEnforcementService),
            typeof(DefaultOperatingSystemInfoService),
            typeof(DefaultThreadPoolService),
            typeof(DefaultNetworkInformationService),
            typeof(RestServiceFactory),
            typeof(LocalRepositoryService),
            typeof(ExemptablePolicyFilterService),
            typeof(LocalMailMessageRepository),
            typeof(LocalStockManagementRepositoryService),
            typeof(LocalTagPersistenceService),
            typeof(PubSubBroker),
            typeof(AppletBiRepository),
            typeof(LocalBiRenderService),
            typeof(DefaultNotificationService),
            typeof(LocalTemplateDefinitionRepositoryService),
            typeof(DefaultDataSigningService),
            typeof(AesSymmetricCrypographicProvider),
            typeof(SimpleTfaSecretGenerator),
            typeof(AppletLocalizationService),
            typeof(CachedResourceCheckoutService)
        };

        /// <summary>
        /// Gets the name of the feature
        /// </summary>
        public string Name => "SanteDB Server Core";

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description => "Core features for the SanteDB Server";

        /// <summary>
        /// Get the group
        /// </summary>
        public string Group => FeatureGroup.System;

        /// <summary>
        /// Gets the configuration type
        /// </summary>
        public Type ConfigurationType => null;

        /// <summary>
        /// Gets or sets the configuration
        /// </summary>
        public object Configuration { get; set; }

        /// <summary>
        /// Gets the flags for the configuration object
        /// </summary>
        public FeatureFlags Flags => FeatureFlags.NoRemove | FeatureFlags.AutoSetup;

        /// <summary>
        /// Create the installation task
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateInstallTasks()
        {
            return new IConfigurationTask[]
            {
                new InstallCoreServicesTask(this)
            };
        }

        /// <summary>
        /// Create uninstall tasks
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateUninstallTasks()
        {
            return null;
        }

        /// <summary>
        /// Query the status
        /// </summary>
        public FeatureInstallState QueryState(SanteDBConfiguration configuration)
        {
            var nServices = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.Count(s => SERVICE_TYPES.Any(o => o == s.Type));
            if (nServices == SERVICE_TYPES.Length)
                return FeatureInstallState.Installed;
            else if (nServices == 0)
                return FeatureInstallState.NotInstalled;
            else
                return FeatureInstallState.PartiallyInstalled;
        }

        /// <summary>
        /// Represents the core services task
        /// </summary>
        public class InstallCoreServicesTask : IConfigurationTask
        {
            /// <summary>
            /// Create a new install core services task
            /// </summary>
            public InstallCoreServicesTask(CoreServerFeatures feature)
            {
                this.Feature = feature;
            }

            /// <summary>
            /// Get the name of the service task
            /// </summary>
            public string Name => "Install SanteDB Server Services";

            /// <summary>
            /// Gets the description of the task
            /// </summary>
            public string Description => "Installs core services used by SanteDB iCDR Server including configuration, threading, and network information";

            /// <summary>
            /// Gets the feature
            /// </summary>
            public IFeature Feature { get; }

            /// <summary>
            /// Fired when progress of install or execution has changed
            /// </summary>
            public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

            /// <summary>
            /// Execute the installation
            /// </summary>
            public bool Execute(SanteDBConfiguration configuration)
            {
                var appSection = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
                int i = 0;
                foreach (var svc in SERVICE_TYPES.Reverse())
                {
                    this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs((float)i++ / SERVICE_TYPES.Length, $"Installing {svc.Name}"));
                    appSection.ServiceProviders.RemoveAll(o => o.Type == svc);
                    appSection.ServiceProviders.Insert(0, new TypeReferenceConfiguration(svc));
                }
                return true;
            }

            /// <summary>
            /// Rollback any changes
            /// </summary>
            public bool Rollback(SanteDBConfiguration configuration)
            {
                var appSection = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
                foreach (var svc in SERVICE_TYPES.Reverse())
                    appSection.ServiceProviders.RemoveAll(o => o.Type == svc);
                return true;
            }

            /// <summary>
            /// Verify that this task can run
            /// </summary>
            public bool VerifyState(SanteDBConfiguration configuration)
            {
                return true;
            }
        }
    }
}