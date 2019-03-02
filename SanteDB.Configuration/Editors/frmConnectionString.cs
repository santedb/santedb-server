using SanteDB.Core.Configuration.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SanteDB.Configuration.Editors
{
    public partial class frmConnectionString : Form
    {
        // Get the name
        private string m_name = $"conn{Guid.NewGuid().ToString().Substring(0, 8)}";

        public frmConnectionString()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Connection string name
        /// </summary>
        public ConnectionString ConnectionString
        {
            get
            {
                var retVal = this.ucConnection.ConnectionString;
                retVal.Name = this.m_name;
                return retVal;
            }
        }

        /// <summary>
        /// Cancel was clicked
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// Ok was clicked
        /// </summary>
        private void btnOk_Click(object sender, EventArgs e)
        {
            if (!this.ucConnection.Validate())
                MessageBox.Show("Invalid Connection Settings");
            else
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void ucConnection_ConfigurationChanged(object sender, EventArgs e)
        {
            this.btnOk.Enabled = ucConnection.IsConfigured;
        }
    }
}
