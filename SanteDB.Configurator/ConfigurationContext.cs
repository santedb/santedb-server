using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Data;
using SanteDB.Core.Configuration.Features;
using SanteDB.Core.Configuration.Tasks;
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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SanteDB.Configurator
{
    /// <summary>
    /// Configuration Context
    /// </summary>
    public class ConfigurationContext : INotifyPropertyChanged, IApplicationServiceContext, IConfigurationManager
    {

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
        public SanteDBConfiguration Configuration {
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
                        .SelectMany(a => a.ExportedTypes)
                        .Where(t => typeof(IFeature).IsAssignableFrom(t) && !t.ContainsGenericParameters && !t.IsAbstract && !t.IsInterface).Select(i => Activator.CreateInstance(i) as IFeature).ToList();
                }
                return this.m_features;
            }
        } 

        /// <summary>
        /// Get the configuration tasks
        /// </summary>
        public List<IConfigurationTask> ConfigurationTasks
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
            this.ConfigurationTasks = new List<IConfigurationTask>();
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
                        .SelectMany(a => a.ExportedTypes)
                        .Where(t => typeof(IDataConfigurationProvider).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface).Select(i => Activator.CreateInstance(i) as IDataConfigurationProvider)
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
        /// Apply the tasks specified in the current task queue
        /// </summary>
        public void Apply()
        {

            if (this.ConfigurationTasks.Count == 0)
                return;

            var progress = new frmProgress();
            progress.Show();

            try
            {

                // Do work here
                int i = 0, t = this.ConfigurationTasks.Count;
                foreach(var ct in this.ConfigurationTasks)
                {
                    ct.ProgressChanged += (o, e) =>
                    {
                        progress.ActionStatusText = e.State?.ToString() ?? "...";
                        progress.ActionStatus = (int)(e.Progress * 100);
                        Application.DoEvents();
                    };

                    progress.OverallStatusText = $"Applying {ct.Feature.Name}";
                    ct.Execute(this.Configuration);
                    progress.OverallStatus = (int)(((float)++i / t) * 100.0);
                }

                using (var fs = File.OpenWrite(this.ConfigurationFile))
                    this.Configuration.Save(fs);
            }
            catch(Exception e)
            {
                MessageBox.Show($"Error applying configuration: {e.Message}");
            }
            progress.Close();
        }

        /// <summary>
        /// Get the specified service
        /// </summary>
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IConfigurationManager))
                return this;
            else
                return null;
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
    }
}
