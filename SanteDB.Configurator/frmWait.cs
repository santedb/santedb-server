using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SanteDB.Configurator
{
   [ExcludeFromCodeCoverage]
    public partial class frmWait : Form
    {
        public frmWait()
        {
            InitializeComponent();
        }

        public static frmWait ShowWait()
        {
            var frmWait = new frmWait();
            frmWait.Show();
            return frmWait;
        }
    }
}
