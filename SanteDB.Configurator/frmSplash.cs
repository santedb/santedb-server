/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
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
 * User: JustinFyfe
 * Date: 2019-2-28
 */
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

        /// <summary>
        /// Notify status
        /// </summary>
        /// <param name="statusText"></param>
        /// <param name="status"></param>
        public void NotifyStatus(String statusText, float status)
        {
            if (status < 0)
            {
                this.pbStatus.Style = ProgressBarStyle.Marquee;
                this.pbStatus.Value = 100;
            }
            else
            {
                this.pbStatus.Style = ProgressBarStyle.Blocks;
                this.pbStatus.Value = (int)(status * this.pbStatus.Maximum);
            }
            this.lblStatus.Text = statusText;
            Application.DoEvents();
        }
    }
}
