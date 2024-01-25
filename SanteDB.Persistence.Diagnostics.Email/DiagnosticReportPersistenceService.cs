/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Event;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Notifications;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Persistence.Diagnostics.Email.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Xml.Serialization;

namespace SanteDB.Persistence.Diagnostics.Email
{
    /// <summary>
    /// Persistence service for diagnostics
    /// </summary>
#pragma warning disable CS0067

    [ServiceProvider("E-Mail Diagnostic (Bug) Report Submission")]
    [ExcludeFromCodeCoverage]
    public class DiagnosticReportPersistenceService : IDataPersistenceService<DiagnosticReport>
    {

        /// <summary>
        /// Policy enforcement service
        /// </summary>
        /// <param name="policyEnforcementService"></param>
        public DiagnosticReportPersistenceService(IPolicyEnforcementService policyEnforcementService)
        {
            this.m_pepService = policyEnforcementService;
        }

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "E-Mail Diagnostic Report Submission";

        // Trace source
        private readonly Tracer m_traceSource = new Tracer("SanteDB.Persistence.Diagnostics.Email");
        private readonly IPolicyEnforcementService m_pepService;

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
        public event EventHandler<DataPersistedEventArgs<DiagnosticReport>> Deleted;

        /// <summary>
        /// Not supported
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<DiagnosticReport>> Deleting;

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
        public DiagnosticReport Insert(DiagnosticReport storageData, TransactionMode mode, IPrincipal overrideAuthContext = null)
        {

            this.m_pepService.Demand(PermissionPolicyIdentifiers.Login, overrideAuthContext ?? AuthenticationContext.Current.Principal);

            var persistenceArgs = new DataPersistingEventArgs<DiagnosticReport>(storageData, mode, overrideAuthContext);
            this.Inserting?.Invoke(this, persistenceArgs);
            if (persistenceArgs.Cancel)
            {
                this.m_traceSource.TraceEvent(EventLevel.Warning, "Pre-persistence event cancelled the insertion");
                return persistenceArgs.Data;
            }

            try
            {
                var issueId = DateTimeOffset.Now.ToString("yyyy-MM-dd-HHmmss");

                var subject = $"SANTEDB ISSUE MAILER #{issueId}";
                var body = $"<html><body><p>{storageData.LoadProperty<SecurityUser>("CreatedBy")?.UserName ?? storageData?.Submitter?.LoadProperty<SecurityUser>("SecurityUser")?.UserName ?? overrideAuthContext?.Identity.Name} has reported a bug</p><pre>{storageData.Note}</pre><p>You can reply directly to the reporter by pressing the Reply button in your mail client</p></body></html>";
                var attachments = storageData.Attachments.Select(a =>
                {
                    if (a is DiagnosticBinaryAttachment bin)
                    {
                        return new NotificationAttachment(a.FileName ?? a.FileDescription, a.ContentType ?? "application/x-gzip", bin.Content);
                    }
                    else if (a is DiagnosticTextAttachment txt)
                    {
                        return new NotificationAttachment(a.FileName ?? a.FileDescription, a.ContentType ?? "text/plain", txt.Content);
                    }
                    else
                    {
                        return new NotificationAttachment(a.FileName ?? a.FileDescription, a.ContentType ?? "text/plain", $"Unknown attachment - {a}");
                    }
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
                notificationService?.SendNotification(recipients, subject, body, null, true, attachments.ToArray());

                // Invoke
                this.Inserted?.Invoke(this, new DataPersistedEventArgs<DiagnosticReport>(storageData, mode, overrideAuthContext));
                storageData.CorrelationId = issueId;
                storageData.Key = Guid.NewGuid();
                return storageData;
            }
            catch (Exception ex)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, "Error sending to E-Mail: {0}", ex);
                throw new InvalidOperationException("Error sending diagnostic reports to administrative contacts", ex);
            }
        }

        /// <summary>
        /// Not supported
        /// </summary>
        public DiagnosticReport Obsolete(Guid storageData, TransactionMode mode, IPrincipal overrideAuthContext = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not supported
        /// </summary>
        public DiagnosticReport Delete(Guid storageData, TransactionMode mode, IPrincipal overrideAuthContext)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not supported
        /// </summary>
        public IQueryResultSet<DiagnosticReport> Query(Expression<Func<DiagnosticReport, bool>> query, IPrincipal overrideAuthContext = null)
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

        /// <summary>
        /// Not supported - obsoleting DX reports
        /// </summary>
        public void ObsoleteAll(Expression<Func<DiagnosticReport, bool>> matching, TransactionMode mode, IPrincipal principal)
        {
            throw new NotImplementedException();
        }


        public IQueryResultSet<DiagnosticReport> Query<TExpression>(Expression<Func<TExpression, bool>> query, IPrincipal principal) where TExpression : DiagnosticReport
        {
            throw new NotImplementedException();
        }
    }

#pragma warning restore CS0067
}