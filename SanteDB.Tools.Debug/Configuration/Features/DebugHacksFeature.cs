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
using System.Text;
using System.Threading.Tasks;

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
            this.m_configuration.Values.Add(SanteboxServiceSetting, configuration.GetSection<RestConfigurationSection>().Services.FirstOrDefault(o => o.Name == "HDSI_Sandbox") ?? new RestServiceConfiguration()
            {
                Name = "HDSI_Sandbox",
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