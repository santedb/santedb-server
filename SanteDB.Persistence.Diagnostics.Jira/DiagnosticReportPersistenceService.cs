/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using SanteDB.Core;
using SanteDB.Core.Event;
using SanteDB.Core.Http;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Server.Core.Security.Attribute;
using SanteDB.Core.Services;
using SanteDB.Persistence.Diagnostics.Jira.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Xml.Serialization;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Diagnostics;
using System.Diagnostics.Tracing;
using SanteDB.Core.Model.Serialization;
using SanteDB.Server.Core.Http;

namespace SanteDB.Persistence.Diagnostics.Jira
{
    /// <summary>
    /// Diagnostic report persistence service.
    /// </summary>
#pragma warning disable CS0067

    [ServiceProvider("JIRA Based Diagnostic (Bug) Report Submissions")]
    public class JiraDiagnosticReportPersistenceService : IDataPersistenceService<DiagnosticReport>
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "JIRA Diagnostic Report Submission";

        // Trace source
        private readonly Tracer m_traceSource = new Tracer("SanteDB.Persistence.Diagnostics.Jira");

        // Configuration
        private JiraServiceConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<JiraServiceConfigurationSection>();

        /// <summary>
        /// Initializes a new instance of the <see cref="JiraDiagnosticReportPersistenceService"/> class.
        /// </summary>
        public JiraDiagnosticReportPersistenceService()
        {
        }

        /// <summary>
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
        /// Fired after report is deleted (not used)
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<DiagnosticReport>> Deleted;

        /// <summary>
        /// Fired when report is deleting
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<DiagnosticReport>> Deleting;

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
            var persistenceArgs = new DataPersistingEventArgs<DiagnosticReport>(storageData, mode, overrideAuthContext);
            this.Inserting?.Invoke(this, persistenceArgs);
            if (persistenceArgs.Cancel)
            {
                this.m_traceSource.TraceEvent(EventLevel.Warning, "Pre-persistence event cancelled the insertion");
                return persistenceArgs.Data;
            }

            try
            {
                // Send
                var serviceClient = new JiraServiceClient(new RestClient(this.m_configuration));

                serviceClient.Authenticate(new Model.JiraAuthenticationRequest(this.m_configuration.UserName, this.m_configuration.Password));
                var issue = serviceClient.CreateIssue(new Model.JiraIssueRequest()
                {
                    Fields = new Model.JiraIssueFields()
                    {
                        Description = storageData.Note,
                        Summary = String.Format("SanteDB-DIAG: Issue from {0}", storageData?.Submitter?.Names?.FirstOrDefault()?.Component?.FirstOrDefault(n => n.ComponentTypeKey == NameComponentKeys.Given)?.Value),
                        IssueType = new Model.JiraIdentifier("Bug"),
                        Priority = new Model.JiraIdentifier("High"),
                        Project = new Model.JiraKey(this.m_configuration.Project),
                        Labels = new string[] { "SanteDBMobile" }
                    }
                });

                serviceClient.Client.Requesting += (o, e) =>
                {
                    e.AdditionalHeaders.Add("X-Atlassian-Token", "nocheck");
                };
                // Attachments
                List<MultipartAttachment> attachments = new List<MultipartAttachment>();

                foreach (var itm in storageData.Attachments)
                {
                    if (itm is DiagnosticBinaryAttachment)
                    {
                        var bin = itm as DiagnosticBinaryAttachment;
                        attachments.Add(new MultipartAttachment(bin.Content, "application/x-gzip", bin.FileDescription, true));
                    }
                    else
                    {
                        var txt = itm as DiagnosticTextAttachment;
                        attachments.Add(new MultipartAttachment(Encoding.UTF8.GetBytes(txt.Content), "text/plain", txt.FileName, true));
                    }
                }

                // Attach the application information
                using (var ms = new MemoryStream())
                {
                    XmlSerializer xsz = XmlModelSerializerFactory.Current.CreateSerializer(typeof(DiagnosticApplicationInfo));
                    xsz.Serialize(ms, storageData.ApplicationInfo);
                    attachments.Add(new MultipartAttachment(ms.ToArray(), "text/xml", "appinfo.xml", true));
                }
                serviceClient.CreateAttachment(issue, attachments);
                storageData.CorrelationId = issue.Key;
                storageData.Key = Guid.NewGuid();

                // Invoke
                this.Inserted?.Invoke(this, new DataPersistedEventArgs<DiagnosticReport>(storageData, mode, overrideAuthContext));

                return storageData;
            }
            catch (Exception ex)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, "Error sending to JIRA: {0}", ex);
                throw;
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
        /// Deleting from JIRA not supported
        /// </summary>
        public DiagnosticReport Delete(Guid key, TransactionMode mode, IPrincipal principal)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported to query JIRA
        /// </summary>
        public IQueryResultSet<DiagnosticReport> Query<TExpression>(Expression<Func<TExpression, bool>> query, IPrincipal principal) where TExpression : DiagnosticReport
        {
            throw new NotSupportedException();
        }
    }

#pragma warning restore CS0067
}