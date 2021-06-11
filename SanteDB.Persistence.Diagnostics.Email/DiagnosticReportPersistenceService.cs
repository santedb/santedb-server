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
using SanteDB.Core;
using SanteDB.Core.Event;
using SanteDB.Core.Model;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Server.Core.Security.Attribute;
using SanteDB.Core.Services;
using SanteDB.Persistence.Diagnostics.Email.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mail;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Xml.Serialization;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Diagnostics;
using System.Diagnostics.Tracing;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Notifications;
using SanteDB.Server.Core.Security.Attribute;

namespace SanteDB.Persistence.Diagnostics.Email
{
    /// <summary>
    /// Persistence service for diagnostics
    /// </summary>
#pragma warning disable CS0067
    [ServiceProvider("E-Mail Diagnostic (Bug) Report Submission")]
    public class DiagnosticReportPersistenceService : IDataPersistenceService<DiagnosticReport>
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "E-Mail Diagnostic Report Submission";

        // Trace source
        private Tracer m_traceSource = new Tracer("SanteDB.Persistence.Diagnostics.Email");

        // Configuration
        private DiagnosticEmailServiceConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<DiagnosticEmailServiceConfigurationSection>();

        /// Fired when an issue is being inserted
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<DiagnosticReport>> Inserted;
        /// <summary>
        /// Fired when the issue is being inserted
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<DiagnosticReport>> Inserting;
        /// <summary>
        /// Not supported
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<DiagnosticReport>> Obsoleted;
        /// <summary>
        /// Not supported
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<DiagnosticReport>> Obsoleting;
        /// <summary>
        /// Not supported
        /// </summary>
        public event EventHandler<QueryResultEventArgs<DiagnosticReport>> Queried;
        /// <summary>
        /// Not supported
        /// </summary>
        public event EventHandler<QueryRequestEventArgs<DiagnosticReport>> Querying;
        /// <summary>
        /// Not supported
        /// </summary>
        public event EventHandler<DataRetrievedEventArgs<DiagnosticReport>> Retrieved;
        /// <summary>
        /// Not supported
        /// </summary>
        public event EventHandler<DataRetrievingEventArgs<DiagnosticReport>> Retrieving;
        /// <summary>
        /// Not supported
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<DiagnosticReport>> Updated;
        /// <summary>
        /// Not supported
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<DiagnosticReport>> Updating;

        /// <summary>
        /// Not supported
        /// </summary>
        public long Count(Expression<Func<DiagnosticReport, bool>> query, IPrincipal overrideAuthContext = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not supported
        /// </summary>
        public DiagnosticReport Get(Guid containerId, Guid? versionId, IPrincipal overrideAuthContext = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Inserts the specified diagnostic report
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.Login)]
        public DiagnosticReport Insert(DiagnosticReport storageData, TransactionMode mode, IPrincipal overrideAuthContext = null)
        {
            var persistenceArgs = new DataPersistingEventArgs<DiagnosticReport>(storageData, overrideAuthContext);
            this.Inserting?.Invoke(this, persistenceArgs);
            if (persistenceArgs.Cancel)
            {
                this.m_traceSource.TraceEvent(EventLevel.Warning, "Pre-persistence event cancelled the insertion");
                return persistenceArgs.Data;
            }

            try
            {
                var issueId = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");

                var subject = $"SANTEDB ISSUE MAILER #{issueId}";
                var body = $"<html><body><p>{storageData.LoadProperty<SecurityUser>("CreatedBy")?.UserName ?? storageData?.Submitter?.LoadProperty<SecurityUser>("SecurityUser")?.UserName} has reported a bug</p><pre>{storageData.Note}</pre><p>You can reply directly to the reporter by pressing the Reply button in your mail client</p></body></html>";
                var attachments = storageData.Attachments.Select(a =>
                {
                    if (a is DiagnosticBinaryAttachment bin)
                        return new NotificationAttachment(a.FileName ?? a.FileDescription, a.ContentType ?? "application/x-gzip", bin.Content);
                    else if (a is DiagnosticTextAttachment txt)
                        return new NotificationAttachment(a.FileName ?? a.FileDescription, a.ContentType ?? "text/plain", txt.Content);
                    else
                        return new NotificationAttachment(a.FileName ?? a.FileDescription, a.ContentType ?? "text/plain", $"Unknown attachment - {a}");
                }).ToList();

                // Attach the application information
                using (var ms = new MemoryStream())
                {
                    XmlSerializer xsz = XmlModelSerializerFactory.Current.CreateSerializer(typeof(DiagnosticApplicationInfo));
                    xsz.Serialize(ms, storageData.ApplicationInfo);
                    attachments.Add(new NotificationAttachment("appinfo.xml", "text/xml", ms.ToArray()));
                }

                var notificationService = ApplicationServiceContext.Current.GetService<INotificationService>();
                var recipients = this.m_configuration?.Recipients.Select(o => o.StartsWith("mailto:") ? o : $"mailto:{o}").ToArray();
                notificationService?.Send(recipients, subject, body, null,  true, attachments.ToArray());

                // Invoke
                this.Inserted?.Invoke(this, new DataPersistedEventArgs<DiagnosticReport>(storageData, overrideAuthContext));
                storageData.CorrelationId = issueId;
                storageData.Key = Guid.NewGuid();
                return storageData;
            }
            catch (Exception ex)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error,  "Error sending to JIRA: {0}", ex);
                throw;
            }

        }

        /// <summary>
        /// Not supported
        /// </summary>
        public DiagnosticReport Obsolete(DiagnosticReport storageData, TransactionMode mode, IPrincipal overrideAuthContext = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not supported
        /// </summary>
        public IEnumerable<DiagnosticReport> Query(Expression<Func<DiagnosticReport, bool>> query, IPrincipal overrideAuthContext = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not supported
        /// </summary>
        public IEnumerable<DiagnosticReport> Query(Expression<Func<DiagnosticReport, bool>> query, int offset, int? count, out int totalCount, IPrincipal overrideAuthContext, params ModelSort<DiagnosticReport>[] orderBy)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not supported
        /// </summary>
        public DiagnosticReport Update(DiagnosticReport storageData, TransactionMode mode, IPrincipal overrideAuthContext = null)
        {
            throw new NotImplementedException();
        }
    }
    #pragma warning restore CS0067

}
