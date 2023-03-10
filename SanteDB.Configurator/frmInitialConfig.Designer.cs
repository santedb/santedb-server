/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
 */
using SanteDB.Configuration.Controls;

namespace SanteDB.Configurator
{
    partial class frmInitialConfig
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
            SanteDB.Core.Configuration.Data.ConnectionString connectionString1 = new SanteDB.Core.Configuration.Data.ConnectionString();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmInitialConfig));
            this.pnlLogo = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.lblText = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.rdoEasy = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.dbSelector = new SanteDB.Configuration.Controls.ucDatabaseSelector();
            this.pnlSettings = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.cbxTemplate = new System.Windows.Forms.ComboBox();
            this.txtInstance = new System.Windows.Forms.TextBox();
            this.btnContinue = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.rdoAdvanced = new System.Windows.Forms.RadioButton();
            this.label6 = new System.Windows.Forms.Label();
            this.rdoOpen = new System.Windows.Forms.RadioButton();
            this.pnlLogo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.pnlSettings.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlLogo
            // 
            this.pnlLogo.BackColor = System.Drawing.Color.White;
            this.pnlLogo.Controls.Add(this.label1);
            this.pnlLogo.Controls.Add(this.lblText);
            this.pnlLogo.Controls.Add(this.pictureBox1);
            this.pnlLogo.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlLogo.Location = new System.Drawing.Point(0, 0);
            this.pnlLogo.Name = "pnlLogo";
            this.pnlLogo.Size = new System.Drawing.Size(512, 70);
            this.pnlLogo.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(74, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(426, 32);
            this.label1.TabIndex = 2;
            this.label1.Text = "Welcome to SanteDB! Before you can run this service you have to configure the ser" +
    "vice host";
            // 
            // lblText
            // 
            this.lblText.AutoSize = true;
            this.lblText.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblText.Location = new System.Drawing.Point(73, 18);
            this.lblText.Name = "lblText";
            this.lblText.Size = new System.Drawing.Size(166, 20);
            this.lblText.TabIndex = 1;
            this.lblText.Text = "Initial Configuration";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Left;
            this.pictureBox1.Image = global::SanteDB.Configurator.Properties.Resources.icon;
            this.pictureBox1.InitialImage = global::SanteDB.Configurator.Properties.Resources.icon;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(67, 70);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // rdoEasy
            // 
            this.rdoEasy.AutoSize = true;
            this.rdoEasy.Checked = true;
            this.rdoEasy.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rdoEasy.Location = new System.Drawing.Point(13, 76);
            this.rdoEasy.Name = "rdoEasy";
            this.rdoEasy.Size = new System.Drawing.Size(226, 17);
            this.rdoEasy.TabIndex = 2;
            this.rdoEasy.TabStop = true;
            this.rdoEasy.Text = "Easy Configuration (Recommended)";
            this.rdoEasy.UseVisualStyleBackColor = true;
            this.rdoEasy.CheckedChanged += new System.EventHandler(this.rdoEasy_CheckedChanged);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(34, 96);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(478, 31);
            this.label2.TabIndex = 4;
            this.label2.Text = "Select this option if you wish to use the default configuration parameters for th" +
    "e SanteDB service and sub-services.";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.dbSelector);
            this.groupBox1.Controls.Add(this.pnlSettings);
            this.groupBox1.Location = new System.Drawing.Point(10, 130);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(490, 265);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Database Parameters";
            // 
            // dbSelector
            // 
            connectionString1.Name = null;
            connectionString1.Provider = "";
            connectionString1.Value = "";
            this.dbSelector.ConnectionString = connectionString1;
            this.dbSelector.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dbSelector.Location = new System.Drawing.Point(3, 16);
            this.dbSelector.Name = "dbSelector";
            this.dbSelector.Size = new System.Drawing.Size(484, 176);
            this.dbSelector.TabIndex = 1;
            this.dbSelector.ConfigurationChanged += new System.EventHandler(this.dbSelector_Configured);
            // 
            // pnlSettings
            // 
            this.pnlSettings.Controls.Add(this.label4);
            this.pnlSettings.Controls.Add(this.label5);
            this.pnlSettings.Controls.Add(this.cbxTemplate);
            this.pnlSettings.Controls.Add(this.txtInstance);
            this.pnlSettings.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlSettings.Location = new System.Drawing.Point(3, 192);
            this.pnlSettings.Name = "pnlSettings";
            this.pnlSettings.Size = new System.Drawing.Size(484, 70);
            this.pnlSettings.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 10);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(79, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Instance Name";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 38);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(51, 13);
            this.label5.TabIndex = 4;
            this.label5.Text = "Template";
            // 
            // cbxTemplate
            // 
            this.cbxTemplate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxTemplate.FormattingEnabled = true;
            this.cbxTemplate.Location = new System.Drawing.Point(88, 35);
            this.cbxTemplate.Name = "cbxTemplate";
            this.cbxTemplate.Size = new System.Drawing.Size(364, 21);
            this.cbxTemplate.TabIndex = 5;
            // 
            // txtInstance
            // 
            this.txtInstance.Location = new System.Drawing.Point(88, 9);
            this.txtInstance.Name = "txtInstance";
            this.txtInstance.Size = new System.Drawing.Size(364, 20);
            this.txtInstance.TabIndex = 3;
            this.txtInstance.Text = "SanteDB";
            // 
            // btnContinue
            // 
            this.btnContinue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnContinue.Enabled = false;
            this.btnContinue.Location = new System.Drawing.Point(344, 506);
            this.btnContinue.Name = "btnContinue";
            this.btnContinue.Size = new System.Drawing.Size(75, 23);
            this.btnContinue.TabIndex = 9;
            this.btnContinue.Text = "Continue";
            this.btnContinue.UseVisualStyleBackColor = true;
            this.btnContinue.Click += new System.EventHandler(this.btnContinue_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(425, 506);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 10;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(34, 419);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(478, 31);
            this.label3.TabIndex = 12;
            this.label3.Text = "Select this option if you wish configure the individual components that will run " +
    "within this SanteDB instance";
            // 
            // rdoAdvanced
            // 
            this.rdoAdvanced.AutoSize = true;
            this.rdoAdvanced.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rdoAdvanced.Location = new System.Drawing.Point(13, 399);
            this.rdoAdvanced.Name = "rdoAdvanced";
            this.rdoAdvanced.Size = new System.Drawing.Size(161, 17);
            this.rdoAdvanced.TabIndex = 11;
            this.rdoAdvanced.Text = "Advanced Configuration";
            this.rdoAdvanced.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(34, 473);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(478, 31);
            this.label6.TabIndex = 14;
            this.label6.Text = "Select this option if you wish to open another configuration file other than the " +
    "santedb.config.xml file";
            // 
            // rdoOpen
            // 
            this.rdoOpen.AutoSize = true;
            this.rdoOpen.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rdoOpen.Location = new System.Drawing.Point(13, 453);
            this.rdoOpen.Name = "rdoOpen";
            this.rdoOpen.Size = new System.Drawing.Size(134, 17);
            this.rdoOpen.TabIndex = 13;
            this.rdoOpen.Text = "Open Configuration";
            this.rdoOpen.UseVisualStyleBackColor = true;
            // 
            // frmInitialConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(512, 541);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.rdoOpen);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.rdoAdvanced);
            this.Controls.Add(this.btnContinue);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.rdoEasy);
            this.Controls.Add(this.pnlLogo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmInitialConfig";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Welcome to SanteDB";
            this.pnlLogo.ResumeLayout(false);
            this.pnlLogo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.pnlSettings.ResumeLayout(false);
            this.pnlSettings.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlLogo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblText;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.RadioButton rdoEasy;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox1;
        private ucDatabaseSelector dbSelector;
        private System.Windows.Forms.Button btnContinue;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RadioButton rdoAdvanced;
        private System.Windows.Forms.TextBox txtInstance;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cbxTemplate;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Panel pnlSettings;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.RadioButton rdoOpen;
    }
}