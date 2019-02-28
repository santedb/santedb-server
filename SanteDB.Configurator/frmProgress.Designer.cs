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
            this.lblAction = new System.Windows.Forms.Label();
            this.pgAction = new System.Windows.Forms.ProgressBar();
            this.lblOverall = new System.Windows.Forms.Label();
            this.pgMain = new System.Windows.Forms.ProgressBar();
            this.tmrPB = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // lblAction
            // 
            this.lblAction.AutoSize = true;
            this.lblAction.Location = new System.Drawing.Point(12, 13);
            this.lblAction.Name = "lblAction";
            this.lblAction.Size = new System.Drawing.Size(104, 13);
            this.lblAction.TabIndex = 7;
            this.lblAction.Text = "Performing Actions...";
            // 
            // pgAction
            // 
            this.pgAction.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pgAction.Location = new System.Drawing.Point(12, 34);
            this.pgAction.Name = "pgAction";
            this.pgAction.Size = new System.Drawing.Size(343, 23);
            this.pgAction.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.pgAction.TabIndex = 6;
            // 
            // lblOverall
            // 
            this.lblOverall.AutoSize = true;
            this.lblOverall.Location = new System.Drawing.Point(12, 60);
            this.lblOverall.Name = "lblOverall";
            this.lblOverall.Size = new System.Drawing.Size(93, 13);
            this.lblOverall.TabIndex = 5;
            this.lblOverall.Text = "Overall Progress...";
            // 
            // pgMain
            // 
            this.pgMain.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pgMain.Location = new System.Drawing.Point(12, 80);
            this.pgMain.Name = "pgMain";
            this.pgMain.Size = new System.Drawing.Size(343, 23);
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
            // frmProgress
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(367, 124);
            this.ControlBox = false;
            this.Controls.Add(this.lblAction);
            this.Controls.Add(this.pgAction);
            this.Controls.Add(this.lblOverall);
            this.Controls.Add(this.pgMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmProgress";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Configuring Service";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblAction;
        private System.Windows.Forms.ProgressBar pgAction;
        private System.Windows.Forms.Label lblOverall;
        private System.Windows.Forms.ProgressBar pgMain;
        private System.Windows.Forms.Timer tmrPB;
    }
}