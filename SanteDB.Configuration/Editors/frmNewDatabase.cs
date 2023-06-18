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
using SanteDB.Core.Configuration.Data;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;

namespace SanteDB.Configuration.Editors
{
    [ExcludeFromCodeCoverage]
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
            this.Provider = provider;
            this.ConnectionString = connectionString;

            this.txtDatabaseAddress.Enabled = !String.IsNullOrEmpty(provider.Capabilities.HostSetting);
            this.txtOwner.Enabled = provider.Capabilities.SupportsOwnership;
            this.txtPassword.Enabled = !String.IsNullOrEmpty(provider.Capabilities.PasswordSetting);
            this.txtUserName.Enabled = !String.IsNullOrEmpty(provider.Capabilities.UserNameSetting);
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
                this.txtOwner.Text = this.txtUserName.Text = value.GetComponent(this.Provider.Capabilities.UserNameSetting);
                this.txtPassword.Text = value.GetComponent(this.Provider.Capabilities.PasswordSetting);
                this.txtDatabaseAddress.Text = value.GetComponent(this.Provider.Capabilities.HostSetting);
                this.cbxDatabase.SelectedValue = value.GetComponent(this.Provider.Capabilities.NameSetting);
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
            if (txtUserName.Enabled && txtUserName.Text == "")
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
            catch (System.Reflection.TargetInvocationException ex)
            {
                MessageBox.Show(String.Format("Create database failed, error was : {0}", ex.InnerException.Message), "Creation Error");

            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Create database failed, error was : {0}", ex.Message), "Creation Error");
            }
        }

        private void txtDatabaseAddress_TextChanged(object sender, EventArgs e)
        {
            this.m_connectionString.SetComponent(this.Provider.Capabilities.HostSetting, txtDatabaseAddress.Text);
        }

        private void txtUserName_TextChanged(object sender, EventArgs e)
        {
            this.m_connectionString.SetComponent(this.Provider.Capabilities.UserNameSetting, txtUserName.Text);
        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {
            this.m_connectionString.SetComponent(this.Provider.Capabilities.PasswordSetting, txtPassword.Text);
        }

        private void cbxDatabase_TextChanged(object sender, EventArgs e)
        {
            btnOk.Enabled = !String.IsNullOrEmpty(this.cbxDatabase.Text) &&
                (!this.txtOwner.Enabled ^ !String.IsNullOrEmpty(this.txtOwner.Text));
        }
    }
}
