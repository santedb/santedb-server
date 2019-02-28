using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SanteDB.Configurator
{
    public partial class frmProgress : Form
    {

        public frmProgress()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the status text
        /// </summary>
        public int ActionStatus { get; set; }

        /// <summary>
        /// Gets or sets the status text
        /// </summary>
        public string ActionStatusText { get; set; }

        /// <summary>
        /// Gets or sets the status text
        /// </summary>
        internal int OverallStatus { get; set; }

        /// <summary>
        /// Gets or sets the status text
        /// </summary>
        internal string OverallStatusText { get; set; }

        /// <summary>
        /// Timer has ticked
        /// </summary>
        private void tmrPB_Tick(object sender, EventArgs e)
        {

            this.lblAction.Text = $"{this.ActionStatusText} ({this.ActionStatus}%)";
            this.lblOverall.Text = $"{this.OverallStatusText} ({this.OverallStatus}%)";
            this.pgAction.Value = this.ActionStatus;
            this.pgMain.Value = this.OverallStatus;
            
        }
    }
}
