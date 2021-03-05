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
using SanteDB.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Data;
using SanteDB.Server.Core.Configuration.Tasks;
using SanteDB.OrmLite.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SanteDB.Core.Interfaces;
using SanteDB.Core;

namespace SanteDB.Configurator
{
    public partial class frmInitialConfig : Form
    {
        public frmInitialConfig()
        {
            InitializeComponent();
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
            // Create a default configuration with minimal sections
            ConfigurationContext.Current.Configuration = new SanteDBConfiguration()
            {
                Sections = new List<object>()
                {
                    new DataConfigurationSection(),
                    new DiagnosticsConfigurationSection(),
                    new ApplicationServiceContextConfigurationSection()
                    {
                        ThreadPoolSize = Environment.ProcessorCount
                    },
                    new OrmLite.Configuration.OrmConfigurationSection()
                    {
                        Providers = ConfigurationContext.Current.DataProviders.Select(o => new OrmLite.Configuration.ProviderRegistrationConfiguration(o.Invariant, o.DbProviderType)).ToList(),
                        AdoProvider = ConfigurationContext.Current.GetService<IServiceManager>().GetAllTypes().Where(t => typeof(DbProviderFactory).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface ).Select(t => new ProviderRegistrationConfiguration(t.Namespace.StartsWith("System") ? t.Name : t.Namespace.Split('.')[0], t)).ToList(),
                    }
                }
            };

            // Push the initial configuration features onto the service
            if(rdoEasy.Checked)
            {
                // Create feature
                dbSelector.ConnectionString.Name = "main";
                ConfigurationContext.Current.Configuration.GetSection<DataConfigurationSection>().ConnectionString.Add(dbSelector.ConnectionString);

                // Configuration of windows service parameters
                ConfigurationContext.Current.Features.OfType<WindowsServiceFeature>().FirstOrDefault().Configuration = new WindowsServiceFeature.Options()
                {
                    ServiceName = txtInstance.Text,
                    StartBehavior = ServiceTools.ServiceBootFlag.AutoStart
                };

                // Set all data connections
                var autoFeatures = ConfigurationContext.Current.Features.Where(o => o.Flags.HasFlag(FeatureFlags.AutoSetup));
                foreach(var ftr in autoFeatures)
                {
                    var ormConfig = (ftr.Configuration as OrmLite.Configuration.OrmConfigurationBase);
                    if(ormConfig != null)
                    {
                        ormConfig.ReadWriteConnectionString = ormConfig.ReadonlyConnectionString = dbSelector.ConnectionString.Name;
                        ormConfig.ProviderType = dbSelector.Provider.Invariant;
                        ormConfig.TraceSql = false;
                    }
                    // Add configuration 
                    foreach(var tsk in ftr.CreateInstallTasks().Where(o=>o.VerifyState(ConfigurationContext.Current.Configuration)))
                        ConfigurationContext.Current.ConfigurationTasks.Add(tsk);
                }

                ConfigurationContext.Current.Apply();
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
