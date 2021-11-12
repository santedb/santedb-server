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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Data;
using SanteDB.Core.Diagnostics;
using SanteDB.OrmLite.Configuration;
using SanteDB.OrmLite.Providers;

namespace SanteDB.Configuration
{
    /// <summary>
    /// Configuration Context
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConfigurationContext : INotifyPropertyChanged
    {
        // Configuration
        private SanteDBConfiguration m_configuration;

        // Current context
        private static ConfigurationContext m_current;

        // Providers
        private IEnumerable<IDataConfigurationProvider> m_providers;

        // Tracer provider
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(ConfigurationContext));

#if DEBUG
#else
        private string m_configurationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "santedb.config.xml");
#endif


        /// <summary>
        /// Gets the configuration file name
        /// </summary>
        public string ConfigurationFile { get; private set; } = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "santedb.config.debug.xml");

        /// <summary>
        /// Gets the plugin assemblies
        /// </summary>
        public ObservableCollection<Assembly> PluginAssemblies { get; }

        /// <summary>
        /// Gets the start time of the context
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// Gets or sets the configuration handler
        /// </summary>
        public SanteDBConfiguration Configuration
        {
            get => this.m_configuration;
            set
            {
                this.m_configuration = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Configuration)));
            }
        }

        /// <summary>
        /// Gets the features available in the current version of the object
        /// </summary>
        public List<IFeature> Features { get; }

        /// <summary>
        /// Initialize features
        /// </summary>
        public void InitializeFeatures()
        {
            this.Features.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                .Where(o => !o.IsDynamic)
                .SelectMany(a =>
                {
                    try
                    {
                        return a.ExportedTypes;
                    }
                    catch
                    {
                        return new List<Type>();
                    }
                })
                .Where(t => typeof(IFeature).IsAssignableFrom(t) && !t.ContainsGenericParameters && !t.IsAbstract && !t.IsInterface)
                .Select(i =>
                {
                    try
                    {
                        var feature = Activator.CreateInstance(i) as IFeature;
                        if (feature.Flags == FeatureFlags.NonPublic)
                        {
                            return null;
                        }

                        return feature;
                    }
                    catch (Exception e)
                    {
                        return null;
                    }
                })
                .OfType<IFeature>());
        }

        /// <summary>
        /// Get app setting
        /// </summary>
        public string GetAppSetting(string key)
        {
            // Use configuration setting 
            string retVal = null;
            try
            {
                retVal = this.Configuration.GetSection<ApplicationServiceContextConfigurationSection>()?.AppSettings.Find(o => o.Key == key)?.Value;
            }
            catch
            {
            }

            return retVal;
        }

        /// <summary>
        /// Initial configuration startup
        /// </summary>
        public void InitialStart()
        {
            // Default configuration
            this.Configuration = new SanteDBConfiguration();
            // Initial settings for initial 
            this.Configuration.AddSection(new OrmConfigurationSection
            {
                AdoProvider = this.GetAllTypes().Where(t => typeof(DbProviderFactory).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface).Select(t => new ProviderRegistrationConfiguration(t.Namespace.StartsWith("System") ? t.Name : t.Namespace.Split('.')[0], t)).ToList(),
                Providers = this.GetAllTypes().Where(t => typeof(IDbProvider).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface).Select(t => new ProviderRegistrationConfiguration((Activator.CreateInstance(t) as IDbProvider).Invariant, t)).ToList()
            });
            this.Started?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Get all types
        /// </summary>
        public IEnumerable<Type> GetAllTypes()
        {
            // HACK: The weird TRY/CATCH in select many is to prevent mono from throwning a fit
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a =>
                {
                    try
                    {
                        return a.ExportedTypes;
                    }
                    catch
                    {
                        return new List<Type>();
                    }
                });
        }


        /// <summary>
        /// Get the configuration tasks
        /// </summary>
        public ObservableCollection<IConfigurationTask> ConfigurationTasks { get; }

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
            this.Features = new List<IFeature>();
        }

        /// <summary>
        /// Get the data providers 
        /// </summary>
        public IEnumerable<IDataConfigurationProvider> DataProviders
        {
            get
            {
                if (this.m_providers == null)
                {
                    this.m_providers = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(o => !o.IsDynamic)
                        .SelectMany(a =>
                        {
                            try
                            {
                                return a.ExportedTypes;
                            }
                            catch
                            {
                                return new List<Type>();
                            }
                        })
                        .Where(t => t != null && typeof(IDataConfigurationProvider).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                        .Select(i => Activator.CreateInstance(i) as IDataConfigurationProvider)
                        .ToArray();
                }

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
                {
                    m_current = new ConfigurationContext();
                }

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
        public SanteDBHostType HostType => SanteDBHostType.Configuration;

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
                {
                    this.Configuration = SanteDBConfiguration.Load(s);
                    this.Features.Clear();
                    this.InitializeFeatures();
                    this.ConfigurationFile = filename;
                }

                return true;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Could not load configuration file {0}: {1}", filename, e);
                return false;
            }
        }
    }
}