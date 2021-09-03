/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2021-8-5
 */
using SanteDB.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Configuration;
using SanteDB.Server.Core.Configuration.Tasks;
using SanteDB.Server.Core.Configuration.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SanteDB.Server.Core.Configuration.Features
{
    /// <summary>
    /// Configuration for REST services on the gateway
    /// </summary>
    public class RestServiceFeature : IFeature
    {

        // Tracer
        internal readonly Tracer m_tracer;

        // Configuration
        private GenericFeatureConfiguration m_configuration;

        // The old configuration (for detecting if certificates need to be removed)
        private RestServiceConfiguration m_oldConfiguration;

        // Contract type
        private Type m_behaviorType = null;
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

            foreach (var feature in AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(t => { try { return t.ExportedTypes; } catch { return Type.EmptyTypes; } })
                .Where(t => t.GetCustomAttribute<ApiServiceProviderAttribute>() != null && !t.IsInterface && !t.IsAbstract)
                .Select(t => new RestServiceFeature(t))
                .ToArray())
                ConfigurationContext.Current.Features.Add(feature);
            this.Flags = FeatureFlags.NonPublic;
        }

        /// <summary>
        /// Creates a new rest service configuration type
        /// </summary>
        public RestServiceFeature(Type serviceProviderType)
        {
            this.m_serviceType = serviceProviderType;
            this.m_configurationType = serviceProviderType.GetCustomAttribute<ApiServiceProviderAttribute>().Configuration;
            this.m_behaviorType = serviceProviderType.GetCustomAttribute<ApiServiceProviderAttribute>().BehaviorType;
            this.m_contractType = this.m_behaviorType.GetInterfaces().First();
            this.Name = serviceProviderType.GetCustomAttribute<ApiServiceProviderAttribute>().Name ?? serviceProviderType.Name;
            this.Description = serviceProviderType.GetCustomAttribute<DescriptionAttribute>()?.Description;
            this.m_tracer = new Tracer(this.Name);
            if (serviceProviderType.GetCustomAttribute<ApiServiceProviderAttribute>().Required)
            {
                this.Flags = FeatureFlags.SystemFeature;
            }
            else
            {
                this.Flags = FeatureFlags.None;
            }
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
        public object Configuration
        {
            get => this.m_configuration;
            set => this.m_configuration = value as GenericFeatureConfiguration;
        }

        /// <summary>
        /// Gets the flags for this configuration section
        /// </summary>
        public FeatureFlags Flags { get; }

        /// <summary>
        /// Set the old configuration
        /// </summary>
        internal void SetOldConfiguration(RestServiceConfiguration oldConfiguration)
        {
            this.m_oldConfiguration = new RestServiceConfiguration(oldConfiguration);
        }

        /// <summary>
        /// Create installation tasks
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateInstallTasks()
        {

            var retVal = new List<IConfigurationTask>();

            if (this.m_oldConfiguration != null) // There is an old configuration
            {
                if (this.m_oldConfiguration.Endpoints.Any(o => o.Address.StartsWith("https")))
                {
                    // There was an HTTPS so we're going to unbind the cert
                    retVal.Add(new RestEndpointUninstallTask(this, this.m_oldConfiguration));
                }
            }

            // Add configuration 
            retVal.AddRange(new IConfigurationTask[] {
                new RestMessageDaemonInstallTask(this, this.m_serviceType),
                new RestEndpointInstallTask(this, this.m_configuration.Values["REST API"] as RestServiceConfiguration),
                new RestServiceConfigurationInstallTask(this, this.m_configuration.Values["Service"])
            });

            return retVal;
        }

        /// <summary>
        /// Create uninstall tasks
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateUninstallTasks()
        {
            return new IConfigurationTask[] {
                new RestMessageDaemonUninstallTask(this, this.m_serviceType),
                new RestEndpointUninstallTask(this, this.m_configuration.Values["REST API"] as RestServiceConfiguration),
                new RestServiceConfigurationUninstallTask(this, this.m_configuration.Values["Service"])
            };
        }

        /// <summary>
        /// Query for the state of this rest service
        /// </summary>
        public FeatureInstallState QueryState(SanteDBConfiguration configuration)
        {

            // First, is the REST service enabled?
            if (!configuration.SectionTypes.Any(o => typeof(SanteDB.Rest.Common.Configuration.RestConfigurationSection).IsAssignableFrom(o.Type)))
                configuration.AddSection(new SanteDB.Rest.Common.Configuration.RestConfigurationSection());

            // Does the contract exist?
            var restConfiguration = configuration.GetSection<SanteDB.Rest.Common.Configuration.RestConfigurationSection>().Services.FirstOrDefault(s => s.ServiceType == this.m_behaviorType || s.Endpoints.Any(e => e.Contract == this.m_contractType));
            if (restConfiguration != null)
            {
                this.m_oldConfiguration = new RestServiceConfiguration(restConfiguration);
            }

            if (restConfiguration != null && restConfiguration.ServiceType == null)
            {
                restConfiguration.ServiceType = this.m_behaviorType;
            }
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
                {
                    restConfiguration = RestServiceConfiguration.Load(s);
                    restConfiguration.ServiceType = this.m_behaviorType;
                }
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

    /// <summary>
    /// REST Service Configuration Uninstall task
    /// </summary>
    internal class RestServiceConfigurationUninstallTask : IConfigurationTask
    {
        /// <summary>
        /// Configuration section 
        /// </summary>
        private object m_configurationSection;

        /// <summary>
        /// Constructs a new rest service configuration uninstall task
        /// </summary>
        public RestServiceConfigurationUninstallTask(RestServiceFeature feature, object configurationSection)
        {
            this.m_configurationSection = configurationSection;
            this.Feature = feature;
        }

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description => $"Removes {this.Feature.Name} service registration";

        /// <summary>
        /// Gets the feature this is connected to
        /// </summary>
        public IFeature Feature { get; }

        /// <summary>
        /// Remove the feature
        /// </summary>
        public string Name => $"Remove {this.Feature.Name} Service";

        /// <summary>
        /// Fired when progress changes
        /// </summary>
        public event EventHandler<SanteDB.Core.Services.ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Execute the change
        /// </summary>
        public bool Execute(SanteDBConfiguration configuration)
        {
            configuration.Sections.RemoveAll(o => o.GetType() == this.m_configurationSection.GetType());
            return true;
        }

        /// <summary>
        /// Rollback the change/install
        /// </summary>
        public bool Rollback(SanteDBConfiguration configuration)
        {
            configuration.Sections.Add(this.m_configurationSection);
            return true;
        }

        /// <summary>
        /// Verify this action can occur
        /// </summary>
        public bool VerifyState(SanteDBConfiguration configuration) => configuration.Sections.Any(o => o.GetType() == this.m_configurationSection.GetType());
    }

    /// <summary>
    /// Remove the rest endpoint from the rest configuration
    /// </summary>
    internal class RestEndpointUninstallTask : IConfigurationTask
    {
        // The rest configuration
        private RestServiceConfiguration m_restServiceConfiguration;

        // Service
        private RestServiceFeature m_feature;

        /// <summary>
        /// Creates a new endpoint uninstall task
        /// </summary>
        public RestEndpointUninstallTask(RestServiceFeature feature, RestServiceConfiguration restServiceConfiguration)
        {
            this.m_restServiceConfiguration = restServiceConfiguration;
            this.Feature = this.m_feature = feature;
        }

        /// <summary>
        /// Gets the description of this object
        /// </summary>
        public string Description => $"Removes the HTTP binding for {this.Feature.Name} service";

        /// <summary>
        /// Gets the feature 
        /// </summary>
        public IFeature Feature { get; }

        /// <summary>
        /// Gets the name of the feature
        /// </summary>
        public string Name => $"Remove {this.Feature.Name}";

        /// <summary>
        /// Progress has changed
        /// </summary>
        public event EventHandler<SanteDB.Core.Services.ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Execute the uninstall procedure
        /// </summary>
        public bool Execute(SanteDBConfiguration configuration)
        {
            var restSection = configuration.GetSection<SanteDB.Rest.Common.Configuration.RestConfigurationSection>();
            if (restSection != null)
            {
                this.ProgressChanged?.Invoke(this, new SanteDB.Core.Services.ProgressChangedEventArgs(0.33f, null));
                restSection.Services.RemoveAll(o => o.ServiceType == this.m_restServiceConfiguration.ServiceType);
            }

            // Remove the HTTP bindings
            foreach (var ep in this.m_restServiceConfiguration.Endpoints)
            {
                Uri address = new Uri(ep.Address);
                if (address.Scheme == "https" && ep.CertificateBinding != null)
                {
                    try
                    {
                        // Reserve the SSL certificate on the IP address
                        if (address.HostNameType == UriHostNameType.Dns)
                        {
                            var ipAddresses = Dns.GetHostAddresses(address.Host);
                            HttpSslTool.RemoveCertificate(ipAddresses[0], address.Port, ep.CertificateBinding.Certificate.GetCertHash(), ep.CertificateBinding.StoreName, ep.CertificateBinding.StoreLocation);
                        }
                        else
                        {
                            HttpSslTool.RemoveCertificate(IPAddress.Parse(address.Host), address.Port, ep.CertificateBinding.Certificate.GetCertHash(), ep.CertificateBinding.StoreName, ep.CertificateBinding.StoreLocation);
                        }
                    }
                    catch (Exception e)
                    {
                        this.m_feature.m_tracer.TraceError($"Warning: Could not un-bind SSL certificate {ep.CertificateBinding.FindValue} to {ep.Address} - you can manually bind this certificate using netsh http add sslcert - Error: {e.Message}");
                        this.m_feature.m_tracer.TraceWarning($"Run: netsh http remove sslcert ipport={address.Host}:{address.Port} certhash={ep.CertificateBinding.FindValue} appid={{{{21F35B18-E417-4F8E-B9C7-73E98B7C71B8}}}}");
                    }
                }
            }
            this.ProgressChanged?.Invoke(this, new SanteDB.Core.Services.ProgressChangedEventArgs(0.66f, null));
            return true;
        }

        /// <summary>
        /// Rollback the changes to the configuration
        /// </summary>
        public bool Rollback(SanteDBConfiguration configuration)
        {
            var restSection = configuration.GetSection<SanteDB.Rest.Common.Configuration.RestConfigurationSection>();
            if (restSection != null &&
                !restSection.Services.Any(o => o.ServiceType == this.m_restServiceConfiguration.ServiceType))
            {
                restSection.Services.Add(this.m_restServiceConfiguration);
            }
            return true;
        }

        /// <summary>
        /// Verify that the object can be removed
        /// </summary>
        public bool VerifyState(SanteDBConfiguration configuration)
        {
            var restSection = configuration.GetSection<SanteDB.Rest.Common.Configuration.RestConfigurationSection>();
            return restSection != null &&
                restSection.Services.Any(o => o.Name == this.m_restServiceConfiguration.Name);
        }
    }

    /// <summary>
    /// Uninstall the message daemon
    /// </summary>
    internal class RestMessageDaemonUninstallTask : IConfigurationTask
    {
        // Service type
        private Type m_serviceType;

        /// <summary>
        /// Creates a new feature
        /// </summary>
        public RestMessageDaemonUninstallTask(RestServiceFeature feature, Type serviceType)
        {
            this.m_serviceType = serviceType;
            this.Feature = feature;
        }

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description => $"Removes the daemon service which runs {this.Feature.Name}";

        /// <summary>
        /// Get the feature bound to this task
        /// </summary>
        public IFeature Feature { get; }

        /// <summary>
        /// Gets the name of the feature
        /// </summary>
        public string Name => $"Remove {this.Feature.Name} Daemon";

        /// <summary>
        /// Fired when progress has changed
        /// </summary>
        public event EventHandler<SanteDB.Core.Services.ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Execute the uninstall
        /// </summary>
        public bool Execute(SanteDBConfiguration configuration)
        {
            var appServiceSection = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
            appServiceSection.ServiceProviders.RemoveAll(o => o.Type == this.m_serviceType);
            return true;
        }

        /// <summary>
        /// Rollback the removal of the service
        /// </summary>
        public bool Rollback(SanteDBConfiguration configuration)
        {
            var appServiceSection = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
            if (!appServiceSection.ServiceProviders.Any(o => o.Type == this.m_serviceType))
            {
                appServiceSection.ServiceProviders.Add(new TypeReferenceConfiguration(this.m_serviceType));
            }
            return true;
        }

        /// <summary>
        /// Verify the state
        /// </summary>
        public bool VerifyState(SanteDBConfiguration configuration)
        {
            var appServiceSection = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
            return appServiceSection.ServiceProviders.Any(o => o.Type == this.m_serviceType);
        }
    }

    /// <summary>
    /// A configuration task which sets up the service configuration
    /// </summary>
    internal class RestServiceConfigurationInstallTask : IConfigurationTask
    {
        // The configuration object
        private object m_serviceConfigurationObject;

        /// <summary>
        /// Creates a new rest service configuration installation task
        /// </summary>
        public RestServiceConfigurationInstallTask(RestServiceFeature feature, object serviceConfiguration)
        {
            this.m_serviceConfigurationObject = serviceConfiguration;
            this.Feature = feature;
        }

        /// <summary>
        /// Gets a description of this feature
        /// </summary>
        public string Description => $"Adds the configuration for {this.Feature.Name} to the application configuration file";

        /// <summary>
        /// Gets the feature to which this task is bound
        /// </summary>
        public IFeature Feature { get; }

        /// <summary>
        /// Gets the name of the feature
        /// </summary>
        public string Name => $"Add {this.Feature.Name} Configuration";

        /// <summary>
        /// Fired when the progress has changed
        /// </summary>
        public event EventHandler<SanteDB.Core.Services.ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Execute the operation
        /// </summary>
        public bool Execute(SanteDBConfiguration configuration)
        {
            configuration.Sections.RemoveAll(o => o.GetType() == this.m_serviceConfigurationObject.GetType());
            configuration.AddSection(this.m_serviceConfigurationObject);
            return true;
        }

        /// <summary>
        /// Rollback the configuration task
        /// </summary>
        public bool Rollback(SanteDBConfiguration configuration)
        {
            configuration.Sections.RemoveAll(o => o.GetType() == this.m_serviceConfigurationObject.GetType());
            return true;
        }

        /// <summary>
        /// Verify this task can be executed
        /// </summary>
        public bool VerifyState(SanteDBConfiguration configuration) => true;

    }

    /// <summary>
    /// A configuration task which setups the rest endpoint
    /// </summary>
    internal class RestEndpointInstallTask : IConfigurationTask
    {

        // The feature
        private readonly RestServiceFeature m_feature;

        // The service configuration
        private RestServiceConfiguration m_restServiceConfiguration;

        public RestEndpointInstallTask(RestServiceFeature feature, RestServiceConfiguration restServiceConfiguration)
        {
            this.m_restServiceConfiguration = restServiceConfiguration;
            this.m_feature = feature;
        }

        /// <summary>
        /// Gets the description of this task
        /// </summary>
        public string Description => $"Adds the REST endpoint and HTTP binding for {this.Feature.Name}";

        /// <summary>
        /// Gets the feature to which this task is associated
        /// </summary>
        public IFeature Feature => this.m_feature;

        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name => $"Register HTTP binding for {this.Feature.Name}";

        /// <summary>
        /// Fired when the progress of this operation changes
        /// </summary>
        public event EventHandler<SanteDB.Core.Services.ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Execute the configuration operation
        /// </summary>
        public bool Execute(SanteDBConfiguration configuration)
        {
            var restSection = configuration.GetSection<SanteDB.Rest.Common.Configuration.RestConfigurationSection>();
            if (restSection == null)
            {
                restSection = new SanteDB.Rest.Common.Configuration.RestConfigurationSection();
                configuration.AddSection(restSection);
            }

            restSection.Services.RemoveAll(o => o.ServiceType == this.m_restServiceConfiguration.ServiceType);
            restSection.Services.Add(this.m_restServiceConfiguration);

            // Remove the HTTP bindings
            this.ProgressChanged?.Invoke(this, new SanteDB.Core.Services.ProgressChangedEventArgs(0.5f, "Binding Certificates"));
            foreach (var ep in this.m_restServiceConfiguration.Endpoints)
            {
                Uri address = new Uri(ep.Address);
                if (address.Scheme == "https" && Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    if (ep.CertificateBinding == null || String.IsNullOrEmpty(ep.CertificateBinding.FindValue))
                    {
                        throw new InvalidOperationException($"Endpoint binding for {ep.Address} requires a certificate binding if HTTPS is used as the scheme");
                    }
                    try
                    {
                        // Reserve the SSL certificate on the IP address
                        ep.CertificateBinding.StoreLocationSpecified = ep.CertificateBinding.StoreNameSpecified = ep.CertificateBinding.FindTypeSpecified = true;
                        if (address.HostNameType == UriHostNameType.Dns)
                        {
                            var ipAddresses = Dns.GetHostAddresses(address.Host);
                            HttpSslTool.BindCertificate(ipAddresses[0], address.Port, ep.CertificateBinding.Certificate.GetCertHash(), ep.CertificateBinding.StoreName, ep.CertificateBinding.StoreLocation);
                        }
                        else
                        {
                            HttpSslTool.BindCertificate(IPAddress.Parse(address.Host), address.Port, ep.CertificateBinding.Certificate.GetCertHash(), ep.CertificateBinding.StoreName, ep.CertificateBinding.StoreLocation);
                        }
                    }
                    catch (Exception e)
                    {
                        this.m_feature.m_tracer.TraceError($"Warning: Could not bind SSL certificate {ep.CertificateBinding.FindValue} to {ep.Address} - you can manually bind this certificate using netsh http add sslcert - Error: {e.Message}");
                        this.m_feature.m_tracer.TraceWarning($"Run: netsh http add sslcert ipport={address.Host}:{address.Port} certhash={ep.CertificateBinding.FindValue} appid={{{{21F35B18-E417-4F8E-B9C7-73E98B7C71B8}}}}");
                    }
                }
                else
                {
                    ep.CertificateBinding = null;
                }
            }

            // Set backup
            (this.Feature as RestServiceFeature).SetOldConfiguration(this.m_restServiceConfiguration);
            return true;
        }

        /// <summary>
        /// Rollback the configuration (in case of an error)
        /// </summary>
        public bool Rollback(SanteDBConfiguration configuration)
        {
            var restSection = configuration.GetSection<SanteDB.Rest.Common.Configuration.RestConfigurationSection>();
            if (restSection != null)
            {
                restSection.Services.RemoveAll(o => o.Name == this.m_restServiceConfiguration.Name);
            }
            return true;
        }

        /// <summary>
        /// Verify the status of this installation task
        /// </summary>
        public bool VerifyState(SanteDBConfiguration configuration) => true;
    }

    /// <summary>
    /// An installation taks which registers the daemon type
    /// </summary>
    internal class RestMessageDaemonInstallTask : IConfigurationTask
    {
        // The service daemon type
        private Type m_serviceType;

        /// <summary>
        /// Creates a new instance of the installation task
        /// </summary>
        public RestMessageDaemonInstallTask(RestServiceFeature feature, Type serviceType)
        {
            this.m_serviceType = serviceType;
            this.Feature = feature;
        }

        /// <summary>
        /// Gets the description for this task
        /// </summary>
        public string Description => $"Register Daemon service for REST service {this.Feature.Name}";

        /// <summary>
        /// Gets the feature
        /// </summary>
        public IFeature Feature { get; }

        /// <summary>
        /// Gets the name of the feature
        /// </summary>
        public string Name => $"Register {this.Feature.Name} daemon";

        /// <summary>
        /// Fired when progress has changed
        /// </summary>
        public event EventHandler<SanteDB.Core.Services.ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Execute the feature install 
        /// </summary>
        public bool Execute(SanteDBConfiguration configuration)
        {
            var appServiceSection = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
            if (!appServiceSection.ServiceProviders.Any(o => o.Type == this.m_serviceType))
            {
                appServiceSection.ServiceProviders.Add(new TypeReferenceConfiguration(this.m_serviceType));
            }
            return true;
        }

        /// <summary>
        /// Rollback the configuration
        /// </summary>
        public bool Rollback(SanteDBConfiguration configuration)
        {
            var appServiceSection = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
            appServiceSection.ServiceProviders.RemoveAll(o => o.Type == this.m_serviceType);
            return true;
        }

        /// <summary>
        /// Verify that this can be installed
        /// </summary>
        public bool VerifyState(SanteDBConfiguration configuration) => configuration.GetSection<ApplicationServiceContextConfigurationSection>()?.ServiceProviders.Any(o => o.Type == this.m_serviceType) != true;
    }
}
