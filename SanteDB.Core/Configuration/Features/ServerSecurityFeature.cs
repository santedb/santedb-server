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
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Security.Privacy;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Server.Core.Security;
using SanteDB.Server.Core.Security.Privacy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;
using System.Windows.Forms;

namespace SanteDB.Server.Core.Configuration.Features
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
                new InstallSecurityServicesTask(this),
                new InstallCertificatesTask(this)
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
            var configSection = configuration.GetSection<SanteDB.Core.Security.Configuration.SecurityConfigurationSection>() ?? new SanteDB.Core.Security.Configuration.SecurityConfigurationSection()
            {
                Signatures = new List<SanteDB.Core.Security.Configuration.SecuritySignatureConfiguration>()
                {
                    new SanteDB.Core.Security.Configuration.SecuritySignatureConfiguration()
                    {
                        KeyName ="jwsdefault",
                        Algorithm = SanteDB.Core.Security.Configuration.SignatureAlgorithm.HS256,
                        HmacSecret = "@SanteDBDefault$$$2021"
                    }
                }
            };
            config.Values.Add("Configuration", configSection);

            // Add options for password encrypting and such
            config.Options.Add("PasswordHasher", () => AppDomain.CurrentDomain.GetAllTypes().Where(t => !t.IsInterface && !t.IsAbstract && typeof(IPasswordHashingService).IsAssignableFrom(t)));
            config.Options.Add("PolicyDecisionProvider", () => AppDomain.CurrentDomain.GetAllTypes().Where(t => !t.IsInterface && !t.IsAbstract && typeof(IPolicyDecisionService).IsAssignableFrom(t)));
            config.Options.Add("PolicyInformationProvider", () => AppDomain.CurrentDomain.GetAllTypes().Where(t => !t.IsInterface && !t.IsAbstract && typeof(IPolicyInformationService).IsAssignableFrom(t)));
            config.Options.Add("PasswordValidator", () => AppDomain.CurrentDomain.GetAllTypes().Where(t => !t.IsInterface && !t.IsAbstract && typeof(IPasswordValidatorService).IsAssignableFrom(t)));

            var appServices = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders;

            Type validator = appServices.FirstOrDefault(t => typeof(IPasswordValidatorService).IsAssignableFrom(t.Type))?.Type,
                hasher = appServices.FirstOrDefault(t => typeof(IPasswordHashingService).IsAssignableFrom(t.Type))?.Type,
                pip = appServices.FirstOrDefault(t => typeof(IPolicyInformationService).IsAssignableFrom(t.Type))?.Type,
                pdp = appServices.FirstOrDefault(t => typeof(IPolicyDecisionService).IsAssignableFrom(t.Type))?.Type;

            config.Values.Add("PasswordHasher", hasher ?? typeof(SanteDB.Core.Security.SHA256PasswordHashingService));
            config.Values.Add("PasswordValidator", validator ?? typeof(DefaultPasswordValidationService));
            config.Values.Add("PolicyDecisionProvider", pdp ?? typeof(DefaultPolicyDecisionService));
            config.Values.Add("PolicyInformationProvider", pip ?? (config.Options["PolicyInformationProvider"]() as IEnumerable<Type>).FirstOrDefault());

            if (this.Configuration == null)
                this.Configuration = config;

            // Add policies
            config.Categories.Add("Policies", new String[] {
                "PasswordAge",
                "PasswordHistory",
                "FailedLogins",
                "SessionLength",
                "SessionRefresh"
            });
            config.Options.Add("PasswordAge", () => ConfigurationOptionType.Numeric);
            config.Options.Add("PasswordHistory", () => ConfigurationOptionType.Boolean);
            config.Options.Add("FailedLogins", () => ConfigurationOptionType.Numeric);
            config.Options.Add("SessionLength", () => Enumerable.Range(15, 180).Where(o => o % 15 == 0).Select(o => new PolicyValueTimeSpan(0, o, 0)));
            config.Options.Add("SessionRefresh", () => Enumerable.Range(15, 180).Where(o => o % 15 == 0).Select(o => new PolicyValueTimeSpan(0, o, 0)));
            config.Values.Add("PasswordAge", configSection.GetSecurityPolicy<Int32>(SecurityPolicyIdentification.MaxPasswordAge, 3650));
            config.Values.Add("PasswordHistory", configSection.GetSecurityPolicy<bool>(SecurityPolicyIdentification.PasswordHistory, false));
            config.Values.Add("FailedLogins", configSection.GetSecurityPolicy(SecurityPolicyIdentification.PasswordHistory, 5));
            config.Values.Add("SessionLength", configSection.GetSecurityPolicy<PolicyValueTimeSpan>(SecurityPolicyIdentification.SessionLength, new PolicyValueTimeSpan(0, 30, 0)));
            config.Values.Add("SessionRefresh", configSection.GetSecurityPolicy<PolicyValueTimeSpan>(SecurityPolicyIdentification.RefreshLength, new PolicyValueTimeSpan(0, 30, 0)));
            return hasher != null && validator != null && pdp != null && pip != null ? FeatureInstallState.Installed : FeatureInstallState.PartiallyInstalled;

        }

        /// <summary>
        /// Install security certificates
        /// </summary>
        private class InstallCertificatesTask : IConfigurationTask
        {

            /// <summary>
            /// Create a new install certificates features
            /// </summary>
            public InstallCertificatesTask(IFeature feature)
            {
                this.Feature = feature;
            }

            /// <summary>
            /// Description of the feature
            /// </summary>
            public string Description => "Install SanteDB's applet signing certificates into the machine's certificate store";

            /// <summary>
            /// The feature to be installed
            /// </summary>
            public IFeature Feature { get; }

            /// <summary>
            /// Gets the nameof the feature
            /// </summary>
            public string Name => "Install Certificates";

            /// <summary>
            /// Progress has changed
            /// </summary>
            public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

            /// <summary>
            /// Execute
            /// </summary>
            public bool Execute(SanteDBConfiguration configuration)
            {
                SecurityExtensions.InstallCertsForChain();
                this.ProgressChanged?.Invoke(this, new SanteDB.Core.Services.ProgressChangedEventArgs(1.0f, "Complete"));

                return true;
            }

            /// <summary>
            /// Rollback
            /// </summary>
            public bool Rollback(SanteDBConfiguration configuration)
            {
                return true;
            }

            /// <summary>
            /// Verify state
            /// </summary>
            public bool VerifyState(SanteDBConfiguration configuration) => true;
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

                configuration.RemoveSection<SanteDB.Core.Security.Configuration.SecurityConfigurationSection>();
                var secSection = config.Values["Configuration"] as SanteDB.Core.Security.Configuration.SecurityConfigurationSection;
                configuration.AddSection(secSection);

                // Now add the services
                appServices.RemoveAll(t => t.Type == typeof(ExemptablePolicyFilterService));
                appServices.Add(new TypeReferenceConfiguration(typeof(ExemptablePolicyFilterService)));
                appServices.Add(new TypeReferenceConfiguration(config.Values["PasswordHasher"] as Type));
                appServices.Add(new TypeReferenceConfiguration(config.Values["PasswordValidator"] as Type));
                appServices.Add(new TypeReferenceConfiguration(config.Values["PolicyDecisionProvider"] as Type));
                appServices.Add(new TypeReferenceConfiguration(config.Values["PolicyInformationProvider"] as Type));

                // Now set the policy values
                secSection.SetPolicy(SecurityPolicyIdentification.MaxPasswordAge, (Int32)config.Values["PasswordAge"]);
                secSection.SetPolicy(SecurityPolicyIdentification.PasswordHistory, (bool)config.Values["PasswordHistory"]);
                secSection.SetPolicy(SecurityPolicyIdentification.MaxInvalidLogins, (Int32)config.Values["FailedLogins"]);
                secSection.SetPolicy(SecurityPolicyIdentification.SessionLength, (PolicyValueTimeSpan)config.Values["SessionLength"]);
                secSection.SetPolicy(SecurityPolicyIdentification.RefreshLength, (PolicyValueTimeSpan)config.Values["SessionRefresh"]);
                this.ProgressChanged?.Invoke(this, new SanteDB.Core.Services.ProgressChangedEventArgs(1.0f, "Complete"));

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
