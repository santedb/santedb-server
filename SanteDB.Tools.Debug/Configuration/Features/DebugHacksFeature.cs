﻿/*
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
using SanteDB.BI.Services;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using SanteDB.Rest.Common.Behavior;
using SanteDB.Rest.Common.Configuration;
using SanteDB.Tools.Debug.BI;
using SanteDB.Tools.Debug.Wcf;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SanteDB.Tools.Debug.Configuration.Features
{
    /// <summary>
    /// Debugging and hacks feature
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DebugHacksFeature : IFeature
    {
        private const string BiFileRepositorySetting = "Debug Options";
        private const string BiFileRepositoryEnabledSetting = "Enable Debug BI Repository";
        private const string SanteboxServiceEnabledSetting = "HDSI Sandbox UI Enabled";
        private const string SanteboxServiceSetting = "HDSI Sandbox UI";

        // Generic feature configuration
        private GenericFeatureConfiguration m_configuration;

        /// <summary>
        /// Gets or sets the configuration object
        /// </summary>
        public object Configuration
        {
            get => this.m_configuration;
            set => this.m_configuration = value as GenericFeatureConfiguration;
        }

        /// <summary>
        /// Gets or sets the configuration type
        /// </summary>
        public Type ConfigurationType => typeof(GenericFeatureConfiguration);

        /// <summary>
        /// Gets the description of this feature
        /// </summary>
        public string Description => "Tools which can assist developers in unsing SanteDB";

        /// <summary>
        /// Flags for the feature
        /// </summary>
        public FeatureFlags Flags => FeatureFlags.None;

        /// <summary>
        /// Gets the group
        /// </summary>
        public string Group => FeatureGroup.Development;

        /// <summary>
        /// Get the name of the feature
        /// </summary>
        public string Name => "Developer Options";

        /// <summary>
        /// Create install tasks
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateInstallTasks()
        {
            yield return new InstallServiceTask(this, typeof(FileMetadataRepository), () => (bool)this.m_configuration.Values[BiFileRepositoryEnabledSetting] == true, typeof(IBiMetadataRepository));
            yield return new InstallServiceTask(this, typeof(DataSandboxService), () => (bool)this.m_configuration.Values[SanteboxServiceEnabledSetting] == true);
            yield return new InstallRestServiceTask(this, this.m_configuration.Values[SanteboxServiceSetting] as RestServiceConfiguration, () => (bool)this.m_configuration.Values[SanteboxServiceEnabledSetting] == true);
            yield return new InstallConfigurationSectionTask(this, this.m_configuration.Values[BiFileRepositorySetting] as IConfigurationSection, "Debug Tools");
        }

        /// <summary>
        /// Create uninstall tasks
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateUninstallTasks()
        {
            yield return new UnInstallServiceTask(this, typeof(FileMetadataRepository), () => (bool)this.m_configuration.Values[BiFileRepositoryEnabledSetting] == false);
            yield return new UnInstallServiceTask(this, typeof(DataSandboxService), () => (bool)this.m_configuration.Values[SanteboxServiceEnabledSetting] == false);
            yield return new UnInstallRestServiceTask(this, this.m_configuration.Values[SanteboxServiceEnabledSetting] as RestServiceConfiguration, () => (bool)this.m_configuration.Values[SanteboxServiceEnabledSetting] == false);
            yield return new UnInstallConfigurationSectionTask(this, this.m_configuration.Values[BiFileRepositorySetting] as IConfigurationSection, "Debug Tools");
        }

        /// <summary>
        /// Query state
        /// </summary>
        public FeatureInstallState QueryState(SanteDBConfiguration configuration)
        {
            var sectionConfig = configuration.GetSection<DebugToolsConfigurationSection>();

            // Is the FILE based BI repository enabled?
            this.m_configuration = new GenericFeatureConfiguration();
            this.m_configuration.Options.Add(BiFileRepositorySetting, () => ConfigurationOptionType.Object);
            this.m_configuration.Values.Add(BiFileRepositorySetting, sectionConfig ?? new DebugToolsConfigurationSection());
            this.m_configuration.Options.Add(BiFileRepositoryEnabledSetting, () => ConfigurationOptionType.Boolean);
            this.m_configuration.Values.Add(BiFileRepositoryEnabledSetting, configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.Any(t => typeof(FileMetadataRepository) == t.Type));
            this.m_configuration.Categories.Add("HDSI Sandbox UI", new String[] { SanteboxServiceEnabledSetting, SanteboxServiceSetting });
            this.m_configuration.Options.Add(SanteboxServiceEnabledSetting, () => ConfigurationOptionType.Boolean);
            this.m_configuration.Values.Add(SanteboxServiceEnabledSetting, configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.Any(t => t.Type == typeof(DataSandboxService)));
            this.m_configuration.Options.Add(SanteboxServiceSetting, () => ConfigurationOptionType.Object);
            this.m_configuration.Values.Add(SanteboxServiceSetting, configuration.GetSection<RestConfigurationSection>().Services.FirstOrDefault(o => o.ConfigurationName == "HDSI_Sandbox") ?? new RestServiceConfiguration()
            {
                ConfigurationName = DataSandboxService.ConfigurationName,
                ServiceType = typeof(DataSandboxTool),
                Endpoints = new List<RestEndpointConfiguration>() {
                new RestEndpointConfiguration()
                {
                    Contract = typeof(IDataSandboxTool),
                    Address = "http://127.0.0.1:8080/sandbox",
                    Behaviors = new List<RestEndpointBehaviorConfiguration>()
                    {
                        new RestEndpointBehaviorConfiguration(typeof(MessageCompressionEndpointBehavior)),
                        new RestEndpointBehaviorConfiguration(typeof(AcceptLanguageEndpointBehavior))
                    }
                }
            }
            });
            // Construct the configuration options
            return sectionConfig != null ? FeatureInstallState.Installed : FeatureInstallState.NotInstalled;
        }
    }
}