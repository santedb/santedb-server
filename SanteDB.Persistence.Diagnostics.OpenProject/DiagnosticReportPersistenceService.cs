using SanteDB.Core;
using SanteDB.Core.Event;
using SanteDB.Core.Http;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Services;
using SanteDB.Persistence.Diagnostics.OpenProject.Api;
using SanteDB.Persistence.Diagnostics.OpenProject.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Persistence.Diagnostics.OpenProject
{
    /// <summary>
    /// A diagnostic report persister for OpenProject
    /// </summary>
    [ServiceProvider("OpenProject Diagnostics", Configuration = typeof(OpenProjectConfigurationSection))]
    public class DiagnosticReportPersistenceService : IDataPersistenceService<DiagnosticReport>
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "OpenProject Diagnostic Persister";

        // Trace source
        private TraceSource m_traceSource = new TraceSource("SanteDB.Persistence.Diagnostics.OpenProject");

        /// <summary>
        /// Gets the api
        /// </summary>
        private OpenProjectApiWrapper m_api;

        // Gets the configuration
        private OpenProjectConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<OpenProjectConfigurationSection>();

        /// <summary>
        /// Diagnostic report persistence service
        /// </summary>
        public DiagnosticReportPersistenceService()
        {
            this.m_api = new OpenProjectApiWrapper(new RestClient(new ServiceClientDescription()
            {
                Binding = new ServiceClientBindingDescription()
                {
                    Optimize = false
                },
                EndpointCollection = new List<ServiceClientEndpointDescription>()
                {
                    new ServiceClientEndpointDescription(this.m_configuration.ApiEndpoint)
                },
                Trace = false
            }));
            this.m_api.Client.Credentials = new HttpBasicCredentials("apikey", this.m_configuration.ApiKey);
        }

        //
        public event EventHandler<DataPersistedEventArgs<DiagnosticReport>> Inserted;
        public event EventHandler<DataPersistingEventArgs<DiagnosticReport>> Inserting;
        public event EventHandler<DataPersistedEventArgs<DiagnosticReport>> Updated;
        public event EventHandler<DataPersistingEventArgs<DiagnosticReport>> Updating;
        public event EventHandler<DataPersistedEventArgs<DiagnosticReport>> Obsoleted;
        public event EventHandler<DataPersistingEventArgs<DiagnosticReport>> Obsoleting;
        public event EventHandler<QueryResultEventArgs<DiagnosticReport>> Queried;
        public event EventHandler<QueryRequestEventArgs<DiagnosticReport>> Querying;
        public event EventHandler<DataRetrievingEventArgs<DiagnosticReport>> Retrieving;
        public event EventHandler<DataRetrievedEventArgs<DiagnosticReport>> Retrieved;

        public long Count(Expression<Func<DiagnosticReport, bool>> p, IPrincipal authContext = null)
        {
            throw new NotSupportedException();
        }

        public DiagnosticReport Get(Guid key, Guid? versionKey, bool loadFast, IPrincipal principal)
        {
            throw new NotSupportedException();
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
                // Send 
                var issue = this.m_api.CreateWorkPackage(this.m_configuration.ProjectKey, new WorkPackage()
                {
                    Subject = $"SanteDB-DIAG: Issue from {storageData?.Submitter?.Names?.FirstOrDefault()?.Component?.FirstOrDefault(n => n.ComponentTypeKey == NameComponentKeys.Given)?.Value}",
                    Description = new FormattedText()
                    {
                        Raw = storageData.Note 
                    },
                    Type = this.m_configuration.BugType
                });

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
                    XmlSerializer xsz = new XmlSerializer(typeof(DiagnosticApplicationInfo));
                    xsz.Serialize(ms, storageData.ApplicationInfo);
                    attachments.Add(new MultipartAttachment(ms.ToArray(), "text/xml", "appinfo.xml", true));
                }

                // Create attachment
                this.m_api.CreateAttachments(issue, attachments);
                storageData.CorrelationId = issue.Id.ToString();
                storageData.Key = Guid.NewGuid();

                // Invoke
                this.Inserted?.Invoke(this, new DataPersistedEventArgs<DiagnosticReport>(storageData, overrideAuthContext));

                return storageData;
            }
            catch (Exception ex)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, ex.HResult, "Error sending to JIRA: {0}", ex);
                throw;
            }


        }

        public DiagnosticReport Obsolete(DiagnosticReport data, TransactionMode mode, IPrincipal principal)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<DiagnosticReport> Query(Expression<Func<DiagnosticReport, bool>> query, IPrincipal principal)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<DiagnosticReport> Query(Expression<Func<DiagnosticReport, bool>> query, int offset, int? count, out int totalResults, IPrincipal principal, params ModelSort<DiagnosticReport>[] orderBy)
        {
            throw new NotSupportedException();
        }

        public DiagnosticReport Update(DiagnosticReport data, TransactionMode mode, IPrincipal principal)
        {
            throw new NotSupportedException();
        }
    }
}
