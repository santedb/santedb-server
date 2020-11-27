/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using SanteDB.Core.Interfaces;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Privacy;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Configuration.Features
{
    /// <summary>
    /// Represents the default password validation service feature
    /// </summary>
    public class ServerSecurityFeature : IFeature
    {
        /// <summary>
        /// Gets the name of the feature
        /// </summary>
        public string Name => "Server Security";

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description => "Configures the default SanteDB server security features";

        /// <summary>
        /// Gets the group
        /// </summary>
        public string Group => FeatureGroup.Security;

        /// <summary>
        /// Configuration type
        /// </summary>
        public Type ConfigurationType => typeof(GenericFeatureConfiguration);

        /// <summary>
        /// Gets the current configuration
        /// </summary>
        public object Configuration { get; set; }

        /// <summary>
        /// Gets or sets the flags
        /// </summary>
        public FeatureFlags Flags => FeatureFlags.SystemFeature;

        /// <summary>
        /// Create the installation task
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateInstallTasks()
        {
            return new IConfigurationTask[]
            {
                new InstallSecurityServicesTask(this)
            };
        }

        /// <summary>
        /// Create uninstallation tasks
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateUninstallTasks()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Query the current status of the feature
        /// </summary>
        public FeatureInstallState QueryState(SanteDBConfiguration configuration)
        {

            // Configuration of features
            var config = new GenericFeatureConfiguration();
            config.Options.Add("Configuration", () => ConfigurationOptionType.Object);
            config.Values.Add("Configuration", configuration.GetSection(typeof(SecurityConfigurationSection)) ?? new SecurityConfigurationSection());

            // Add options for password encrypting and such
            config.Options.Add("PasswordHasher", () => ApplicationServiceContext.Current.GetService<IServiceManager>().GetAllTypes().Where(t => !t.IsInterface && !t.IsAbstract && typeof(IPasswordHashingService).IsAssignableFrom(t)));
            config.Options.Add("PolicyDecisionProvider", () => ApplicationServiceContext.Current.GetService<IServiceManager>().GetAllTypes().Where(t => !t.IsInterface && !t.IsAbstract && typeof(IPolicyDecisionService).IsAssignableFrom(t)));
            config.Options.Add("PolicyInformationProvider", () => ApplicationServiceContext.Current.GetService<IServiceManager>().GetAllTypes().Where(t => !t.IsInterface && !t.IsAbstract && typeof(IPolicyInformationService).IsAssignableFrom(t)));
            config.Options.Add("PasswordValidator", () => ApplicationServiceContext.Current.GetService<IServiceManager>().GetAllTypes().Where(t => !t.IsInterface && !t.IsAbstract && typeof(IPasswordValidatorService).IsAssignableFrom(t)));

            var appServices = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders;

            Type validator = appServices.FirstOrDefault(t => typeof(IPasswordValidatorService).IsAssignableFrom(t.Type))?.Type,
                hasher = appServices.FirstOrDefault(t => typeof(IPasswordHashingService).IsAssignableFrom(t.Type))?.Type,
                pip = appServices.FirstOrDefault(t => typeof(IPolicyInformationService).IsAssignableFrom(t.Type))?.Type,
                pdp = appServices.FirstOrDefault(t => typeof(IPolicyDecisionService).IsAssignableFrom(t.Type))?.Type;

            config.Values.Add("PasswordHasher", hasher ?? typeof(SHA256PasswordHashingService));
            config.Values.Add("PasswordValidator", validator ?? typeof(DefaultPasswordValidationService));
            config.Values.Add("PolicyDecisionProvider", pdp ?? typeof(DefaultPolicyDecisionService));
            config.Values.Add("PolicyInformationProvider", pip ?? (config.Options["PolicyInformationProvider"]() as IEnumerable<Type>).FirstOrDefault());

            if(this.Configuration == null)
                this.Configuration = config;

            return hasher != null && validator != null && pdp != null && pip != null ? FeatureInstallState.Installed : FeatureInstallState.PartiallyInstalled;

    }

        /// <summary>
        /// Install security services task
        /// </summary>
        private class InstallSecurityServicesTask : IConfigurationTask
        {

            // THe feature reference
            private ServerSecurityFeature m_feature;

            /// <summary>
            /// Creates a new installation task
            /// </summary>
            public InstallSecurityServicesTask(ServerSecurityFeature feature)
            {
                this.m_feature = feature;
            }

            /// <summary>
            /// Get the name of the service
            /// </summary>
            public string Name => "Save Security Configuration";

            /// <summary>
            /// Gets the description
            /// </summary>
            public string Description => "Installs and configures the core security services";

            /// <summary>
            /// Gets the feature
            /// </summary>
            public IFeature Feature => this.m_feature;

            /// <summary>
            /// Progress has changed
            /// </summary>
            public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

            /// <summary>
            /// Execute the 
            /// </summary>
            /// <param name="configuration"></param>
            /// <returns></returns>
            public bool Execute(SanteDBConfiguration configuration)
            {
                var appServices = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders;
                appServices.RemoveAll(t => typeof(IPasswordValidatorService).IsAssignableFrom(t.Type) ||
                        typeof(IPasswordHashingService).IsAssignableFrom(t.Type) ||
                        typeof(IPolicyInformationService).IsAssignableFrom(t.Type) ||
                        typeof(IPolicyDecisionService).IsAssignableFrom(t.Type));

                // Now we want to read the configuration 
                var config = this.m_feature.Configuration as GenericFeatureConfiguration;
                if (config == null)
                {
                    this.m_feature.QueryState(configuration);
                    config = this.m_feature.Configuration as GenericFeatureConfiguration;
                }

                configuration.RemoveSection<SecurityConfigurationSection>();
                configuration.AddSection(config.Values["Configuration"] as SecurityConfigurationSection);

                // Now add the services
                appServices.RemoveAll(t => t.Type == typeof(ExemptablePolicyFilterService));
                appServices.Add(new TypeReferenceConfiguration(typeof(ExemptablePolicyFilterService)));
                appServices.Add(new TypeReferenceConfiguration(config.Values["PasswordHasher"] as Type));
                appServices.Add(new TypeReferenceConfiguration(config.Values["PasswordValidator"] as Type));
                appServices.Add(new TypeReferenceConfiguration(config.Values["PolicyDecisionProvider"] as Type));
                appServices.Add(new TypeReferenceConfiguration(config.Values["PolicyInformationProvider"] as Type));

                return true;
            }

            /// <summary>
            /// Rollback configuration
            /// </summary>
            public bool Rollback(SanteDBConfiguration configuration)
            {
                return true;
            }

            /// <summary>
            /// Verify state
            /// </summary>
            public bool VerifyState(SanteDBConfiguration configuration)
            {
                return true;
            }
        }
    }
}
