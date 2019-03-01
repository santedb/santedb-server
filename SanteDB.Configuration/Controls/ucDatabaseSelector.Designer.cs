namespace SanteDB.Configuration.Controls
{
    partial class ucDatabaseSelector
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label5 = new System.Windows.Forms.Label();
            this.cbxProviderType = new System.Windows.Forms.ComboBox();
            this.pgProperties = new SanteDB.Configuration.Controls.PropertyGridEx();
            this.SuspendLayout();
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(5, 6);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(98, 13);
            this.label5.TabIndex = 31;
            this.label5.Text = "Database Software";
            // 
            // cbxProviderType
            // 
            this.cbxProviderType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxProviderType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxProviderType.FormattingEnabled = true;
            this.cbxProviderType.Location = new System.Drawing.Point(109, 3);
            this.cbxProviderType.Name = "cbxProviderType";
            this.cbxProviderType.Size = new System.Drawing.Size(287, 21);
            this.cbxProviderType.TabIndex = 30;
            this.cbxProviderType.DropDown += new System.EventHandler(this.cbxProviderType_DropDown);
            this.cbxProviderType.SelectedIndexChanged += new System.EventHandler(this.cbxProviderType_SelectedIndexChanged);
            // 
            // pgProperties
            // 
            this.pgProperties.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pgProperties.CommandsVisibleIfAvailable = false;
            this.pgProperties.Location = new System.Drawing.Point(8, 30);
            this.pgProperties.Name = "pgProperties";
            this.pgProperties.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.pgProperties.Size = new System.Drawing.Size(387, 234);
            this.pgProperties.TabIndex = 32;
            this.pgProperties.ToolbarVisible = false;
            this.pgProperties.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.pgProperties_PropertyValueChanged);
            // 
            // ucDatabaseSelector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pgProperties);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.cbxProviderType);
            this.Name = "ucDatabaseSelector";
            this.Size = new System.Drawing.Size(399, 267);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cbxProviderType;
        private PropertyGridEx pgProperties;
    }
}
