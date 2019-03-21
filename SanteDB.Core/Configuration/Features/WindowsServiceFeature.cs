/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
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
 * User: JustinFyfe
 * Date: 2019-2-28
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Services;
using ServiceTools;

namespace SanteDB.Core.Configuration.Tasks
{

    /// <summary>
    /// Represents a feature which is a windows service installer
    /// </summary>
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
        public String Group => "Daemons";

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
            [Description("The name of the windows service")]
            public String ServiceName { get; set; }

            /// <summary>
            /// Gets or sets the user
            /// </summary>
            [Description("Identifies the logon user for the windows service")]
            public String User { get; set; }

            /// <summary>
            /// Gets or sets the password
            /// </summary>
            [Description("Identifies the password for the login user")]
            [PasswordPropertyText]
            public String Password { get; set; }

            /// <summary>
            /// Gets or sets the boot behavior
            /// </summary>
            [Description("Identifies the boot behavior")]
            public ServiceBootFlag StartBehavior { get; set; }
        }

        /// <summary>
        /// Configuration task
        /// </summary>
        public class InstallTask : IConfigurationTask
        {

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
            public event EventHandler<Services.ProgressChangedEventArgs> ProgressChanged;

            /// <summary>
            /// Execute the installation task
            /// </summary>
            public bool Execute(SanteDBConfiguration configuration)
            {
                this.ProgressChanged?.Invoke(this, new Services.ProgressChangedEventArgs(0.0f, $"Installing Windows Service {this.m_options.ServiceName}..."));
                if (!ServiceInstaller.ServiceIsInstalled(this.m_options.ServiceName))
                {
                    ServiceInstaller.Install(this.m_options.ServiceName, "SanteDB Host Process",
                        Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "santedb.exe"),
                        this.m_options.User,
                        this.m_options.Password,
                        this.m_options.StartBehavior);
                    configuration.GetSection<ApplicationServiceContextConfigurationSection>().AppSettings.Add(new AppSettingKeyValuePair("w32instance.name", this.m_options.ServiceName));
                }
                this.ProgressChanged?.Invoke(this, new Services.ProgressChangedEventArgs(1.0f, null));
                return true;
            }

            /// <summary>
            /// Rollback 
            /// </summary>
            public bool Rollback(SanteDBConfiguration configuration)
            {
                if (ServiceInstaller.ServiceIsInstalled(this.m_options.ServiceName))
                {
                    ServiceInstaller.StopService(this.m_options.ServiceName);
                    ServiceInstaller.Uninstall(this.m_options.ServiceName);
                    configuration.GetSection<ApplicationServiceContextConfigurationSection>().AppSettings.RemoveAll(o => o.Key == "w32instance.name");
                }
                return true;
            }

            /// <summary>
            /// Verify state
            /// </summary>
            public bool VerifyState(SanteDBConfiguration configuration) {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                if (Environment.OSVersion.Platform == PlatformID.Win32NT && 
                    principal.IsInRole(WindowsBuiltInRole.Administrator))
                    return ServiceInstaller.ServiceIsInstalled(this.m_options.ServiceName);
                return false;
            }
        }

        /// <summary>
        /// Configuration task
        /// </summary>
        public class UninstallTask : IConfigurationTask
        {
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
            public event EventHandler<Services.ProgressChangedEventArgs> ProgressChanged;

            /// <summary>
            /// Execute the installation task
            /// </summary>
            public bool Execute(SanteDBConfiguration configuration)
            {
                this.ProgressChanged?.Invoke(this, new Services.ProgressChangedEventArgs(0.0f, $"Removing Windows Service {this.m_options.ServiceName}..."));
                if (ServiceInstaller.ServiceIsInstalled(this.m_options.ServiceName))
                {
                    ServiceInstaller.StopService(this.m_options.ServiceName);
                    ServiceInstaller.Uninstall(this.m_options.ServiceName);
                    configuration.GetSection<ApplicationServiceContextConfigurationSection>().AppSettings.RemoveAll(o => o.Key == "w32instance.name");
                }
                this.ProgressChanged?.Invoke(this, new Services.ProgressChangedEventArgs(1.0f, null));
                return true;
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
            public bool VerifyState(SanteDBConfiguration configuration) => ServiceInstaller.ServiceIsInstalled(this.m_options.ServiceName);

        }
    }
}
