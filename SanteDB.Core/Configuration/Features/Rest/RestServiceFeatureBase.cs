using SanteDB.Configuration;
using SanteDB.Configuration.Features;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using SanteDB.Core.Interop;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SanteDB.Core.Configuration.Rest
{
    /// <summary>
    /// Represents an abstract implementation of the rest service feature
    /// </summary>
    public class RestServiceFeatureBase : IFeature
    {
        // Message daemon type
        private Type m_messageDaemonType = null;
        // Contract type
        private Type m_contractType = null;
        // Configuration type
        private Type m_configurationType = null;

        /// <summary>
        /// When no constructor is called this will add the services to the configuration context
        /// </summary>
        public RestServiceFeatureBase()
        {
            ApplicationServiceContext.Current.Started += (o, e) =>
            {
                foreach (var i in AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.IsDynamic)
                        .SelectMany(a => { try { return a.ExportedTypes; } catch { return new List<Type>(); } })
                        .Where(t => typeof(IApiEndpointProvider).IsAssignableFrom(t) && !t.ContainsGenericParameters && !t.IsAbstract && !t.IsInterface && t.GetCustomAttribute<ApiServiceProviderAttribute>() != null)
                        .Select(i => new RestServiceFeatureBase(i)))
                    (ApplicationServiceContext.Current as ConfigurationContext).Features.Add(i);
            };
        }

        /// <summary>
        /// Creates a new rest service feature
        /// </summary>
        public RestServiceFeatureBase(Type messageDaemonType)
        {
            this.Name = messageDaemonType.GetCustomAttribute<DescriptionAttribute>()?.Description ?? messageDaemonType.Name;
            this.Description = $"Configures the {this.Name} daemon";
            this.m_messageDaemonType = messageDaemonType;
            this.m_contractType = messageDaemonType.GetCustomAttribute<ApiServiceProviderAttribute>()?.ContractType;
            this.m_configurationType = messageDaemonType.GetCustomAttribute<ApiServiceProviderAttribute>()?.Configuration;
        }

        /// <summary>
        /// Gets or sets the name of the 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Group for the messaging context
        /// </summary>
        public string Group => FeatureGroup.Messaging;

        /// <summary>
        /// Configuration type
        /// </summary>
        public Type ConfigurationType => this.m_configurationType == null ? typeof(RestConfigurationSection) : typeof(GenericFeatureConfiguration);

        /// <summary>
        /// Gets or sets the configuration
        /// </summary>
        public object Configuration { get; set; }

        /// <summary>
        /// Flags for this feature
        /// </summary>
        public FeatureFlags Flags => FeatureFlags.None;

        /// <summary>
        /// Create the installation tasks
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateInstallTasks()
        {
            return new IConfigurationTask[]
            {
                new ConfigureServiceSectionTask(this),
                new ActivateApiService(this),
                new ConfigureRestEndpointTask(this)
            };
        }

        /// <summary>
        /// Create uninstall tasks
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateUninstallTasks()
        {
            return new IConfigurationTask[]
            {
                new RemoveServiceSectionTask(this),
                new DeactivateApiService(this),
                new RemoveRestEndpointTask(this)
            };
        }

        /// <summary>
        /// Query the status of this feature
        /// </summary>
        public FeatureInstallState QueryState(SanteDBConfiguration configuration)
        {
            if (!configuration.SectionTypes.Any(o => o.Type == typeof(RestConfigurationSection)))
                configuration.SectionTypes.Add(new TypeReferenceConfiguration(typeof(RestConfigurationSection)));

            if (this.m_configurationType != null)
            {
                if (!configuration.SectionTypes.Any(o => o.Type == this.m_configurationType))
                    configuration.SectionTypes.Add(new TypeReferenceConfiguration(this.m_configurationType));

                if (configuration.GetSection<RestConfigurationSection>() == null)
                    configuration.AddSection(new RestConfigurationSection());

                var config = new GenericFeatureConfiguration();
                this.Configuration = config;

                config.Options.Add("Service", () => ConfigurationOptionType.Object);
                config.Options.Add("REST", () => ConfigurationOptionType.Object);

                config.Categories.Add("REST Configuration", new string[] { "Service", "REST" });

                config.Values.Add("Service", configuration.GetSection(this.m_configurationType) ?? Activator.CreateInstance(this.m_configurationType));
                config.Values.Add("REST", configuration.GetSection<RestConfigurationSection>().Services.FirstOrDefault(s => s.ServiceType == this.m_contractType) ?? new RestServiceConfiguration());

                return configuration.GetSection(this.m_configurationType) != null &&
                    configuration.GetSection<RestConfigurationSection>().Services.Any(s => s.ServiceType == this.m_contractType) ?
                    FeatureInstallState.Installed : FeatureInstallState.NotInstalled;
            }
            else
            {
                this.Configuration = configuration.GetSection<RestConfigurationSection>();
                if (this.Configuration == null)
                {
                    this.Configuration = new RestConfigurationSection();
                    return FeatureInstallState.NotInstalled;
                }
                else
                    return FeatureInstallState.Installed;
            }
        }
    }
}
