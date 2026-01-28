/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using ServiceTools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;

namespace SanteDB.Configuration.Tasks
{
    /// <summary>
    /// Represents a feature which is a windows service installer
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class WindowsServiceFeature : IFeature
    {
        /// <summary>
        /// Windows service feature ctor
        /// </summary>
        public WindowsServiceFeature()
        {
            this.Configuration = new Options();
        }

        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name => "Windows Service";

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description => "Configures the Windows Service";

        /// <summary>
        /// Configuration type
        /// </summary>
        public Type ConfigurationType => typeof(Options);

        /// <summary>
        /// Flags for this configuration feature
        /// </summary>
        public FeatureFlags Flags => FeatureFlags.AutoSetup;

        /// <summary>
        /// Gets the group name
        /// </summary>
        public String Group => FeatureGroup.OperatingSystem;

        /// <summary>
        /// Gets or sets the configuration
        /// </summary>
        public object Configuration
        {
            get; set;
        }

        /// <summary>
        /// Create the installation task
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateInstallTasks()
        {
            return new IConfigurationTask[] { new InstallTask(this) };
        }

        /// <summary>
        /// Create tasks for uninstallation
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateUninstallTasks()
        {
            return new IConfigurationTask[] { new UninstallTask(this) };
        }

        /// <summary>
        /// Return true if the service is configured
        /// </summary>
        public FeatureInstallState QueryState(SanteDBConfiguration configuration)
        {
            var options = this.Configuration as Options;
            options.ServiceName = configuration.GetSection<ApplicationServiceContextConfigurationSection>().AppSettings.FirstOrDefault(o => o.Key == "w32instance.name")?.Value ?? options.ServiceName;
            var config = ServiceTools.ServiceInstaller.GetServiceConfig(options.ServiceName);
            options.StartBehavior = (ServiceBootFlag?)config?.dwStartType ?? ServiceBootFlag.AutoStart;
            return ServiceTools.ServiceInstaller.ServiceIsInstalled(options.ServiceName) ? FeatureInstallState.Installed : FeatureInstallState.NotInstalled;
        }

        /// <summary>
        /// Windows service parameters
        /// </summary>
        public class Options
        {
            /// <summary>
            /// Options
            /// </summary>
            public Options()
            {
                this.StartBehavior = ServiceBootFlag.AutoStart;
                this.ServiceName = "SanteDB";
            }

            /// <summary>
            /// Gets or sets the service name
            /// </summary>
            [DisplayName("Instance Name"), Description("The name of the windows service. Use this setting if you plan on running more than one copy of SanteDB on this server")]
            public String ServiceName { get; set; }

            /// <summary>
            /// Gets or sets the user
            /// </summary>
            [DisplayName("Service Account"), Description("Identifies the logon user for the windows service. Leave this setting empty if you're running as LOCAL SERVICE")]
            public String User { get; set; }

            /// <summary>
            /// Gets or sets the password
            /// </summary>
            [DisplayName("Service Account Password"), Description("Identifies the password for the login user. Leave this setting empty if you're running as LOCAL SERVICE")]
            [PasswordPropertyText]
            public String Password { get; set; }

            /// <summary>
            /// Gets or sets the boot behavior
            /// </summary>
            [DisplayName("Startup Behavior"), Description("Identifies the boot behavior")]
            public ServiceBootFlag StartBehavior { get; set; }
        }

        /// <summary>
        /// Configuration task
        /// </summary>
        public class InstallTask : IConfigurationTask
        {
            // Tracer
            private readonly Tracer m_tracer = new Tracer("Windows Service Installer");

            /// <summary>
            /// Get the name
            /// </summary>
            public String Name => "Install Windows Service";

            /// <summary>
            /// Gets the description
            /// </summary>
            public String Description => $"Install instance {this.m_options.ServiceName} on this machine";

            /// <summary>
            /// Options for configuration
            /// </summary>
            private Options m_options;

            /// <summary>
            /// Installation task
            /// </summary>
            public InstallTask(WindowsServiceFeature feature)
            {
                this.Feature = feature;
                this.m_options = feature.Configuration as Options;
            }

            /// <summary>
            /// Get the feature
            /// </summary>
            public IFeature Feature { get; }

            /// <summary>
            /// Progress has changed
            /// </summary>
            public event EventHandler<SanteDB.Core.Services.ProgressChangedEventArgs> ProgressChanged;

            /// <summary>
            /// Execute the installation task
            /// </summary>
            public bool Execute(SanteDBConfiguration configuration)
            {
                try
                {
                    this.ProgressChanged?.Invoke(this, new SanteDB.Core.Services.ProgressChangedEventArgs(nameof(InstallTask), 0.0f, $"Installing Windows Service {this.m_options.ServiceName}..."));
                    if (!ServiceTools.ServiceInstaller.ServiceIsInstalled(this.m_options.ServiceName))
                    {
                        ServiceTools.ServiceInstaller.Install(this.m_options.ServiceName, "SanteDB Host Process",
                            Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "santedb.exe"),
                            this.m_options.User,
                            this.m_options.Password,
                            this.m_options.StartBehavior);
                        configuration.GetSection<ApplicationServiceContextConfigurationSection>().InstanceName = this.m_options.ServiceName;
                    }
                    this.ProgressChanged?.Invoke(this, new SanteDB.Core.Services.ProgressChangedEventArgs(nameof(InstallTask), 1.0f, null));
                    return true;
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Could not install Windows Service {0} => {1}", this.m_options.ServiceName, e.Message);
                    return false;
                }
            }

            /// <summary>
            /// Rollback
            /// </summary>
            public bool Rollback(SanteDBConfiguration configuration)
            {
                if (ServiceTools.ServiceInstaller.ServiceIsInstalled(this.m_options.ServiceName))
                {
                    ServiceTools.ServiceInstaller.StopService(this.m_options.ServiceName);
                    ServiceTools.ServiceInstaller.Uninstall(this.m_options.ServiceName);
                    configuration.GetSection<ApplicationServiceContextConfigurationSection>().AppSettings.RemoveAll(o => o.Key == "w32instance.name");
                }
                return true;
            }

            /// <summary>
            /// Verify state
            /// </summary>
            public bool VerifyState(SanteDBConfiguration configuration)
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    return !ServiceTools.ServiceInstaller.ServiceIsInstalled(this.m_options.ServiceName);
                }

