/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Jobs;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Notifications;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Server.Jobs
{
    /// <summary>
    /// A job which prunes old users from the system 
    /// </summary>
    public class InactiveUserPruneJob : IReportProgressJob
    {

        /// <summary>
        /// Tracer for inactivity
        /// </summary>
        private Tracer m_tracer = Tracer.GetTracer(typeof(InactiveUserPruneJob));

        // Cancel flag
        private bool m_cancelFlag = false;

        /// <summary>
        /// Get the id of this job
        /// </summary>
        public Guid Id => Guid.Parse("B4D43458-F032-4811-979C-43154B30E5F4");

        /// <summary>
        /// Get the name of the job
        /// </summary>
        public string Name => "Prune Inactive Users";

        /// <summary>
        /// Can cancel the job?
        /// </summary>
        public bool CanCancel => true;

        /// <summary>
        /// Gets the current state
        /// </summary>
        public JobStateType CurrentState { get; private set; }

        /// <summary>
        /// Parameters to the job
        /// </summary>
        public IDictionary<string, Type> Parameters => new Dictionary<String, Type>()
        {
            { "max_idle_time", typeof(Int32) }
        };

        /// <summary>
        /// Gets last time the job was started
        /// </summary>
        public DateTime? LastStarted { get; private set; }

        /// <summary>
        /// Gets the time that the job was finished
        /// </summary>
        public DateTime? LastFinished { get; private set; }

        /// <summary>
        /// Gets the progress
        /// </summary>
        public float Progress { get; private set; }

        /// <summary>
        /// Gets the status text
        /// </summary>
        public string StatusText { get; private set; }

        /// <summary>
        /// Cancel the operation
        /// </summary>
        public void Cancel()
        {
            this.m_cancelFlag = true;
            this.CurrentState = JobStateType.Cancelled;
        }

        /// <summary>
        /// Run the job
        /// </summary>
        public void Run(object sender, EventArgs e, object[] parameters)
        {
            try
            {
                this.CurrentState = JobStateType.Running;
                this.LastStarted = DateTime.Now;

                using (AuthenticationContext.EnterSystemContext())
                {
                    this.m_tracer.TraceInfo("Will notify users of inactivity...");
                    int days = parameters.Length > 0 ? (int)parameters[0] : 28;
                    var userRepository = ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityUser>>();
                    var entityRepository = ApplicationServiceContext.Current.GetService<IRepositoryService<UserEntity>>();
                    var notificationService = ApplicationServiceContext.Current.GetService<INotificationService>();
                    var templateService = ApplicationServiceContext.Current.GetService<INotificationTemplateFiller>();

                    DateTimeOffset cutoff = DateTimeOffset.Now.AddDays(-days);

                    this.StatusText = "Pruning Users";

                    int offset = 0, totalResults = 1;
                    while (offset < totalResults)
                    {
                        this.Progress = (float)offset / (float)totalResults;
                        List<SecurityUser> actionedUser = new List<SecurityUser>(10);

                        // Users who haven't logged in 
                        foreach (var usr in userRepository.Find(o => o.UserClass == UserClassKeys.HumanUser && o.LastLoginTime < cutoff, offset, 100, out totalResults))
                        {
                            // Cancel request?
                            if (this.m_cancelFlag) break;

                            double daysSinceLastLogin = 0;
                            if (!usr.LastLoginTime.HasValue)
                                daysSinceLastLogin = DateTimeOffset.Now.Subtract(usr.CreationTime).TotalDays;
                            else
                                daysSinceLastLogin = DateTimeOffset.Now.Subtract(usr.LastLoginTime.Value).TotalDays;

                            // To which address?
                            String[] to = null;
                            if (usr.EmailConfirmed)
                                to = new string[] { $"mailto:{usr.Email}" };
                            else if (usr.PhoneNumberConfirmed)
                                to = new string[] { $"tel:{usr.Email}" };

                            // Template and action
                            string templateId = null;
                            if (daysSinceLastLogin > days + 7) // a week since we notified them, obsolete
                            {
                                actionedUser.Add(usr);
                                this.m_tracer.TraceVerbose("De-activating user {0}...", usr.UserName);
                                userRepository.Obsolete(usr.Key.Value);
                                templateId = "org.santedb.notification.security.user.inactiveRemoved";
                            }
                            else
                                templateId = "org.santedb.notification.security.user.inactiveWarned";

                            var entity = entityRepository.Find(o => o.SecurityUserKey == usr.Key, 0, 1, out int _).FirstOrDefault();
                            var lang = entity?.GetPersonLanguages()?.FirstOrDefault(o => o.IsPreferred)?.LanguageCode;

                            var template = templateService.FillTemplate(templateId, lang, new
                            {
                                removalDays = Math.Round((days + 7) - daysSinceLastLogin),
                                removalDate = DateTime.Now.AddDays((days + 7) - daysSinceLastLogin).Date,
                                user = usr,
                                entity = entity
                            });

                            // Send the notification
                            this.m_tracer.TraceVerbose("Sending {0} notification to {1}...", templateId, usr.UserName);
                            notificationService.Send(to, template.Subject, template.Body);
                        }
                        offset += 100;

                        // Audit that we deleted users
                        if (actionedUser.Count > 0)
                            AuditUtil.AuditSecurityDeletionAction(actionedUser, true, new String[] { "obsoletionTime" });
                    }
                    this.LastFinished = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                this.m_tracer.TraceError("Error pruning inactive users : {0}", ex);
                this.CurrentState = JobStateType.Aborted;
            }
            finally
            {
                this.m_cancelFlag = false;
                this.StatusText = String.Empty;
                this.Progress = 1.0f;
            }
        }
    }
}
