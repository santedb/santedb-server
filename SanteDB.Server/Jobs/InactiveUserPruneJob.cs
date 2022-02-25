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
    public class InactiveUserPruneJob : IJob
    {
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(InactiveUserPruneJob));
        private readonly IRepositoryService<SecurityUser> m_securityUserRepository;
        private readonly IRepositoryService<UserEntity> m_userEntityRepository;
        private readonly INotificationService m_notificationService;
        private readonly INotificationTemplateFiller m_templateFiller;
        private readonly IJobStateManagerService m_stateManager;

        // Cancel flag
        private volatile bool m_cancelFlag = false;

        /// <summary>
        /// DI constructor
        /// </summary>
        public InactiveUserPruneJob(IJobStateManagerService stateManagerService, IRepositoryService<SecurityUser> securityUserRepository, IRepositoryService<UserEntity> userEntityRepository, INotificationService notificationService, INotificationTemplateFiller templateFiller)
        {
            this.m_securityUserRepository = securityUserRepository;
            this.m_userEntityRepository = userEntityRepository;
            this.m_notificationService = notificationService;
            this.m_templateFiller = templateFiller;
            this.m_stateManager = stateManagerService;
        }

        /// <summary>
        /// Get the id of this job
        /// </summary>
        public Guid Id => Guid.Parse("B4D43458-F032-4811-979C-43154B30E5F4");

        /// <summary>
        /// Get the name of the job
        /// </summary>
        public string Name => "Prune Inactive Users";

        /// <inheritdoc/>
        public string Description => "When configured or enabled on a schedule - this job regularly prunes inactive users.";

        /// <summary>
        /// Can cancel the job?
        /// </summary>
        public bool CanCancel => true;

        /// <summary>
        /// Parameters to the job
        /// </summary>
        public IDictionary<string, Type> Parameters => new Dictionary<String, Type>()
        {
            { "max_idle_time", typeof(Int32) }
        };

        /// <summary>
        /// Cancel the operation
        /// </summary>
        public void Cancel()
        {
            this.m_cancelFlag = true;
            this.m_stateManager.SetProgress(this, "Cancel Requested", 0.0f);
        }

        /// <summary>
        /// Run the job
        /// </summary>
        public void Run(object sender, EventArgs e, object[] parameters)
        {
            try
            {
                this.m_stateManager.SetState(this, JobStateType.Running);

                using (AuthenticationContext.EnterSystemContext())
                {
                    this.m_tracer.TraceInfo("Will notify users of inactivity...");
                    int days = parameters.Length > 0 ? (int)parameters[0] : 28;
                    

                    DateTimeOffset cutoff = DateTimeOffset.Now.AddDays(-days);

                    this.StatusText = "Pruning Users";

                    List<SecurityUser> actionedUser = new List<SecurityUser>(10);

                    // Users who haven't logged in
                    foreach (var usr in this.m_securityUserRepository.Find(o => o.UserClass == UserClassKeys.HumanUser && o.LastLoginTime < cutoff))
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

                        var entity = this.m_userEntityRepository.Find(o => o.SecurityUserKey == usr.Key).FirstOrDefault();
                        var lang = entity?.GetPersonLanguages()?.FirstOrDefault(o => o.IsPreferred)?.LanguageCode;

                        // Template and action
                        string templateId = null;
                        if (daysSinceLastLogin > days + 7) // a week since we notified them, obsolete
                        {
                            actionedUser.Add(usr);
                            this.m_tracer.TraceVerbose("De-activating user {0}...", usr.UserName);
                            this.m_securityUserRepository.Delete(usr.Key.Value);
                            templateId = "org.santedb.notification.security.user.inactiveRemoved";
                        }
                        else
                            templateId = "org.santedb.notification.security.user.inactiveWarned";

                       
                        var template = this.m_templateFiller.FillTemplate(templateId, lang, new
                        {
                            removalDays = Math.Round((days + 7) - daysSinceLastLogin),
                            removalDate = DateTime.Now.AddDays((days + 7) - daysSinceLastLogin).Date,
                            user = usr,
                            entity = entity
                        });

                        // Send the notification
                        this.m_tracer.TraceVerbose("Sending {0} notification to {1}...", templateId, usr.UserName);
                        this.m_notificationService.Send(to, template.Subject, template.Body);
                    }

                    // Audit that we deleted users
                    if (actionedUser.Count > 0)
                        AuditUtil.AuditSecurityDeletionAction(actionedUser, true, new String[] { "obsoletionTime" });
                    this.m_stateManager.SetState(this, JobStateType.Completed);
                }
            }
            catch (Exception ex)
            {
                this.m_tracer.TraceError("Error pruning inactive users : {0}", ex);
                this.m_stateManager.SetProgress(this, ex.Message, 0.0f);
                this.m_stateManager.SetState(this, JobStateType.Aborted);
            }
            finally
            {
                this.m_cancelFlag = false;
            }
        }
    }
}