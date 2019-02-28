namespace SanteDB.Configurator
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.tbMain = new System.Windows.Forms.TabControl();
            this.tpSettings = new System.Windows.Forms.TabPage();
            this.spMainControl = new System.Windows.Forms.SplitContainer();
            this.trvFeatures = new System.Windows.Forms.TreeView();
            this.imlMain = new System.Windows.Forms.ImageList(this.components);
            this.tpAdvanced = new System.Windows.Forms.TabPage();
            this.spEditor = new System.Windows.Forms.SplitContainer();
            this.lsvConfigSections = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.pbEditor = new SanteDB.Configurator.Controls.PropertyGridEx();
            this.label2 = new System.Windows.Forms.Label();
            this.tspMain = new System.Windows.Forms.ToolStrip();
            this.btnApply = new System.Windows.Forms.ToolStripButton();
            this.btnRestart = new System.Windows.Forms.ToolStripButton();
            this.tbMain.SuspendLayout();
            this.tpSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.spMainControl)).BeginInit();
            this.spMainControl.Panel1.SuspendLayout();
            this.spMainControl.SuspendLayout();
            this.tpAdvanced.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.spEditor)).BeginInit();
            this.spEditor.Panel1.SuspendLayout();
            this.spEditor.Panel2.SuspendLayout();
            this.spEditor.SuspendLayout();
            this.tspMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // tbMain
            // 
            this.tbMain.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            this.tbMain.Controls.Add(this.tpSettings);
            this.tbMain.Controls.Add(this.tpAdvanced);
            this.tbMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbMain.ImageList = this.imlMain;
            this.tbMain.Location = new System.Drawing.Point(0, 38);
            this.tbMain.Multiline = true;
            this.tbMain.Name = "tbMain";
            this.tbMain.SelectedIndex = 0;
            this.tbMain.Size = new System.Drawing.Size(744, 428);
            this.tbMain.TabIndex = 0;
            // 
            // tpSettings
            // 
            this.tpSettings.Controls.Add(this.spMainControl);
            this.tpSettings.ImageIndex = 0;
            this.tpSettings.Location = new System.Drawing.Point(4, 4);
            this.tpSettings.Name = "tpSettings";
            this.tpSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tpSettings.Size = new System.Drawing.Size(736, 401);
            this.tpSettings.TabIndex = 0;
            this.tpSettings.Text = "Settings";
            this.tpSettings.UseVisualStyleBackColor = true;
            // 
            // spMainControl
            // 
            this.spMainControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.spMainControl.Location = new System.Drawing.Point(3, 3);
            this.spMainControl.Name = "spMainControl";
            // 
            // spMainControl.Panel1
            // 
            this.spMainControl.Panel1.Controls.Add(this.trvFeatures);
            this.spMainControl.Size = new System.Drawing.Size(730, 395);
            this.spMainControl.SplitterDistance = 191;
            this.spMainControl.TabIndex = 0;
            // 
            // trvFeatures
            // 
            this.trvFeatures.Dock = System.Windows.Forms.DockStyle.Fill;
            this.trvFeatures.FullRowSelect = true;
            this.trvFeatures.HideSelection = false;
            this.trvFeatures.ImageIndex = 0;
            this.trvFeatures.ImageList = this.imlMain;
            this.trvFeatures.Location = new System.Drawing.Point(0, 0);
            this.trvFeatures.Name = "trvFeatures";
            this.trvFeatures.SelectedImageIndex = 0;
            this.trvFeatures.Size = new System.Drawing.Size(191, 395);
            this.trvFeatures.TabIndex = 0;
            // 
            // imlMain
            // 
            this.imlMain.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imlMain.ImageStream")));
            this.imlMain.TransparentColor = System.Drawing.Color.Magenta;
            this.imlMain.Images.SetKeyName(0, "ServerSettings_16x_24.bmp");
            this.imlMain.Images.SetKeyName(1, "SettingsGroup_16x_24.bmp");
            this.imlMain.Images.SetKeyName(2, "SettingsPanel_16x_24.bmp");
            this.imlMain.Images.SetKeyName(3, "XMLIntellisenseElement_16x_24.bmp");
            this.imlMain.Images.SetKeyName(4, "StatusWarning_cyan_12x_16x_24.bmp");
            this.imlMain.Images.SetKeyName(5, "StatusInfoTip_16x_24.bmp");
            // 
            // tpAdvanced
            // 
            this.tpAdvanced.Controls.Add(this.spEditor);
            this.tpAdvanced.Controls.Add(this.label2);
            this.tpAdvanced.ImageIndex = 1;
            this.tpAdvanced.Location = new System.Drawing.Point(4, 4);
            this.tpAdvanced.Name = "tpAdvanced";
            this.tpAdvanced.Padding = new System.Windows.Forms.Padding(3);
            this.tpAdvanced.Size = new System.Drawing.Size(736, 401);
            this.tpAdvanced.TabIndex = 1;
            this.tpAdvanced.Text = "Setting Editor";
            this.tpAdvanced.UseVisualStyleBackColor = true;
            // 
            // spEditor
            // 
            this.spEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.spEditor.Location = new System.Drawing.Point(3, 29);
            this.spEditor.Name = "spEditor";
            // 
            // spEditor.Panel1
            // 
            this.spEditor.Panel1.Controls.Add(this.lsvConfigSections);
            // 
            // spEditor.Panel2
            // 
            this.spEditor.Panel2.Controls.Add(this.pbEditor);
            this.spEditor.Size = new System.Drawing.Size(730, 369);
            this.spEditor.SplitterDistance = 243;
            this.spEditor.TabIndex = 0;
            // 
            // lsvConfigSections
            // 
            this.lsvConfigSections.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.lsvConfigSections.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lsvConfigSections.FullRowSelect = true;
            this.lsvConfigSections.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lsvConfigSections.HideSelection = false;
            this.lsvConfigSections.Location = new System.Drawing.Point(0, 0);
            this.lsvConfigSections.MultiSelect = false;
            this.lsvConfigSections.Name = "lsvConfigSections";
            this.lsvConfigSections.ShowGroups = false;
            this.lsvConfigSections.Size = new System.Drawing.Size(243, 369);
            this.lsvConfigSections.SmallImageList = this.imlMain;
            this.lsvConfigSections.TabIndex = 0;
            this.lsvConfigSections.UseCompatibleStateImageBehavior = false;
            this.lsvConfigSections.View = System.Windows.Forms.View.Details;
            this.lsvConfigSections.SelectedIndexChanged += new System.EventHandler(this.lsvConfigSections_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Section";
            this.columnHeader1.Width = 239;
            // 
            // pbEditor
            // 
            this.pbEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbEditor.Location = new System.Drawing.Point(0, 0);
            this.pbEditor.Name = "pbEditor";
            this.pbEditor.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.pbEditor.Size = new System.Drawing.Size(483, 369);
            this.pbEditor.TabIndex = 0;
            this.pbEditor.ToolbarVisible = false;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.SystemColors.Info;
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.InfoText;
            this.label2.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.label2.ImageIndex = 5;
            this.label2.ImageList = this.imlMain;
            this.label2.Location = new System.Drawing.Point(3, 3);
            this.label2.Name = "label2";
            this.label2.Padding = new System.Windows.Forms.Padding(3);
            this.label2.Size = new System.Drawing.Size(730, 26);
            this.label2.TabIndex = 1;
            this.label2.Text = "      Editing the values in this panel can damage your installation of SanteDB";
            // 
            // tspMain
            // 
            this.tspMain.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tspMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnApply,
            this.btnRestart});
            this.tspMain.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Flow;
            this.tspMain.Location = new System.Drawing.Point(0, 0);
            this.tspMain.Name = "tspMain";
            this.tspMain.Size = new System.Drawing.Size(744, 38);
            this.tspMain.TabIndex = 1;
            this.tspMain.Text = "toolStrip1";
            // 
            // btnApply
            // 
            this.btnApply.Image = ((System.Drawing.Image)(resources.GetObject("btnApply.Image")));
            this.btnApply.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(91, 35);
            this.btnApply.Text = "Apply Changes";
            this.btnApply.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            // 
            // btnRestart
            // 
            this.btnRestart.Image = ((System.Drawing.Image)(resources.GetObject("btnRestart.Image")));
            this.btnRestart.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnRestart.Name = "btnRestart";
            this.btnRestart.Size = new System.Drawing.Size(87, 35);
            this.btnRestart.Text = "Restart Service";
            this.btnRestart.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(744, 466);
            this.Controls.Add(this.tbMain);
            this.Controls.Add(this.tspMain);
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SanteDB Configuration Editor";
            this.tbMain.ResumeLayout(false);
            this.tpSettings.ResumeLayout(false);
            this.spMainControl.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.spMainControl)).EndInit();
            this.spMainControl.ResumeLayout(false);
            this.tpAdvanced.ResumeLayout(false);
            this.spEditor.Panel1.ResumeLayout(false);
            this.spEditor.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.spEditor)).EndInit();
            this.spEditor.ResumeLayout(false);
            this.tspMain.ResumeLayout(false);
            this.tspMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tbMain;
        private System.Windows.Forms.TabPage tpSettings;
        private System.Windows.Forms.TabPage tpAdvanced;
        private System.Windows.Forms.ImageList imlMain;
        private System.Windows.Forms.ToolStrip tspMain;
        private System.Windows.Forms.ToolStripButton btnApply;
        private System.Windows.Forms.ToolStripButton btnRestart;
        private System.Windows.Forms.SplitContainer spEditor;
        private System.Windows.Forms.ListView lsvConfigSections;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private Controls.PropertyGridEx pbEditor;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.SplitContainer spMainControl;
        private System.Windows.Forms.TreeView trvFeatures;
    }
}