                return false;
            }
        }

        /// <summary>
        /// Configuration task
        /// </summary>
        public class UninstallTask : IConfigurationTask
        {
            // Tracer
            private readonly Tracer m_tracer = new Tracer("Windows Service Installer");

            /// <summary>
            /// Get the name
            /// </summary>
            public String Name => "Uninstall Windows Service";

            /// <summary>
            /// Gets the description
            /// </summary>
            public String Description => $"Remove instance {this.m_options} from this machine";

            /// <summary>
            /// Options for configuration
            /// </summary>
            private Options m_options;

            /// <summary>
            /// Installation task
            /// </summary>
            public UninstallTask(WindowsServiceFeature feature)
            {
                this.Feature = feature;
                this.m_options = feature.Configuration as Options;
            }

            /// <summary>
            /// Get the feature
            /// </summary>
            public IFeature Feature { get; }

            /// <summary>
            /// Progress has changed
            /// </summary>
            public event EventHandler<SanteDB.Core.Services.ProgressChangedEventArgs> ProgressChanged;

            /// <summary>
            /// Execute the installation task
            /// </summary>
            public bool Execute(SanteDBConfiguration configuration)
            {
                try
                {
                    this.ProgressChanged?.Invoke(this, new SanteDB.Core.Services.ProgressChangedEventArgs(nameof(UninstallTask), 0.0f, $"Removing Windows Service {this.m_options.ServiceName}..."));
                    if (ServiceTools.ServiceInstaller.ServiceIsInstalled(this.m_options.ServiceName))
                    {
                        ServiceTools.ServiceInstaller.StopService(this.m_options.ServiceName);
                        ServiceTools.ServiceInstaller.Uninstall(this.m_options.ServiceName);
                        configuration.GetSection<ApplicationServiceContextConfigurationSection>().AppSettings.RemoveAll(o => o.Key == "w32instance.name");
                    }
                    this.ProgressChanged?.Invoke(this, new SanteDB.Core.Services.ProgressChangedEventArgs(nameof(UninstallTask), 1.0f, null));
                    return true;
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Could not uninstall Windows Service {0} - {1}", this.m_options.ServiceName, e.Message);
                    return false;
                }
            }

            /// <summary>
            /// Rollback
            /// </summary>
            public bool Rollback(SanteDBConfiguration configuration)
            {
                return false;
            }

            /// <summary>
            /// Verify state
            /// </summary>
            public bool VerifyState(SanteDBConfiguration configuration) => ServiceTools.ServiceInstaller.ServiceIsInstalled(this.m_options.ServiceName);
        }
    }
}