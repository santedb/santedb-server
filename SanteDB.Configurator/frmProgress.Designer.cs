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
namespace SanteDB.Configurator
{
    partial class frmProgress
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmProgress));
            this.lblOverall = new System.Windows.Forms.Label();
            this.pgMain = new System.Windows.Forms.ProgressBar();
            this.tmrPB = new System.Windows.Forms.Timer(this.components);
            this.lsvStatus = new System.Windows.Forms.ListView();
            this.colTask = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colProgress = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.imlImage = new System.Windows.Forms.ImageList(this.components);
            this.SuspendLayout();
            // 
            // lblOverall
            // 
            this.lblOverall.AutoSize = true;
            this.lblOverall.Location = new System.Drawing.Point(12, 252);
            this.lblOverall.Name = "lblOverall";
            this.lblOverall.Size = new System.Drawing.Size(93, 13);
            this.lblOverall.TabIndex = 5;
            this.lblOverall.Text = "Overall Progress...";
            // 
            // pgMain
            // 
            this.pgMain.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pgMain.Location = new System.Drawing.Point(12, 273);
            this.pgMain.Name = "pgMain";
            this.pgMain.Size = new System.Drawing.Size(453, 23);
            this.pgMain.Step = 1;
            this.pgMain.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.pgMain.TabIndex = 4;
            // 
            // tmrPB
            // 
            this.tmrPB.Enabled = true;
            this.tmrPB.Interval = 350;
            this.tmrPB.Tick += new System.EventHandler(this.tmrPB_Tick);
            // 
            // lsvStatus
            // 
            this.lsvStatus.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colTask,
            this.colProgress});
            this.lsvStatus.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lsvStatus.Location = new System.Drawing.Point(12, 12);
            this.lsvStatus.MultiSelect = false;
            this.lsvStatus.Name = "lsvStatus";
            this.lsvStatus.OwnerDraw = true;
            this.lsvStatus.Size = new System.Drawing.Size(453, 237);
            this.lsvStatus.SmallImageList = this.imlImage;
            this.lsvStatus.TabIndex = 8;
            this.lsvStatus.UseCompatibleStateImageBehavior = false;
            this.lsvStatus.View = System.Windows.Forms.View.Details;
            this.lsvStatus.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.lsvStatus_DrawColumnHeader);
            this.lsvStatus.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.lsvStatus_DrawSubItem);
            // 
            // colTask
            // 
            this.colTask.Text = "Task";
            this.colTask.Width = 250;
            // 
            // colProgress
            // 
            this.colProgress.Text = "Progress";
            this.colProgress.Width = 149;
            // 
            // imlImage
            // 
            this.imlImage.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imlImage.ImageStream")));
            this.imlImage.TransparentColor = System.Drawing.Color.Transparent;
            this.imlImage.Images.SetKeyName(0, "Loading_Blue_16x.png");
            this.imlImage.Images.SetKeyName(1, "Loading_Blue_16x.png");
            this.imlImage.Images.SetKeyName(2, "Loading_Blue_16x.png");
            this.imlImage.Images.SetKeyName(3, "Loading_Blue_16x.png");
            this.imlImage.Images.SetKeyName(4, "Checkmark_blue_16x.png");
            // 
            // frmProgress
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(477, 308);
            this.ControlBox = false;
            this.Controls.Add(this.lblOverall);
            this.Controls.Add(this.pgMain);
            this.Controls.Add(this.lsvStatus);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmProgress";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Configuring Service";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label lblOverall;
        private System.Windows.Forms.ProgressBar pgMain;
        private System.Windows.Forms.Timer tmrPB;
        private System.Windows.Forms.ListView lsvStatus;
        private System.Windows.Forms.ColumnHeader colTask;
        private System.Windows.Forms.ColumnHeader colProgress;
        private System.Windows.Forms.ImageList imlImage;
    }
}