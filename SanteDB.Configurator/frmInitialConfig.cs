using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

            // Create a default configuration with minimal sections
            ConfigurationContext.Current.Configuration = new SanteDBConfiguration()
            {
                Sections = new List<object>()
                {
                    new DataConfigurationSection(),
                    new DiagnosticsConfigurationSection(),
                    new ApplicationServiceContextConfigurationSection()
                }
            };

            // Push the initial configuration features onto the service
            if(rdoEasy.Checked)
            {
                // Create feature
                dbSelector.ConnectionString.Name = "main";
                ConfigurationContext.Current.Configuration.GetSection<DataConfigurationSection>().ConnectionString.Add(dbSelector.ConnectionString);
                
                // Set all data connections
                var autoFeatures = ConfigurationContext.Current.Features.Where(o => o.Flags.HasFlag(FeatureFlags.AutoSetup) );
                foreach(var ftr in autoFeatures)
                {
                    var ormConfig = (ftr.Configuration as OrmLite.Configuration.OrmConfigurationBase);
                    if(ormConfig != null)
                    {
                        ormConfig.ReadWriteConnectionString = ormConfig.ReadonlyConnectionString = dbSelector.ConnectionString.Name;
                        ormConfig.ProviderType = dbSelector.Provider.DbProviderType.AssemblyQualifiedName;
                        ormConfig.TraceSql = false;
                    }
                    // Add configuration 
                    foreach(var tsk in ftr.CreateInstallTasks())
                        ConfigurationContext.Current.ConfigurationTasks.Add(tsk);
                }

                var confirmDlg = new frmTaskList();
                if (confirmDlg.ShowDialog() == DialogResult.OK)
                    ConfigurationContext.Current.Apply();
                else
                    return;
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
