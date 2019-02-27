using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
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
        public int ActionStatus { get { return pgAction.Value; } set { pgAction.Value = value; Application.DoEvents(); } }

        /// <summary>
        /// Gets or sets the status text
        /// </summary>
        public string ActionStatusText { get { return label2.Text; } set { label2.Text = $"{value} ({this.ActionStatus}%)"; Application.DoEvents(); } }


        /// <summary>
        /// Gets or sets the status text
        /// </summary>
        internal int OverallStatus { get { return pgMain.Value; } set { pgMain.Value = value; Application.DoEvents(); } }

        /// <summary>
        /// Gets or sets the status text
        /// </summary>
        internal string OverallStatusText { get { return label1.Text; } set { label1.Text = $"{value}"; Application.DoEvents(); } }

    }
}
