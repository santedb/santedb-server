using SanteDB.Core.Configuration.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SanteDB.Configurator
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
            ConfigurationContext.Current.ConfigurationTasks.CollectionChanged += (o, e) =>
            {
                btnApply.Text = $"Apply {ConfigurationContext.Current.ConfigurationTasks.Count} Changes";
            };
            this.PopulateConfiguration();
        }

        /// <summary>
        /// Populate / load the configuration
        /// </summary>
        private void PopulateConfiguration()
        {
            // Load advanced view
            lsvConfigSections.Items.Clear();
            btnRestart.Enabled = ConfigurationContext.Current.Features.OfType<WindowsServiceFeature>().FirstOrDefault()?.QueryState(ConfigurationContext.Current.Configuration) == Core.Configuration.FeatureInstallState.Installed;

            foreach(var sect in ConfigurationContext.Current.Configuration.Sections)
            {
                var lvi = lsvConfigSections.Items.Add(sect.GetType().FullName, sect.GetType().GetCustomAttribute<XmlTypeAttribute>()?.TypeName, 3);
                lvi.Tag = sect;
            }

            // Now load all features from the application domain
            foreach(var ftr in ConfigurationContext.Current.Features)
            {

            }
        }

        /// <summary>
        /// Select index has changed
        /// </summary>
        private void lsvConfigSections_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lsvConfigSections.SelectedItems.Count == 0)
                pbEditor.SelectedObject = null;
            else
                pbEditor.SelectedObject = lsvConfigSections.SelectedItems[0].Tag;
                
        }
    }
}
