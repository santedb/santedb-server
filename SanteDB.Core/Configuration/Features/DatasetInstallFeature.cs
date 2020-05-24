/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using SanteDB.Core.Model.Export;
using SanteDB.Core.Persistence;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Configuration.Features
{
    /// <summary>
    /// Represents a dataset installation feature
    /// </summary>
    public class DatasetInstallFeature : IFeature
    {

        /// <summary>
        /// Gets the name 
        /// </summary>
        public string Name => "Data Update Service";

        /// <summary>
        /// Description of the feature
        /// </summary>
        public string Description => "Provides update services to the SanteDB server";

        /// <summary>
        /// Flags
        /// </summary>
        public FeatureFlags Flags => FeatureFlags.NoRemove | FeatureFlags.AutoSetup | FeatureFlags.AlwaysConfigure;

        /// <summary>
        /// Gets the group
        /// </summary>
        public string Group => FeatureGroup.System;

        /// <summary>
        /// Configuration type
        /// </summary>
        public Type ConfigurationType => null;

        /// <summary>
        /// Configuration
        /// </summary>
        public object Configuration { get; set; }

        /// <summary>
        /// Create the installation tasks
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateInstallTasks()
        {
            List<IConfigurationTask> retVal = new List<IConfigurationTask>()
            {
                new ServiceInstallTask(this)
            };

            // Get all updates
            foreach (var f in Directory.GetFiles(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "data"), "*.dataset").OrderBy(o=>Path.GetFileName(o)))
            {
                if (!File.Exists(Path.ChangeExtension(f, "completed")))
                {
                    try
                    {
                        using(var fs = File.OpenRead(f))
                            retVal.Add(new DatasetImportTask(this, Dataset.Load(fs), f));
                    }
                    catch(Exception e)
                    {
                        Trace.TraceError("Error loading dataset: {0}", e);
                    }
                }
            }

            return retVal;
        }

        /// <summary>
        /// Create uninstall tasks
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateUninstallTasks()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Query the state of this option
        /// </summary>
        public FeatureInstallState QueryState(SanteDBConfiguration configuration)
        {
            return configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.Any(o => o.Type == typeof(DataInitializationService)) ? FeatureInstallState.Installed : FeatureInstallState.NotInstalled;
        }

        /// <summary>
        /// Service installation task
        /// </summary>
        public class ServiceInstallTask : IDescribedConfigurationTask
        {

            /// <summary>
            /// Service installation task
            /// </summary>
            public ServiceInstallTask(DatasetInstallFeature feature)
            {
                this.Feature = feature;
            }

            /// <summary>
            /// Service task name
            /// </summary>
            public string Name => "Configure Updates";

            /// <summary>
            /// Gets the description
            /// </summary>
            public string Description => "Enables the automatic installation of new datasets located on the SanteDB server";

            /// <summary>
            /// Gets the feature
            /// </summary>
            public IFeature Feature { get; }

            /// <summary>
            /// Help URL
            /// </summary>
            public Uri HelpUri => new Uri("https://help.santesuite.org/ops/santedb/data/datasets");

            /// <summary>
            /// Additional information
            /// </summary>
            public string AdditionalInformation => "By enabling this feature, SanteDB will scan the Data directory of this installation and will import all of the dataset files located in that directory that have not been previously imported";

            /// <summary>
            /// Progress has changed
            /// </summary>
            public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

            /// <summary>
            /// Execute
            /// </summary>
            public bool Execute(SanteDBConfiguration configuration)
            {
                configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.RemoveAll(o => o.Type == typeof(DataInitializationService));
                configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.Add(new TypeReferenceConfiguration(typeof(DataInitializationService)));
                return true;
            }

            /// <summary>
            /// Rollback the changes
            /// </summary>
            public bool Rollback(SanteDBConfiguration configuration)
            {
                configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.RemoveAll(o => o.Type == typeof(DataInitializationService));
                return true;
            }

            /// <summary>
            /// Verify that this can be run
            /// </summary>
            /// <param name="configuration"></param>
            /// <returns></returns>
            public bool VerifyState(SanteDBConfiguration configuration) => !configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.Any(o => o.Type == typeof(DataInitializationService));
        }

        /// <summary>
        /// Dataset import task
        /// </summary>
        public class DatasetImportTask : IConfigurationTask
        {

            // Dataset to be installed
            private Dataset m_dataset;
            private string m_file;

            /// <summary>
            /// Feature
            /// </summary>
            public DatasetImportTask(DatasetInstallFeature feature, Dataset ds, String file)
            {
                this.Feature = feature;
                this.m_dataset = ds;
                this.m_file = file;
            }

            /// <summary>
            /// Gets the name of the task
            /// </summary>
            public string Name => $"Import {this.m_dataset.Id}";

            /// <summary>
            /// Gets the description of object
            /// </summary>
            public string Description => $"Import {this.m_dataset.Id} from {Path.GetFileNameWithoutExtension(this.m_file)}";

            /// <summary>
            /// Gets the feature
            /// </summary>
            public IFeature Feature { get; }

            /// <summary>
            /// Progress has changed
            /// </summary>
            public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

            /// <summary>
            /// Execute the change
            /// </summary>
            public bool Execute(SanteDBConfiguration configuration)
            {
                var dsi = new DataInitializationService();
                dsi.ProgressChanged += (o, e) => this.ProgressChanged?.Invoke(o, e); 
                dsi.InstallDataset(this.m_dataset);
                File.Move(this.m_file, Path.ChangeExtension(m_file, "completed"));
                return true;
            }

            /// <summary>
            /// Rollback the change
            /// </summary>
            public bool Rollback(SanteDBConfiguration configuration) => false;

            /// <summary>
            /// Verify state
            /// </summary>
            public bool VerifyState(SanteDBConfiguration configuration) => 
                configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.Any(o=>o.Type == typeof(DataInitializationService)) &&
                !File.Exists(Path.ChangeExtension(this.m_file, "completed"));
        }
    }
}
