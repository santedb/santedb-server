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
 * Date: 2019-1-22
 */
using RestSrvr;
using SanteDB.Core.Auditing;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace SanteDB.Core.Security.Audit
{
    /// <summary>
    /// A daemon service which listens to audit sources and forwards them to the auditor
    /// </summary>
    [ServiceProvider("SECURITY AUDIT SERVICE")]
    public class AuditDaemonService : IDaemonService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Detailed Persistence Layer Audit Subscription Service";

        private bool m_safeToStop = false;

        // Tracer class
        private Tracer m_tracer = new Tracer(SanteDBConstants.SecurityTraceSourceName);

        /// <summary>
        ///  True if the service is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// The service has started
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// The service is starting
        /// </summary>
        public event EventHandler Starting;
        /// <summary>
        /// The service has stopped
        /// </summary>
        public event EventHandler Stopped;
        /// <summary>
        /// The service is stopping
        /// </summary>
        public event EventHandler Stopping;
        
        /// <summary>
        /// Start auditor service
        /// </summary>
        public bool Start()
        {
            this.Starting?.Invoke(this, EventArgs.Empty);

            this.m_safeToStop = false;
            ApplicationServiceContext.Current.Stopping += (o, e) =>
            {
                this.m_safeToStop = true;
                AuditUtil.AuditApplicationStartStop(EventTypeCodes.ApplicationStop);
                this.Stop();
            };
            ApplicationServiceContext.Current.Started += (o, e) =>
            {
                try
                {
                    this.m_tracer.TraceInfo("Binding to service events...");

                    ApplicationServiceContext.Current.GetService<IIdentityProviderService>().Authenticated += (so, se) =>
                    {
                        AuditUtil.AuditLogin(se.Principal, se.UserName, so as IIdentityProviderService, RestOperationContext.Current?.IncomingRequest?.RemoteEndPoint?.ToString(), se.Success);
                    };
                   

                    // Scan for IRepositoryServices and bind to their events as well
                    foreach (var svc in (ApplicationServiceContext.Current as IServiceManager).GetServices().OfType<IAuditEventSource>())
                    {
                        // Audits from the audit repository itself need to be audited however they are not audit data
                        if (svc is IAuditRepositoryService)
                        {
                            svc.DataUpdated += (so, se) => AuditUtil.AuditDataAction<AuditData>(EventTypeCodes.AuditLogUsed, ActionType.Update, AuditableObjectLifecycle.Amendment, EventIdentifierType.ApplicationActivity, se.Success ? OutcomeIndicator.Success : OutcomeIndicator.SeriousFail, null, RestOperationContext.Current?.IncomingRequest?.RemoteEndPoint?.ToString(), se.Objects.OfType<AuditData>().ToArray());
                            svc.DataObsoleted += (so, se) => AuditUtil.AuditDataAction<AuditData>(EventTypeCodes.AuditLogUsed, ActionType.Delete, AuditableObjectLifecycle.Archiving, EventIdentifierType.ApplicationActivity, se.Success ? OutcomeIndicator.Success : OutcomeIndicator.SeriousFail, null, RestOperationContext.Current?.IncomingRequest?.RemoteEndPoint?.ToString(), se.Objects.OfType<AuditData>().ToArray());
                            svc.DataDisclosed += (so, se) => AuditUtil.AuditDataAction<AuditData>(EventTypeCodes.AuditLogUsed, ActionType.Execute, AuditableObjectLifecycle.Access, EventIdentifierType.Query, se.Success ? OutcomeIndicator.Success : OutcomeIndicator.SeriousFail, se.Query, RestOperationContext.Current?.IncomingRequest?.RemoteEndPoint?.ToString(), se.Objects.OfType<AuditData>().ToArray());
                        }
                        else {
                            svc.DataCreated += (so, se) => AuditUtil.AuditDataAction(EventTypeCodes.PatientRecord, ActionType.Create, AuditableObjectLifecycle.Creation, EventIdentifierType.PatientRecord, se.Success ? OutcomeIndicator.Success : OutcomeIndicator.SeriousFail, null, RestOperationContext.Current?.IncomingRequest?.RemoteEndPoint?.ToString(), se.Objects.OfType<IdentifiedData>().ToArray());
                            svc.DataUpdated += (so, se) => AuditUtil.AuditDataAction(EventTypeCodes.PatientRecord, ActionType.Update, AuditableObjectLifecycle.Amendment, EventIdentifierType.PatientRecord, se.Success ? OutcomeIndicator.Success : OutcomeIndicator.SeriousFail, null, RestOperationContext.Current?.IncomingRequest?.RemoteEndPoint?.ToString(), se.Objects.OfType<IdentifiedData>().ToArray());
                            svc.DataObsoleted += (so, se) => AuditUtil.AuditDataAction(EventTypeCodes.PatientRecord, ActionType.Delete, AuditableObjectLifecycle.LogicalDeletion, EventIdentifierType.PatientRecord, se.Success ? OutcomeIndicator.Success : OutcomeIndicator.SeriousFail, RestOperationContext.Current?.IncomingRequest?.RemoteEndPoint?.ToString(), null, se.Objects.OfType<IdentifiedData>().ToArray());
                            svc.DataDisclosed += (so, se) => AuditUtil.AuditDataAction<IdentifiedData>(EventTypeCodes.Query, ActionType.Read, AuditableObjectLifecycle.Disclosure, EventIdentifierType.Query, se.Success ? OutcomeIndicator.Success : OutcomeIndicator.SeriousFail, se.Query, RestOperationContext.Current?.IncomingRequest?.RemoteEndPoint?.ToString());

                            if (svc is ISecurityAuditEventSource)
                            {
                                (svc as ISecurityAuditEventSource).SecurityAttributesChanged += (so, se) => AuditUtil.AuditSecurityAttributeAction(se.Objects, se.Success, se.ChangedProperties.ToArray(), RestOperationContext.Current?.IncomingRequest?.RemoteEndPoint?.ToString());
                                (svc as ISecurityAuditEventSource).SecurityResourceCreated += (so, se) => AuditUtil.AuditSecurityCreationAction(se.Objects, se.Success, se.ChangedProperties, RestOperationContext.Current?.IncomingRequest?.RemoteEndPoint?.ToString());
                                (svc as ISecurityAuditEventSource).SecurityResourceDeleted += (so, se) => AuditUtil.AuditSecurityDeletionAction(se.Objects, se.Success, se.ChangedProperties, RestOperationContext.Current?.IncomingRequest?.RemoteEndPoint?.ToString());
                            }
                        }
                    }

                    AuditUtil.AuditApplicationStartStop(EventTypeCodes.ApplicationStart);
                }
                catch (Exception ex)
                {
                    this.m_tracer.TraceError("Error starting up audit repository service: {0}", ex);
                }
            };

            this.Started?.Invoke(this, EventArgs.Empty);
            return true;
        }


        /// <summary>
        /// Stopped 
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);

            // Audit tool should never stop!!!!!
            if (!this.m_safeToStop)
            {
                AuditData securityAlertData = new AuditData(DateTime.Now, ActionType.Execute, OutcomeIndicator.EpicFail, EventIdentifierType.SecurityAlert, AuditUtil.CreateAuditActionCode(EventTypeCodes.UseOfARestrictedFunction));
                AuditUtil.AddLocalDeviceActor(securityAlertData);
                AuditUtil.SendAudit(securityAlertData);
            }

            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }
}
