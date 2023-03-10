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
using SanteDB.Core.Http;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Persistence.Diagnostics.Jira.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Persistence.Diagnostics.Jira
{
    /// <summary>
    /// Diagnostic report persistence service.
    /// </summary>
#pragma warning disable CS0067

    [ServiceProvider("JIRA Based Diagnostic (Bug) Report Submissions")]
    [ExcludeFromCodeCoverage]
    public class JiraDiagnosticReportPersistenceService : IDataPersistenceService<DiagnosticReport>
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "JIRA Diagnostic Report Submission";

        // Trace source
        private readonly Tracer m_traceSource = new Tracer("SanteDB.Persistence.Diagnostics.Jira");
        private readonly IPolicyEnforcementService m_pepService;
        private readonly IRestClientFactory m_restFactory;

        // Configuration
        private JiraServiceConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<JiraServiceConfigurationSection>();

        /// <summary>
        /// Initializes a new instance of the <see cref="JiraDiagnosticReportPersistenceService"/> class.
        /// </summary>
        public JiraDiagnosticReportPersistenceService(IPolicyEnforcementService policyEnforcementService, IRestClientFactory restClientFactory)
        {
            this.m_pepService = policyEnforcementService;
            this.m_restFactory = restClientFactory;
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
                // Send
                var serviceClient = new JiraServiceClient(this.m_restFactory.CreateRestClient(this.m_configuration.ApiConfiguration));

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
                List<MultiPartFormData> attachments = new List<MultiPartFormData>();

                foreach (var itm in storageData.Attachments)
                {
                    if (itm is DiagnosticBinaryAttachment)
                    {
                        var bin = itm as DiagnosticBinaryAttachment;
                        attachments.Add(new MultiPartFormData(bin.FileDescription, bin.Content, "application/x-gzip", bin.FileDescription, true));
                    }
                    else
                    {
                        var txt = itm as DiagnosticTextAttachment;
                        attachments.Add(new MultiPartFormData(txt.FileName, Encoding.UTF8.GetBytes(txt.Content), "text/plain", txt.FileName, true));
                    }
                }

                // Attach the application information
                using (var ms = new MemoryStream())
                {
                    XmlSerializer xsz = XmlModelSerializerFactory.Current.CreateSerializer(typeof(DiagnosticApplicationInfo));
                    xsz.Serialize(ms, storageData.ApplicationInfo);
                    attachments.Add(new MultiPartFormData("appinfo.xml", ms.ToArray(), "text/xml", "applinfo.xml", true));
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