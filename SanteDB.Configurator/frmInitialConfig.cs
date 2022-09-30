/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using SanteDB.Configuration;
using SanteDB.Configuration.Tasks;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Data;
using SanteDB.OrmLite.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace SanteDB.Configurator
{
    [ExcludeFromCodeCoverage]
    public partial class frmInitialConfig : Form
    {
        public frmInitialConfig()
        {
            InitializeComponent();
            InitializeTemplates();
        }

        /// <summary>
        /// Initialize templates
        /// </summary>
        private void InitializeTemplates()
        {
            try
            {
                cbxTemplate.Items.Clear();
                foreach (var f in Directory.GetFiles(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "config", "template"), "*.xml"))
                {
                    cbxTemplate.Items.Add(Path.GetFileNameWithoutExtension(f));
                }
            }
            catch { }
        }

        private void rdoEasy_CheckedChanged(object sender, EventArgs e)
        {
            groupBox1.Enabled = rdoEasy.Checked;
            btnContinue.Enabled = !rdoEasy.Checked && dbSelector.Validate();
        }

        /// <summary>
        /// Fired when configuration changed
        /// </summary>
        private void dbSelector_Configured(object sender, EventArgs e)
        {
            btnContinue.Enabled = dbSelector.IsConfigured;
        }

        /// <summary>
        /// Continue with configuration
        /// </summary>
        private void btnContinue_Click(object sender, EventArgs e)
        {

            ConfigurationContext.Current.ConfigurationTasks.Clear();
            this.Hide();
            // Create a default configuration with minimal sections
            if (cbxTemplate.SelectedItem != null)
            {
                try
                {
                    var fileName = Path.ChangeExtension(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "config", "template", cbxTemplate.SelectedItem.ToString()), "xml");
                    using (var s = File.OpenRead(fileName))
                    {
                        ConfigurationContext.Current.Configuration = SanteDBConfiguration.Load(s);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading template: {ex.Message}");
                }
            }
            else
            {
                ConfigurationContext.Current.Configuration = new SanteDBConfiguration()
                {
                    Sections = new List<object>()
                    {
                        new DataConfigurationSection(),
                        new DiagnosticsConfigurationSection(),
                        new ApplicationServiceContextConfigurationSection()
                        {
                            ThreadPoolSize = Environment.ProcessorCount * 16
                        }
                    }
                };
            }

            ConfigurationContext.Current.Configuration.RemoveSection<OrmLite.Configuration.OrmConfigurationSection>();
            ConfigurationContext.Current.Configuration.AddSection(new OrmLite.Configuration.OrmConfigurationSection()
            {
                Providers = ConfigurationContext.Current.DataProviders.Select(o => new OrmLite.Configuration.ProviderRegistrationConfiguration(o.Invariant, o.DbProviderType)).ToList(),
                AdoProvider = ConfigurationContext.Current.GetAllTypes().Where(t => typeof(DbProviderFactory).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface).Select(t => new ProviderRegistrationConfiguration(t.Namespace.StartsWith("System") ? t.Name : t.Namespace.Split('.')[0], t)).ToList(),
            });

            // Push the initial configuration features onto the service
            if (rdoEasy.Checked)
            {
                // Create feature
                dbSelector.ConnectionString.Name = "main";
                var dataSection = ConfigurationContext.Current.Configuration.GetSection<DataConfigurationSection>();
                if (dataSection == null)
                {
                    dataSection = new DataConfigurationSection();
                    ConfigurationContext.Current.Configuration.AddSection(dataSection);
                }
                dataSection.ConnectionString.Clear();
                dataSection.ConnectionString.Add(dbSelector.ConnectionString);
                ConfigurationContext.Current.Configuration.Sections.OfType<OrmConfigurationBase>().ToList().ForEach(o =>
                {
                    o.ReadonlyConnectionString = o.ReadWriteConnectionString = "main";
                    o.ProviderType = dbSelector.Provider.Invariant;
                });
                // Configuration of windows service parameters
                ConfigurationContext.Current.Features.OfType<WindowsServiceFeature>().FirstOrDefault().Configuration = new WindowsServiceFeature.Options()
                {
                    ServiceName = txtInstance.Text,
                    StartBehavior = ServiceTools.ServiceBootFlag.AutoStart
                };

                // Set all data connections
                var autoFeatures = ConfigurationContext.Current.Features.Where(o => o.Flags.HasFlag(FeatureFlags.AutoSetup) && o.QueryState(ConfigurationContext.Current.Configuration) != FeatureInstallState.Installed);
                foreach (var ftr in autoFeatures)
                {
                    var ormConfig = (ftr.Configuration as OrmLite.Configuration.OrmConfigurationBase);
                    if (ormConfig != null)
                    {
                        ormConfig.ReadWriteConnectionString = ormConfig.ReadonlyConnectionString = dbSelector.ConnectionString.Name;
                        ormConfig.ProviderType = dbSelector.Provider.Invariant;
                        ormConfig.TraceSql = false;
                    }
                    // Add configuration
                    foreach (var tsk in ftr.CreateInstallTasks().Where(o => o.VerifyState(ConfigurationContext.Current.Configuration)))
                    {
                        ConfigurationContext.Current.ConfigurationTasks.Add(tsk);
                    }
                }

                ConfigurationContext.Current.Apply(this);
            }


            this.DialogResult = DialogResult.OK;
            this.Close();

        }

        /// <summary>
        /// Cancel initial configuration
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}