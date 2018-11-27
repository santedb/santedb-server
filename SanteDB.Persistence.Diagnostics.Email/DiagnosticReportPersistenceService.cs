/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: justin
 * Date: 2018-6-22
 */
using SanteDB.Core;
using SanteDB.Core.Event;
using SanteDB.Core.Model;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
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
        private TraceSource m_traceSource = new TraceSource("SanteDB.Persistence.Diagnostics.Email");

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
        public DiagnosticReport Get(Guid containerId, Guid? versionId, bool loadFast = false, IPrincipal overrideAuthContext = null)
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
                this.m_traceSource.TraceEvent(TraceEventType.Warning, 0, "Pre-persistence event cancelled the insertion");
                return persistenceArgs.Data;
            }

            try
            {
                var issueId = Guid.NewGuid().ToString().Substring(0, 4);
                // Setup message

                MailMessage bugMessage = new MailMessage();
                bugMessage.From = new MailAddress(this.m_configuration.Smtp.From, $"SanteDB Bug on behalf of {storageData?.Submitter?.Names?.FirstOrDefault()?.Component?.FirstOrDefault(n => n.ComponentTypeKey == NameComponentKeys.Given)?.Value}");
                foreach (var itm in this.m_configuration.Recipients)
                    bugMessage.To.Add(itm);
                bugMessage.Subject = $"ISSUE #{issueId}";
                bugMessage.Body = $"{storageData.Note} - Username - {storageData.LoadProperty<SecurityUser>("CreatedBy")?.UserName ?? storageData?.Submitter?.LoadProperty<SecurityUser>("SecurityUser")?.UserName}";


                // Add attachments
                foreach (var itm in storageData.Attachments)
                {
                    if (itm is DiagnosticBinaryAttachment)
                    {
                        var bin = itm as DiagnosticBinaryAttachment;
                        bugMessage.Attachments.Add(new Attachment(new MemoryStream(bin.Content), itm.FileDescription ?? itm.FileName, "application/x-gzip"));
                    }
                    else
                    {
                        var txt = itm as DiagnosticTextAttachment;
                        bugMessage.Attachments.Add(new Attachment(new MemoryStream(Encoding.UTF8.GetBytes(txt.Content)), itm.FileDescription ?? itm.FileName, "text/plain"));
                    }
                }

                // Attach the application information
                using (var ms = new MemoryStream())
                {
                    XmlSerializer xsz = new XmlSerializer(typeof(DiagnosticApplicationInfo));
                    xsz.Serialize(ms, storageData.ApplicationInfo);
                    bugMessage.Attachments.Add(new Attachment(new MemoryStream(ms.ToArray()), "appinfo.xml", "text/xml"));
                }

                SmtpClient smtpClient = new SmtpClient(this.m_configuration.Smtp.Server.Host, this.m_configuration.Smtp.Server.Port);
                smtpClient.UseDefaultCredentials = String.IsNullOrEmpty(this.m_configuration.Smtp.Username);
                smtpClient.EnableSsl = this.m_configuration.Smtp.Ssl;
                if (!(smtpClient.UseDefaultCredentials))
                    smtpClient.Credentials = new NetworkCredential(this.m_configuration.Smtp.Username, this.m_configuration.Smtp.Password);
                smtpClient.SendCompleted += (o, e) =>
                {
                    this.m_traceSource.TraceInformation("Successfully sent message to {0}", bugMessage.To);
                    if (e.Error != null)
                        this.m_traceSource.TraceEvent(TraceEventType.Error, 0, e.Error.ToString());
                    (o as IDisposable).Dispose();
                };
                this.m_traceSource.TraceInformation("Sending bug email message to {0}", bugMessage.To);
                smtpClient.Send(bugMessage);

                // Invoke
                this.Inserted?.Invoke(this, new DataPersistedEventArgs<DiagnosticReport>(storageData, overrideAuthContext));
                storageData.CorrelationId = issueId;
                storageData.Key = Guid.NewGuid();
                return storageData;
            }
            catch (Exception ex)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, ex.HResult, "Error sending to JIRA: {0}", ex);
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
        public IEnumerable<DiagnosticReport> Query(Expression<Func<DiagnosticReport, bool>> query, int offset, int? count, out int totalCount, IPrincipal overrideAuthContext = null)
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
