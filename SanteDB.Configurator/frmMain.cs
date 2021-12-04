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
using SanteDB.Configuration;
using SanteDB.Configuration.Controls;
using SanteDB.Configuration.Converters;
using SanteDB.Configuration.Editors;
using SanteDB.Configuration.Features;
using SanteDB.Configurator.Tasks;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Services;
using SanteDB.Server.Core.Configuration.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SanteDB.Configurator
{
    [ExcludeFromCodeCoverage]
    public partial class frmMain : Form
    {

       
        #region Trace Writer

        private class FormTraceWriter : TraceWriter
        {

            // Form for the error writer
            private readonly frmMain m_form;

            // Synchronization context
            private SynchronizationContext m_syncContext;

            /// <summary>
            /// Trace writer initialization
            /// </summary>
            public FormTraceWriter(frmMain mainForm) : base(System.Diagnostics.Tracing.EventLevel.Warning, null, new Dictionary<String, EventLevel>() { { "SanteDB", EventLevel.Warning } })
            {
                this.m_form = mainForm;
                this.m_syncContext = SynchronizationContext.Current;
            }

            /// <summary>
            /// Write trace
            /// </summary>
            protected override void WriteTrace(EventLevel level, string source, string format, params object[] args)
            {
                this.m_syncContext.Post((o) =>
                {
                    var lvi = this.m_form.lsvLog.Items.Add(level.ToString(), level == EventLevel.Warning ? 0 : 1);
                    lvi.SubItems.Add(source);
                    lvi.SubItems.Add(String.Format(format, args));
                }, null);
            }
        }

        #endregion

        public IFeature CurrentFeature { get; private set; }

        public frmMain()
        {
            Tracer.AddWriter(new FormTraceWriter(this), EventLevel.LogAlways);
            InitializeComponent();
            this.PopulateConfiguration();
        }

        // Configuration hash
        private byte[] m_configHash;

        /// <summary>
        /// Has the configuration changed
        /// </summary>
        /// <returns></returns>
        private bool HasChanged()
        {

            byte[] newHash = null;

            try
            {
                using (var ms = new MemoryStream())
                {
                    ConfigurationContext.Current.Configuration.Save(ms);
                    newHash = MD5.Create().ComputeHash(ms.ToArray());
                    return !Enumerable.SequenceEqual(newHash, m_configHash ?? new byte[16]);
                }
            }
            finally
            {
                this.m_configHash = newHash;
            }
        }

        /// <summary>
        /// Populate / load the configuration
        /// </summary>
        private void PopulateConfiguration()
        {

            using (frmWait.ShowWait())
            {
                this.Hide();
                this.Text = $"SanteDB Confgiuration Tool ({Path.GetFileName(ConfigurationContext.Current.ConfigurationFile)})";
                using (var ms = new MemoryStream())
                {
                    Application.DoEvents();

                    ConfigurationContext.Current.Configuration.Save(ms);
                    this.m_configHash = MD5.Create().ComputeHash(ms.ToArray());
                }

                Tracer tracer = new Tracer("Configuration Tool");
                // Load the license
                try
                {
                    Application.DoEvents();

                    using (var ms = typeof(frmMain).Assembly.GetManifestResourceStream("SanteDB.Configurator.License.rtf"))
                        rtbLicense.LoadFile(ms, RichTextBoxStreamType.RichText);
                }
                catch (Exception e) // common on Linux systems in Mono
                {
                    tracer.TraceError("Could not load license file: {0}", e.Message);
                }
                var asm = Assembly.LoadFile(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "santedb.exe"));
                lblVersion.Text = $"{asm.GetName().Version} ({asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}";
                lblCopyright.Text = $"{asm.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright}";
                lblInstanceName.Text = $"{ConfigurationContext.Current.GetAppSetting("w32instance.name") ?? "DEFAULT"}";

                // Load advanced view
                lsvConfigSections.Items.Clear();
                btnRestartService.Enabled = ConfigurationContext.Current.Features.OfType<WindowsServiceFeature>().FirstOrDefault()?.QueryState(ConfigurationContext.Current.Configuration) == Core.Configuration.FeatureInstallState.Installed;

                foreach (var sect in ConfigurationContext.Current.Configuration.Sections)
                {
                    Application.DoEvents();
                    var lvi = lsvConfigSections.Items.Add(sect.GetType().FullName, sect.GetType().GetCustomAttribute<XmlTypeAttribute>()?.TypeName, 3);
                    lvi.Tag = sect;
                }

                // Now load all features from the application domain
                trvFeatures.Nodes.Clear();
                foreach (var ftr in ConfigurationContext.Current.Features)
                {
                    try
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


                        var mre = new ManualResetEvent(false);
                        Core.Configuration.FeatureInstallState state = FeatureInstallState.NotInstalled;
                        ThreadPool.QueueUserWorkItem((o) =>
                        {
                            state = ftr.QueryState(ConfigurationContext.Current.Configuration);
                            mre.Set();
                        });

                        while (!mre.WaitOne(100))
                            Application.DoEvents();

                        switch (state)
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
                            case FeatureInstallState.CantInstall:
                                node.ImageIndex = 12;
                                break;
                        }
                        node.SelectedImageIndex = node.ImageIndex;
                        node.Tag = ftr;
                    }
                    catch (Exception e)
                    {
                        tracer.TraceError("Could not load feature {0} - {1}", ftr.Name, e.Message);
                    }
                }

                this.Show();
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
                        lblNoInstall.Visible = lblDisabled.Visible = btnEnable.Visible = false;
                        break;
                    case FeatureInstallState.CantInstall:
                        lblDisabled.Visible = lblEnabled.Visible = btnDisable.Visible = btnEnable.Visible = tcSettings.Enabled = false;
                        lblNoInstall.Visible = true;
                        break;
                    default:
                        lblNoInstall.Visible = tcSettings.Enabled = btnDisable.Visible = lblEnabled.Visible = false;
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

            if(this.HasChanged())
            {
                foreach (var itm in ConfigurationContext.Current.ConfigurationTasks.Where(o => o.Feature == this.CurrentFeature).ToArray())
                    ConfigurationContext.Current.ConfigurationTasks.Remove(itm);

                if (this.CurrentFeature != null)
                {
                    foreach (var itm in this.CurrentFeature.CreateInstallTasks().ToArray())
                        ConfigurationContext.Current.ConfigurationTasks.Add(itm);
                }
            }

            foreach (var tsk in ConfigurationContext.Current.Features.Where(o =>
                     o.Flags.HasFlag(FeatureFlags.AlwaysConfigure))
                .SelectMany(o => o.CreateInstallTasks()))
                ConfigurationContext.Current.ConfigurationTasks.Add(tsk);
            ConfigurationContext.Current.ConfigurationTasks.Add(new SaveConfigurationTask());
            ConfigurationContext.Current.Apply();
            this.PopulateConfiguration();
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
            foreach (var tsk in this.CurrentFeature.CreateUninstallTasks())
                ConfigurationContext.Current.ConfigurationTasks.Add(tsk);
        }

        /// <summary>
        /// Open configuration file
        /// </summary>
        private void btnOpenConfig_Click(object sender, EventArgs e)
        {
            var dlgOpen = new OpenFileDialog()
            {
                Title = "Open Alternate Configuration",
                Filter = "Configuration File (*.config.*)|*.config.*"
            };
            if (dlgOpen.ShowDialog() == DialogResult.OK)
            {
                ConfigurationContext.Current.LoadConfiguration(dlgOpen.FileName);
                this.PopulateConfiguration();
            }
        }

        /// <summary>
        /// Feature configuration
        /// </summary>
        private void trvFeatures_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node?.Tag is IFeature feature && this.HasChanged())
            {
                foreach (var itm in ConfigurationContext.Current.ConfigurationTasks.Where(o => o.Feature == this.CurrentFeature).ToArray())
                    ConfigurationContext.Current.ConfigurationTasks.Remove(itm);
                if (this.CurrentFeature != null)
                {
                    foreach (var itm in this.CurrentFeature.CreateInstallTasks().ToArray())
                        ConfigurationContext.Current.ConfigurationTasks.Add(itm);
                }
            }
        }

        /// <summary>
        /// Value has changed
        /// </summary>
        private void pgConfiguration_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            // Force changed
            this.m_configHash = new byte[0];
        }
    }
}
