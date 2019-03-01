using SanteDB.Core.Configuration.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SanteDB.Configuration.Editors
{
    internal partial class frmNewDatabase : Form
    {

        // Connection string
        private ConnectionString m_connectionString;

        /// <summary>
        /// Create a new database
        /// </summary>
        public frmNewDatabase(ConnectionString connectionString, IDataConfigurationProvider provider)
        {
            InitializeComponent();
            this.ConnectionString = connectionString;
            this.Provider = provider;
        }

        /// <summary>
        /// Gets or sets the connection string
        /// </summary>
        public ConnectionString ConnectionString
        {
            get
            {
                return this.m_connectionString;
            }
            set
            {
                this.m_connectionString = value;
                this.txtOwner.Text = this.txtUserName.Text = value.GetComponent("user id");
                this.txtPassword.Text = value.GetComponent("password");
                this.txtDatabaseAddress.Text = value.GetComponent("host") ?? value.GetComponent("server");
                this.cbxDatabase.SelectedValue = value.GetComponent("initial catalog") ?? value.GetComponent("database");
            }
        }

        /// <summary>
        /// Gets or sets the provider reference
        /// </summary>
        public IDataConfigurationProvider Provider { get; }

        /// <summary>
        /// Drop down
        /// </summary>
        private void cbxDatabase_DropDown(object sender, EventArgs e)
        {
            cbxDatabase.Items.Clear();
            try
            {
                cbxDatabase.Items.AddRange(this.Provider.GetDatabases(this.ConnectionString).OfType<Object>().ToArray());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                cbxDatabase.Enabled = false;
            }
        }

        /// <summary>
        /// User has cancelled the dialog
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// User has accepted the input
        /// </summary>
        private void btnOk_Click(object sender, EventArgs e)
        {
            if (txtUserName.Text == "")
            {
                errMain.SetError(txtUserName, "Superuser must be provided");
                return;
            }

            // Is this a current database?
            if (cbxDatabase.SelectedIndex != -1)
            {
                MessageBox.Show("Cannot create an already existing database, please enter a different name", "Duplicate Name");
                return;
            }

            // Create
            try
            {

                this.m_connectionString = this.Provider.CreateDatabase(this.ConnectionString, this.cbxDatabase.Text, this.txtOwner.Text);
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Create database failed, error was : {0}", ex.Message), "Creation Error");
            }
        }

        private void txtDatabaseAddress_TextChanged(object sender, EventArgs e)
        {
            this.m_connectionString.SetComponent("host", txtDatabaseAddress.Text);
        }

        private void txtUserName_TextChanged(object sender, EventArgs e)
        {
            this.m_connectionString.SetComponent("user id", txtUserName.Text);
        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {
            this.m_connectionString.SetComponent("password", txtPassword.Text);
        }

        private void cbxDatabase_TextChanged(object sender, EventArgs e)
        {
            btnOk.Enabled = !String.IsNullOrEmpty(this.cbxDatabase.Text) &&
                !String.IsNullOrEmpty(this.txtOwner.Text);
        }
    }
}
