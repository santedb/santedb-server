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
using SanteDB.Configuration.Controls;
using SanteDB.Configuration.Converters;
using SanteDB.Configuration.Editors;
using SanteDB.Configuration.Features;
using SanteDB.Configurator.Tasks;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using SanteDB.Core.Services;
using SanteDB.Server.Core.Configuration.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
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
        public IFeature CurrentFeature { get; private set; }

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
            using (var ms = typeof(frmMain).Assembly.GetManifestResourceStream("SanteDB.Configurator.License.rtf"))
                rtbLicense.LoadFile(ms, RichTextBoxStreamType.RichText);

            var asm = Assembly.LoadFile(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "santedb.exe"));
            lblVersion.Text = $"{asm.GetName().Version} ({asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}";
            lblCopyright.Text = $"{asm.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright}";
            lblInstanceName.Text = $"{ApplicationServiceContext.Current.GetService<IConfigurationManager>()?.GetAppSetting("w32instance.name") ?? "DEFAULT"}";

            // Load advanced view
            lsvConfigSections.Items.Clear();
            btnRestart.Enabled = ConfigurationContext.Current.Features.OfType<WindowsServiceFeature>().FirstOrDefault()?.QueryState(ConfigurationContext.Current.Configuration) == Core.Configuration.FeatureInstallState.Installed;

            foreach (var sect in ConfigurationContext.Current.Configuration.Sections)
            {
                var lvi = lsvConfigSections.Items.Add(sect.GetType().FullName, sect.GetType().GetCustomAttribute<XmlTypeAttribute>()?.TypeName, 3);
                lvi.Tag = sect;
            }

            // Now load all features from the application domain
            trvFeatures.Nodes.Clear();
            foreach (var ftr in ConfigurationContext.Current.Features)
            {
                if (ftr.ConfigurationType == null) continue;
                // Add the features
                var trvParent = trvFeatures.Nodes.Find(ftr.Group, false).FirstOrDefault();
                if (trvParent == null)
                {
                    trvParent = trvFeatures.Nodes.Add(ftr.Group, ftr.Group, 6);
                    trvParent.SelectedImageIndex = 6;
                }

                // Create node for the object
                var node = trvParent.Nodes.Add($"{ftr.Group}\\{ftr.Name}", ftr.Name, 0);
                switch (ftr.QueryState(ConfigurationContext.Current.Configuration))
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

                var state = feature.QueryState(ConfigurationContext.Current.Configuration);

                // Confiugration 
                if (feature.ConfigurationType == typeof(GenericFeatureConfiguration))
                {
                    var descriptor = new DynamicPropertyClass(feature.Configuration as GenericFeatureConfiguration);

                    pgConfiguration.SelectedObject = descriptor;
                }
                else if (feature.ConfigurationType != null)
                {
                    if (feature.Configuration == null)
                        feature.Configuration = ConfigurationContext.Current.Configuration.GetSection(feature.ConfigurationType) ?? Activator.CreateInstance(feature.ConfigurationType);

                    // Now set the task 
                    pgConfiguration.SelectedObject = feature.Configuration;
                    pgConfiguration.Visible = true;
                }
                else
                    pgConfiguration.Visible = false;

                lblDescription.Text = $"     {feature.Description}";
                this.CurrentFeature = feature;
                // Now detect the necessary bars
                switch (state)
                {
                    case FeatureInstallState.Installed:
                        tcSettings.Enabled = true;
                        btnDisable.Visible = lblEnabled.Visible = !feature.Flags.HasFlag(FeatureFlags.NoRemove);
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
            foreach (var tsk in ConfigurationContext.Current.Features.Where(o =>
                     o.Flags.HasFlag(FeatureFlags.AlwaysConfigure))
                .SelectMany(o => o.CreateInstallTasks()))
                ConfigurationContext.Current.ConfigurationTasks.Add(tsk);
            ConfigurationContext.Current.ConfigurationTasks.Add(new SaveConfigurationTask());
            ConfigurationContext.Current.Apply();
        }

        /// <summary>
        /// Enable the feature
        /// </summary>
        private void btnEnable_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Set the view
            tcSettings.Enabled = btnDisable.Visible = lblEnabled.Visible = true;
            lblDisabled.Visible = btnEnable.Visible = false;

            // Remove any uninstall tasks related to this feature
            foreach (var itm in ConfigurationContext.Current.ConfigurationTasks.Where(o => o.Feature == this.CurrentFeature).ToArray())
                ConfigurationContext.Current.ConfigurationTasks.Remove(itm);

            // Create install tasks
            if (this.CurrentFeature.QueryState(ConfigurationContext.Current.Configuration) != FeatureInstallState.Installed)
                foreach (var tsk in this.CurrentFeature.CreateInstallTasks())
                    ConfigurationContext.Current.ConfigurationTasks.Add(tsk);

        }

        /// <summary>
        /// Disable the feature
        /// </summary>
        private void btnDisable_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            tcSettings.Enabled = btnDisable.Visible = lblEnabled.Visible = false;
            lblDisabled.Visible = btnEnable.Visible = true;
            var feature = (sender as Control).Tag as IFeature;
            // Remove any tasks related to this feature
            foreach (var itm in ConfigurationContext.Current.ConfigurationTasks.Where(o => o.Feature == this.CurrentFeature).ToArray())
                ConfigurationContext.Current.ConfigurationTasks.Remove(itm);

            // Create removal tasks
            if (this.CurrentFeature.QueryState(ConfigurationContext.Current.Configuration) == FeatureInstallState.Installed)
                foreach (var tsk in this.CurrentFeature.CreateUninstallTasks())
                    ConfigurationContext.Current.ConfigurationTasks.Add(tsk);
        }
    }
}
