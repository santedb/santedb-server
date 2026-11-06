using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office.Y2022.FeaturePropertyBag;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Event;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Notifications;
using SanteDB.Core.Services;
using SanteDB.Persistence.Diagnostics.Trello.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Text;
using TrelloDotNet;
using TrelloDotNet.Model;

namespace SanteDB.Persistence.Diagnostics.Trello
{
    /// <summary>
    /// Service which sends diagnostic reports to Trello
    /// </summary>
    public class TrelloDiagnosticReportService : IDataPersistenceService<DiagnosticReport>
    {
        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(TrelloDiagnosticReportService));

        // Configuration 
        private readonly TrelloConfigurationSection m_configuration;

        // List id
        private string m_trelloListId = String.Empty;

        /// <summary>
        /// DI constructor
        /// </summary>
        public TrelloDiagnosticReportService(IConfigurationManager configurationManager)
        {
            this.m_configuration = configurationManager.GetSection<TrelloConfigurationSection>();
            if(this.m_configuration == null)
            {
                throw new InvalidOperationException(String.Format(ErrorMessages.DEPENDENT_CONFIGURATION_MISSING, nameof(TrelloConfigurationSection)));
            }
        }

        /// <inheritdoc/>
        public string ServiceName => throw new NotImplementedException();

        /// <inheritdoc/>
        public event EventHandler<DataPersistedEventArgs<DiagnosticReport>> Inserted;
        /// <inheritdoc/>
        public event EventHandler<DataPersistingEventArgs<DiagnosticReport>> Inserting;
        /// <inheritdoc/>
        public event EventHandler<DataPersistedEventArgs<DiagnosticReport>> Updated;
        /// <inheritdoc/>
        public event EventHandler<DataPersistingEventArgs<DiagnosticReport>> Updating;
        /// <inheritdoc/>
        public event EventHandler<DataPersistedEventArgs<DiagnosticReport>> Deleted;
        /// <inheritdoc/>
        public event EventHandler<DataPersistingEventArgs<DiagnosticReport>> Deleting;
        /// <inheritdoc/>
        public event EventHandler<QueryResultEventArgs<DiagnosticReport>> Queried;
        /// <inheritdoc/>
        public event EventHandler<QueryRequestEventArgs<DiagnosticReport>> Querying;
        /// <inheritdoc/>
        public event EventHandler<DataRetrievingEventArgs<DiagnosticReport>> Retrieving;
        /// <inheritdoc/>
        public event EventHandler<DataRetrievedEventArgs<DiagnosticReport>> Retrieved;

        /// <summary>
        /// Get client for the 
        /// </summary>
        /// <returns></returns>
        private TrelloClient GetClient() => new TrelloClient(this.m_configuration.ApiKey, this.m_configuration.ApiToken, new TrelloDotNet.Model.TrelloClientOptions()
        {
            AllowDeleteOfBoards = false,
            AllowDeleteOfOrganizations = false,
            ApiCallExceptionOption = ApiCallExceptionOption.DoNotIncludeTheUrl,
            SendCredentialsMode = SendCredentialsMode.Header
        });

        /// <summary>
        /// Get the trello list identifier
        /// </summary>
        private string GetTrelloListId(TrelloClient client)
        {
            if(String.IsNullOrEmpty(this.m_trelloListId))
            {
                try
                {
                    var boards = client.GetBoardsCurrentTokenCanAccessAsync().Result;
                    var board = boards.FirstOrDefault(o=>o.Name == this.m_configuration.BoardName); 
                    if(board == null)
                    {
                        throw new KeyNotFoundException(String.Format(ErrorMessages.OBJECT_NOT_FOUND, this.m_configuration.BoardName));
                    }

                    var lists =  client.GetListsOnBoardAsync(board.Id).Result;
                    var list = lists.First(o => o.Name == this.m_configuration.ListName);
                    this.m_trelloListId = list?.Id;
                    if(this.m_trelloListId == null)
                    {
                        throw new KeyNotFoundException(String.Format(ErrorMessages.OBJECT_NOT_FOUND, this.m_configuration.ListName));
                    }

                    // Map the labels
                    this.m_configuration.Labels = this.m_configuration.Labels.SelectMany(o => client.GetLabelsOfBoardAsync(board.Id).Result.Where(l=>l.Name == o)).Select(o => o.Id).ToList();

                }
                catch(Exception ex)
                {
                    throw new DataPersistenceException("Could not retrieve configured Trello list", ex);
                }
            }
            return this.m_trelloListId;
        }

        /// <inheritdoc/>
        public long Count(Expression<Func<DiagnosticReport, bool>> query, IPrincipal authContext = null)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public DiagnosticReport Delete(Guid key, TransactionMode transactionMode, IPrincipal principal)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public DiagnosticReport Get(Guid key, Guid? versionKey, IPrincipal principal)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public DiagnosticReport Insert(DiagnosticReport data, TransactionMode transactionMode, IPrincipal principal)
        {
            if(data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if(transactionMode != TransactionMode.Commit)
            {
                return data;
            }

            try
            {
                var client = this.GetClient();
                var listId = this.GetTrelloListId(client);

                var submitterName = data.LoadProperty(o=>o.CreatedBy)?.LoadProperty(o=>o.User)?.UserName ?? data?.Submitter?.LoadProperty(o=>o.SecurityUser)?.UserName ?? principal?.Identity.Name;

                var newCard = client.AddCardAsync(new TrelloDotNet.Model.Options.AddCardOptions.AddCardOptions()
                {
                    ListId = listId,
                    Name = $"Issue from {submitterName}",
                    Description = $"## Description\r\n{data.Note}\r\n## Summary Tags\r\n```\r\n{String.Join("\r\n", data.Tags?.Select(o => $"{o.TagKey} = {o.Value}"))}\r\n```",
                    Start = DateTimeOffset.Now,
                    LabelIds = this.m_configuration.Labels,
                    AttachmentFileUploads = data.Attachments.Select(a =>
                    {
                        switch (a)
                        {
                            case DiagnosticBinaryAttachment bin:
                                return new AttachmentFileUpload(new MemoryStream(bin.Content), bin.FileName);
                            case DiagnosticTextAttachment txt:
                                return new AttachmentFileUpload(new MemoryStream(Encoding.UTF8.GetBytes(txt.Content)), txt.FileName);
                            default:
                                return null;
                        }
                    }).OfType<AttachmentFileUpload>().ToList()
                }).Result;

                data.CorrelationId = newCard.Id;
                return data;
            }
            catch(Exception ex)
            {
                throw new DataPersistenceException(String.Format(ErrorMessages.WRITE_ERROR, ex.Message), ex);
            }
        }

        /// <inheritdoc/>
        public IQueryResultSet<DiagnosticReport> Query(Expression<Func<DiagnosticReport, bool>> query, IPrincipal principal)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public IEnumerable<DiagnosticReport> Query(Expression<Func<DiagnosticReport, bool>> query, int offset, int? count, out int totalResults, IPrincipal principal, params ModelSort<DiagnosticReport>[] orderBy)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public DiagnosticReport Update(DiagnosticReport data, TransactionMode transactionMode, IPrincipal principal)
        {
            throw new NotSupportedException();
        }
    }
}
