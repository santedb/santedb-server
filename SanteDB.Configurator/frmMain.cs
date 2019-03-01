using SanteDB.Configuration;
using SanteDB.Configuration.Features;
using SanteDB.Configurator.Tasks;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Tasks;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
            this.PopulateConfiguration();
        }

        /// <summary>
        /// Populate / load the configuration
        /// </summary>
        private void PopulateConfiguration()
        {

            // Load the license
            using(var ms = typeof(frmMain).Assembly.GetManifestResourceStream("SanteDB.Configurator.License.rtf"))
                rtbLicense.LoadFile(ms, RichTextBoxStreamType.RichText);

            var asm = Assembly.LoadFile(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "santedb.exe"));
            lblVersion.Text = $"{asm.GetName().Version} ({asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}";
            lblCopyright.Text = $"{asm.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright}";
            lblInstanceName.Text = $"{ApplicationServiceContext.Current.GetService<IConfigurationManager>()?.GetAppSetting("w32instance.name") ?? "DEFAULT"}";

            // Load advanced view
            lsvConfigSections.Items.Clear();
            btnRestart.Enabled = ConfigurationContext.Current.Features.OfType<WindowsServiceFeature>().FirstOrDefault()?.QueryState(ConfigurationContext.Current.Configuration) == Core.Configuration.FeatureInstallState.Installed;

            foreach(var sect in ConfigurationContext.Current.Configuration.Sections)
            {
                var lvi = lsvConfigSections.Items.Add(sect.GetType().FullName, sect.GetType().GetCustomAttribute<XmlTypeAttribute>()?.TypeName, 3);
                lvi.Tag = sect;
            }

            // Now load all features from the application domain
            trvFeatures.Nodes.Clear();
            foreach(var ftr in ConfigurationContext.Current.Features)
            {
                // Add the features
                var trvParent = trvFeatures.Nodes.Find(ftr.Group, false).FirstOrDefault();
                if(trvParent == null)
                {
                    trvParent = trvFeatures.Nodes.Add(ftr.Group, ftr.Group, 6);
                    trvParent.SelectedImageIndex = 6;
                }

                // Create node for the object
                var node = trvParent.Nodes.Add($"{ftr.Group}\\{ftr.Name}", ftr.Name, 0);
                switch(ftr.QueryState(ConfigurationContext.Current.Configuration))
                {
                    case Core.Configuration.FeatureInstallState.NotInstalled:
                        node.ImageIndex = 8;
                        break;
                    case Core.Configuration.FeatureInstallState.Installed:
                        node.ImageIndex = 9;
                        break;
                    case Core.Configuration.FeatureInstallState.PartiallyInstalled:
                        node.ImageIndex = 10;
                        break;
                }
                node.SelectedImageIndex = node.ImageIndex;
                node.Tag = ftr;
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

        /// <summary>
        /// Select the specified feature
        /// </summary>
        private void trvFeatures_AfterSelect(object sender, TreeViewEventArgs e)
        {
            this.SelectFeature(e.Node.Tag as IFeature);
        }

        /// <summary>
        /// Select the feature
        /// </summary>
        private void SelectFeature(IFeature feature)
        {
            if (feature == null)
            {
                pnlInfo.Visible = true;
            }
            else
            {
                pnlInfo.Visible = false;
                lblSelectedOption.Text = $"{feature.Name} Configuration";
                if (feature is IEnhancedConfigurationFeature)
                {
                    if (tcSettings.TabPages.ContainsKey("custom"))
                        tcSettings.TabPages.RemoveByKey("custom");
                    tcSettings.TabPages.Insert(0, "custom", "Properties", 7);
                    tcSettings.TabPages[0].Controls.Add((feature as IEnhancedConfigurationFeature).ConfigurationPanel);
                }

                if (feature.ConfigurationType != null)
                {
                    if (feature.Configuration == null)
                        feature.Configuration = ConfigurationContext.Current.Configuration.GetSection(feature.ConfigurationType) ?? Activator.CreateInstance(feature.ConfigurationType);

                    // Now set the task 
                    pgConfiguration.SelectedObject = feature.Configuration;
                    pgConfiguration.Visible = true;
                }
                else
                    pgConfiguration.Visible = false;

                // Now detect the necessary bars
                switch (feature.QueryState(ConfigurationContext.Current.Configuration))
                {
                    case FeatureInstallState.Installed:
                        tcSettings.Enabled = btnDisable.Visible = lblEnabled.Visible = !feature.Flags.HasFlag(FeatureFlags.NoRemove);
                        lblDisabled.Visible = btnEnable.Visible = false;
                        break;
                    default:
                        tcSettings.Enabled = btnDisable.Visible = lblEnabled.Visible = false;
                        lblDisabled.Visible = btnEnable.Visible = true;
                        break;
                }

            }
        }

        /// <summary>
        /// Launch Help
        /// </summary>
        private void lblSupport_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://help.santesuite.org");
        }

        /// <summary>
        /// Apply the changes
        /// </summary>
        private void btnApply_Click(object sender, EventArgs e)
        {

            // If there is no tasks then we must save
            if (ConfigurationContext.Current.ConfigurationTasks.Count == 0)
                ConfigurationContext.Current.ConfigurationTasks.Add(new SaveConfigurationTask());
            ConfigurationContext.Current.Apply();
        }
    }
}
