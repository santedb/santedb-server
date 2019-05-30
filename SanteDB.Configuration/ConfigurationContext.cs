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
 * Date: 2019-3-2
 */
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Data;
using SanteDB.Core.Configuration.Features;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SanteDB.Configuration
{
    /// <summary>
    /// Configuration Context
    /// </summary>
    public class ConfigurationContext : INotifyPropertyChanged, IApplicationServiceContext, IConfigurationManager, IServiceManager
    {


        /// <summary>
        /// Services
        /// </summary>
        private List<Object> m_services = new List<object>();

        // Configuration
        private SanteDBConfiguration m_configuration;
        // Current context
        private static ConfigurationContext m_current;
        // Providers
        private IEnumerable<IDataConfigurationProvider> m_providers;
        // Features
        private IEnumerable<IFeature> m_features;

        /// <summary>
        /// Gets the configuration file name
        /// </summary>
        public String ConfigurationFile
        {
            get
            {
#if DEBUG
                return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "santedb.config.debug.xml");
#else
                return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "santedb.config.xml");
#endif 
            }
        }
        /// <summary>
        /// Gets the plugin assemblies
        /// </summary>
        public ObservableCollection<Assembly> PluginAssemblies { get; }

        /// <summary>
        /// Gets or sets the configuration handler
        /// </summary>
        public SanteDBConfiguration Configuration
        {
            get => this.m_configuration;
            set
            {
                this.m_configuration = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Configuration)));
            }
        }

        /// <summary>
        /// Gets the features available in the current version of the object
        /// </summary>
        public IEnumerable<IFeature> Features
        {
            get
            {
                if (this.m_features == null)
                {
                    this.m_features = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(o => !o.IsDynamic)
                        .SelectMany(a => { try { return a.ExportedTypes; } catch { return new List<Type>(); } })
                        .Where(t => typeof(IFeature).IsAssignableFrom(t) && !t.ContainsGenericParameters && !t.IsAbstract && !t.IsInterface).Select(i => Activator.CreateInstance(i) as IFeature).ToList();
                }
                return this.m_features;
            }
        }

        /// <summary>
        /// Get the configuration tasks
        /// </summary>
        public ObservableCollection<IConfigurationTask> ConfigurationTasks
        {
            get;
            private set;
        }

        /// <summary>
        /// Property has changed
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Firedwhenstarting
        /// </summary>
        public event EventHandler Starting;
        /// <summary>
        /// Fired when started
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Fired when stopping
        /// </summary>
        public event EventHandler Stopping;
        /// <summary>
        /// Fired when stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Private CTOR for singleton
        /// </summary>
        private ConfigurationContext()
        {
            this.PluginAssemblies = new ObservableCollection<Assembly>();
            this.ConfigurationTasks = new ObservableCollection<IConfigurationTask>();
        }

        /// <summary>
        /// Get the data providers 
        /// </summary>
        public IEnumerable<IDataConfigurationProvider> DataProviders
        {
            get
            {
                if (this.m_providers == null)
                    this.m_providers = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(o => !o.IsDynamic)
                        .SelectMany(a => { try { return a.ExportedTypes; } catch { return new List<Type>(); } })
                        .Where(t => t != null && typeof(IDataConfigurationProvider).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                        .Select(i => Activator.CreateInstance(i) as IDataConfigurationProvider)
                        .ToArray();
                return this.m_providers;

            }
        }

        /// <summary>
        /// Get the current configuration context
        /// </summary>
        public static ConfigurationContext Current
        {
            get
            {
                if (m_current == null)
                    m_current = new ConfigurationContext();
                return m_current;
            }
        }

        /// <summary>
        /// Get whether the object is running
        /// </summary>
        public bool IsRunning => true;

        /// <summary>
        /// Get the operating system
        /// </summary>
        public OperatingSystemID OperatingSystem => OperatingSystemID.Other;

        /// <summary>
        /// Host type
        /// </summary>
        public SanteDBHostType HostType => SanteDBHostType.Other;

        /// <summary>
        /// Service name
        /// </summary>
        public string ServiceName => "Configurator Manager";

        /// <summary>
        /// Load the specified configuration into this context
        /// </summary>
        public bool LoadConfiguration(string filename)
        {
            try
            {
                using (var s = File.OpenRead(filename))
                    this.Configuration = SanteDBConfiguration.Load(s);
                return true;
            }
            catch (Exception e)
            {
                Trace.TraceError("Could not load configuration file {0}: {1}", filename, e);
                return false;
            }
        }

        /// <summary>
        /// Restart the service context
        /// </summary>
        public void RestartContext()
        {
            this.Stop();
            this.Start();
        }

        /// <summary>
        /// Stop the service
        /// </summary>
        public void Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);

            foreach (var itm in this.m_services.OfType<IDisposable>())
            {
                itm.Dispose();
                Application.DoEvents();
            }
            this.m_services.Clear();

            this.Stopped?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Start this service
        /// </summary>
        public void Start()
        {
            this.Starting?.Invoke(this, EventArgs.Empty);

            foreach (var itm in this.Configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders)
            {
                this.GetService(itm.Type);
                Application.DoEvents();
            }

            foreach (var itm in this.Configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.Where(s => typeof(IDaemonService).IsAssignableFrom(s.Type)))
            {
                var svc = this.GetService(itm.Type) as IDaemonService;
                Application.DoEvents();
                svc.Start();
            }

            this.Started?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Get the specified service
        /// </summary>
        public object GetService(Type serviceType)
        {
            if (serviceType.IsAssignableFrom(typeof(ConfigurationContext)))
                return this;
            else
            {
                var candidate = this.m_services.FirstOrDefault(o => serviceType.IsAssignableFrom(o.GetType()));
                if (candidate == null)
                {
                    var dt = this.Configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.FirstOrDefault(o => serviceType.IsAssignableFrom(o.Type))?.Type;
                    if (dt != null)
                        candidate = Activator.CreateInstance(dt);
                    if (candidate != null)
                        this.m_services.Add(candidate);
                }
                return candidate;
            }
        }

        /// <summary>
        /// Get the specified section
        /// </summary>
        public T GetSection<T>() where T : IConfigurationSection
        {
            return this.Configuration.GetSection<T>();
        }

        /// <summary>
        /// Get the specified app setting
        /// </summary>
        public string GetAppSetting(string key)
        {
            return this.Configuration.GetSection<ApplicationServiceContextConfigurationSection>().AppSettings.FirstOrDefault(o => o.Key == key)?.Value;
        }

        /// <summary>
        /// Get the specified connection string
        /// </summary>
        public ConnectionString GetConnectionString(string key)
        {
            return this.Configuration.GetSection<DataConfigurationSection>().ConnectionString.FirstOrDefault(o => o.Name == key);

        }

        /// <summary>
        /// Set an application setting
        /// </summary>
        public void SetAppSetting(string key, string value)
        {
            this.Configuration.GetSection<ApplicationServiceContextConfigurationSection>().AppSettings.RemoveAll(o => o.Key == key);
            this.Configuration.GetSection<ApplicationServiceContextConfigurationSection>().AppSettings.Add(new AppSettingKeyValuePair()
            {
                Key = key,
                Value = value
            });
        }

        /// <summary>
        /// Reload the specified configuration file
        /// </summary>
        public void Reload()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Add a service provider to this context
        /// </summary>
        public void AddServiceProvider(Type serviceType)
        {
            this.m_services.Add(Activator.CreateInstance(serviceType));
        }

        /// <summary>
        /// Get all services
        /// </summary>
        public IEnumerable<object> GetServices()
        {
            return this.m_services;
        }

        /// <summary>
        /// Remove a service provider
        /// </summary>
        public void RemoveServiceProvider(Type serviceType)
        {
            this.m_services.RemoveAll(o => o.GetType() == serviceType);
        }

        /// <summary>
        /// Get all types
        /// </summary>
        public IEnumerable<Type> GetAllTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(o => { try { return o.ExportedTypes; } catch { return new List<Type>(); } }); // HACK: Mono does not like all assemblies
        }
    }
}
