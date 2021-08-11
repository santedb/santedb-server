/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using SanteDB.Configuration;
using SanteDB.Configurator.Tasks;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SanteDB.Configurator
{
    /// <summary>
    /// Extensions to the configuration context
    /// </summary>
    public static class ConfigurationContextExtensions
    {

        /// <summary>
        /// Apply the tasks specified in the current task queue
        /// </summary>
        public static void Apply(this ConfigurationContext me)
        {

            var tracer = new Tracer("Configuration Context");

            if (me.ConfigurationTasks.Count == 0)
                return;

            // Add the save task
            if(!me.ConfigurationTasks.OfType<SaveConfigurationTask>().Any())
                me.ConfigurationTasks.Add(new SaveConfigurationTask());
            // Is the WindowsService installed?
            var rstr = new RestartServiceTask();
            if (rstr.VerifyState(me.Configuration))
                me.ConfigurationTasks.Add(new RestartServiceTask());

            var confirmDlg = new frmTaskList();
            if (confirmDlg.ShowDialog() != DialogResult.OK)
                return;

            var progress = new frmProgress();
            progress.Show();

            try
            {

                // Do work in background thread here
                bool complete = false;
                Exception errCode = null;
                var exeThd = new Thread(() =>
                {
                    using (AuthenticationContext.EnterSystemContext())
                    {
                        try
                        {
                            int i = 0, t = me.ConfigurationTasks.Count;
                            var tasks = me.ConfigurationTasks.ToArray();
                            foreach (var ct in tasks)
                            {
                                ct.ProgressChanged += (o, e) =>
                                {
                                    progress.ActionStatusText = e.State?.ToString() ?? "...";
                                    progress.ActionStatus = (int)(e.Progress * 100);
                                    progress.OverallStatus = (int)((((float)i / t) + (e.Progress * 1.0f / t)) * 100);
                                };

                                progress.OverallStatusText = $"Applying {ct.Feature.Name}";
                                if (ct.VerifyState(me.Configuration) && !ct.Execute(me.Configuration))
                                    tracer.TraceWarning("Configuration task {0} reported unsuccessful deployment", ct.Name);
                                me.ConfigurationTasks.Remove(ct);
                                progress.OverallStatus = (int)(((float)++i / t) * 100.0);
                            }
                        }
                        catch (Exception e)
                        {
                            tracer.TraceError("Error deploying configuration - {0}", e.Message);
                            Trace.TraceError($"Error on component: {e}");
                            errCode = e;
                        }
                        finally
                        {
                            complete = true;
                        }
                    }
                });

                exeThd.Start();
                while (!complete) Application.DoEvents();

                if (errCode != null) throw errCode;

                progress.OverallStatusText = "Reloading Configuration...";
                progress.OverallStatus = 100;
                progress.ActionStatusText = "Reloading Configuration...";
                progress.ActionStatus = 50;
                //me.RestartContext();
                progress.ActionStatusText = "Reloading Configuration...";
                progress.ActionStatus = 100;
            }
            catch (Exception e)
            {
                // TODO: Rollback
                tracer.TraceError("Error applying configuration: {0}", e);
                MessageBox.Show($"Error applying configuration: {e.Message}");
            }
            finally
            {
                progress.Close();
            }
        }

    }
}
