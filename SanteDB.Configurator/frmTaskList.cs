using SanteDB.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.OrmLite.Migration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SanteDB.Configurator
{
    public partial class frmTaskList : Form
    {
        public frmTaskList()
        {
            InitializeComponent();
        }

        /// <summary>
        /// When the form is shown
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            foreach(var itm in ConfigurationContext.Current.ConfigurationTasks)
            {
                int imgidx = itm is SqlMigrationTask ? 0 : 1;
                var lvi = lsvActions.Items.Add(Guid.NewGuid().ToString(), itm.Name, imgidx);
                lvi.SubItems.Add(itm.Description);
                lvi.Checked = true;
                lvi.Tag = itm;
            }

            base.OnShown(e);
        }

        private void lsvActions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lsvActions.SelectedItems.Count > 0)
            {
                var task = lsvActions.SelectedItems[0].Tag as IConfigurationTask;
                var dtask = task as IDescribedConfigurationTask;
                var html = "<html><style type='text/css'>body { margin: 0px; padding: 2px; font-family: sans-serif } th, td { vertical-align: top; border-bottom: solid 1px #999 } </style>" + 
                    $"<body><table width='100%'><tr><th>Task:</th><td>{task.Name}</td></tr>" +
                    $"<tr><th>Feature:</th><td>{task.Feature.Name}</td></tr>" +
                    $"<tr><th>Description:</th><td>{task.Description}</td></tr>";
                if (dtask != null)
                {
                    html += $"<tr><th>Remarks:</th><td>{dtask.AdditionalInformation}</td></tr>" +
                        $"<tr><td colspan='2'><a href=\"{dtask.HelpUri}\">More Information</td></tr>";
                }
                html += "</table></body></html>";
                wbHelp.DocumentText = html;
            }
            else
                wbHelp.DocumentText = "";
        }

        private void wbHelp_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (e.Url.Scheme == "http" || e.Url.Scheme == "https")
            {
                Process.Start(e.Url.ToString());
                e.Cancel = true;
            }
        }

        private void lsvActions_SizeChanged(object sender, EventArgs e)
        {
            lsvActions.Columns[0].Width = -2;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void lsvActions_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (!e.Item.Checked)
                ConfigurationContext.Current.ConfigurationTasks.Remove(e.Item.Tag as IConfigurationTask);
            else if(e.Item.Tag != null && ConfigurationContext.Current.ConfigurationTasks.IndexOf(e.Item.Tag as IConfigurationTask) == -1)
            {
                ConfigurationContext.Current.ConfigurationTasks.Insert(e.Item.Index, e.Item.Tag as IConfigurationTask);
            }
        }
    }
}
