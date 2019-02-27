using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SanteDB.Configurator
{
    public partial class frmSplash : Form
    {
        public frmSplash()
        {
            InitializeComponent();
            lblCopyright.Text = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
            lblVersion.Text = $"v.{Assembly.GetEntryAssembly().GetName().Version} ({Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion})";
        }

        public void NotifyStatus(String statusText, float status)
        {
            this.pbStatus.Value = (int)(status * this.pbStatus.Maximum);
            this.lblStatus.Text = statusText;
            Application.DoEvents();
        }
    }
}
