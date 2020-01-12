using SanteDB.Configuration;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Configuration.Features
{
    /// <summary>
    /// Configuration for REST services on the gateway
    /// </summary>
    public class RestServiceFeature : IFeature
    {

        // Contract type
        private Type m_contractType = null;

        // Configuration type
        private Type m_configurationType = null;

        // Service type
        private Type m_serviceType = null;

        /// <summary>
        /// REST service configuration
        /// </summary>
        public RestServiceFeature()
        {
            ConfigurationContext.Current.Started += (o, e) =>
            {
                foreach (var feature in AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic)
                    .SelectMany(t => t.ExportedTypes)
                    .Where(t => t.GetCustomAttribute<ApiServiceProviderAttribute>() != null && !t.IsInterface && !t.IsAbstract)
                    .Select(t => new RestServiceFeature(t))
                    .ToArray())
                    ConfigurationContext.Current.Features.Add(feature);
            };
        }

        /// <summary>
        /// Creates a new rest service configuration type
        /// </summary>
        public RestServiceFeature(Type serviceProviderType)
        {
            this.m_serviceType = serviceProviderType;
            this.m_configurationType = serviceProviderType.GetCustomAttribute<ApiServiceProviderAttribute>().Configuration;
            this.m_contractType = serviceProviderType.GetCustomAttribute<ApiServiceProviderAttribute>().ContractType;
            this.Name = serviceProviderType.GetCustomAttribute<ApiServiceProviderAttribute>().Name ?? serviceProviderType.Name;
            this.Description = serviceProviderType.GetCustomAttribute<DescriptionAttribute>()?.Description;

            if (serviceProviderType.GetCustomAttribute<ApiServiceProviderAttribute>().Required)
                this.Flags = FeatureFlags.SystemFeature;
            else
                this.Flags = FeatureFlags.None;
        }

        /// <summary>
        /// Gets or sets the name of the REST service
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of this provider
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the group to which this configuration panel belongs
        /// </summary>
        public string Group => FeatureGroup.Messaging;

        /// <summary>
        /// Gets the configuration type
        /// </summary>
        public Type ConfigurationType => this.m_configurationType == null ? null : typeof(GenericFeatureConfiguration);

        /// <summary>
        /// Gets the configuration object itself
        /// </summary>
        public object Configuration { get; set; }

        /// <summary>
        /// Gets the flags for this configuration section
        /// </summary>
        public FeatureFlags Flags { get; }

        /// <summary>
        /// Create installation tasks
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateInstallTasks()
        {
            return new IConfigurationTask[] { };
        }

        /// <summary>
        /// Create uninstall tasks
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateUninstallTasks()
        {
            return new IConfigurationTask[] { };
        }

        /// <summary>
        /// Query for the state of this rest service
        /// </summary>
        public FeatureInstallState QueryState(SanteDBConfiguration configuration)
        {

            // First, is the REST service enabled?
            if (!configuration.SectionTypes.Any(o => o.Type == typeof(RestConfigurationSection)))
                configuration.AddSection(new RestConfigurationSection());

            // Does the contract exist?
            var restConfiguration = configuration.GetSection<RestConfigurationSection>().Services.FirstOrDefault(s => s.Endpoints.Any(e => e.Contract == this.m_contractType));

            // Create / add section type
            if (!configuration.SectionTypes.Any(t => t.Type == this.m_configurationType))
                configuration.SectionTypes.Add(new TypeReferenceConfiguration(this.m_configurationType));
            
            // Does the section exist?
            var serviceConfiguration = configuration.GetSection(this.m_configurationType);

            // Feature state
            var featureState = serviceConfiguration != null && restConfiguration != null ? FeatureInstallState.Installed :
                serviceConfiguration != null ^ restConfiguration != null ? FeatureInstallState.PartiallyInstalled :
                FeatureInstallState.NotInstalled;

            // Do either exist?
            if (restConfiguration == null)
                using (var s = this.m_configurationType.Assembly.GetManifestResourceStream(this.m_configurationType.Namespace + ".Default.xml"))
                    restConfiguration = RestServiceConfiguration.Load(s);
            if (serviceConfiguration == null)
                serviceConfiguration = Activator.CreateInstance(this.m_configurationType);

            // Construct the configuraiton
            this.Configuration = new GenericFeatureConfiguration()
            {
                Categories = new Dictionary<string, string[]>()
                {
                    { "Misc", new string[] { "REST API", "Service" } }
                },
                Options = new Dictionary<string, Func<object>>()
                {
                    { "REST API", () => ConfigurationOptionType.Object },
                    { "Service", () => ConfigurationOptionType.Object }
                },
                Values = new Dictionary<string, object>()
                {
                    { "REST API", restConfiguration },
                    { "Service", serviceConfiguration }
                }
            };

            return featureState;
        }
    }
}
