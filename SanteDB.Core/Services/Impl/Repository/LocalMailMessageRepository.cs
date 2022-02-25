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
using SanteDB.Core.Exceptions;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Represents a local alert service.
    /// </summary>
    [ServiceProvider("Local Mail Message")]
    public class LocalMailMessageRepository : IMailMessageRepositoryService,
        IRepositoryService<MailMessage>
    {
        // Localization Service
        private readonly ILocalizationService m_localizationService;

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Local Mail Message Repository";

        /// <summary>
        /// The internal reference to the <see cref="TraceSource"/> instance.
        /// </summary>
        private readonly Tracer traceSource = new Tracer(SanteDBConstants.ServiceTraceSourceName);

        // Data persistence service
        private readonly IDataPersistenceService<MailMessage> m_dataPersistenceService;

        // Security repository service
        private readonly ISecurityRepositoryService m_securityRepositoryService;

        // Role provider serivce
        private readonly IRoleProviderService m_roleProviderService;

        /// <summary>
        /// Fired when an alert was raised and is being processed.
        /// </summary>
        public event EventHandler<MailMessageEventArgs> Committed;

        /// <summary>
        /// Fired when an alert is received.
        /// </summary>
        public event EventHandler<MailMessageEventArgs> Received;

        /// <summary>
        /// Inserted
        /// </summary>
        public event EventHandler<RepositoryEventArgs<MailMessage>> Inserted;

        /// <summary>
        /// Saved
        /// </summary>
        public event EventHandler<RepositoryEventArgs<MailMessage>> Saved;

        /// <summary>
        /// Retrieved
        /// </summary>
        public event EventHandler<RepositoryEventArgs<MailMessage>> Retrieved;

        /// <summary>
        /// Queried
        /// </summary>
        public event EventHandler<RepositoryEventArgs<IEnumerable<MailMessage>>> Queried;

        /// <summary>
        /// Initialize the instance variable
        /// </summary>
        public LocalMailMessageRepository(ILocalizationService localizationService, IDataPersistenceService<MailMessage> dataPersistenceService, ISecurityRepositoryService securityRepositoryService, IRoleProviderService roleProviderService)
        {
            this.m_localizationService = localizationService;
            this.m_dataPersistenceService = dataPersistenceService;
            this.m_securityRepositoryService = securityRepositoryService;
            this.m_roleProviderService = roleProviderService;
        }

        /// <summary>
        /// Broadcasts an alert.
        /// </summary>
        /// <param name="message">The alert message to be broadcast.</param>
        public void Broadcast(MailMessage message)
        {
            try
            {
                this.traceSource.TraceVerbose("Broadcasting alert {0}", message);

                // Broadcast alert
                // TODO: Fix this, this is bad
                var args = new MailMessageEventArgs(message);
                this.Received?.Invoke(this, args);
                if (args.Ignore)
                    return;

                if (message.Flags != MailMessageFlags.Transient)
                    this.Insert(message);

                // Committed
                this.Committed?.BeginInvoke(this, args, null, null);
            }
            catch (Exception e)
            {
                this.traceSource.TraceError("Error broadcasting alert: {0}", e);
            }
        }

        /// <summary>
        /// Searches for alerts.
        /// </summary>
        /// <param name="predicate">The predicate to use to search for alerts.</param>
        /// <param name="offset">The offset of the search.</param>
        /// <param name="count">The count of the search results.</param>
        /// <param name="totalCount">The total count of the alerts.</param>
        /// <returns>Returns a list of alerts.</returns>
        [Obsolete("Use Find(Expression<Func<MailMessage, bool>>)", true)]
        public IEnumerable<MailMessage> Find(Expression<Func<MailMessage, bool>> predicate, int offset, int? count, out int totalCount, params ModelSort<MailMessage>[] orderBy)
        {
            var results = this.Find(predicate);
            if (results is IOrderableQueryResultSet<MailMessage> orderable)
            {
                foreach (var itm in orderBy)
                {
                    switch (itm.SortOrder)
                    {
                        case SanteDB.Core.Model.Map.SortOrderType.OrderBy:
                            results = orderable.OrderBy(itm.SortProperty);
                            break;

                        case SanteDB.Core.Model.Map.SortOrderType.OrderByDescending:
                            results = orderable.OrderByDescending(itm.SortProperty);
                            break;
                    }
                }
            }

            totalCount = results.Count();
            return results.Skip(offset).Take(count ?? 100);
        }

        /// <summary>
        /// Gets an alert.
        /// </summary>
        /// <param name="id">The id of the alert to be retrieved.</param>
        /// <returns>Returns an alert.</returns>
        public MailMessage Get(Guid id)
        {
            var retVal = this.m_dataPersistenceService.Get(id, null, AuthenticationContext.Current.Principal);
            this.Retrieved?.Invoke(this, new RepositoryEventArgs<MailMessage>(retVal));
            return retVal;
        }

        /// <summary>
        /// Inserts an alert message.
        /// </summary>
        /// <param name="message">The alert message to be inserted.</param>
        /// <returns>Returns the inserted alert.</returns>
        public MailMessage Insert(MailMessage message)
        {
            if (String.IsNullOrEmpty(message.To) || string.IsNullOrEmpty(message.From))
            {
                this.traceSource.TraceError("Mail messages must of TO and FROM fields");
                throw new InvalidOperationException(this.m_localizationService.GetString("error.server.core.mailMessage"));
            }

            MailMessage alert = null;

            try
            {
                // Clear the RCPT TO and set via TO
                message.RcptTo.Clear();
                foreach (var rcp in message.To.Split(';').Select(o => o.ToLower()).Distinct())
                {
                    var usr = this.m_securityRepositoryService.GetUser(rcp);
                    if (usr == null) // Group?
                    {
                        var rol = this.m_roleProviderService.FindUsersInRole(rcp);
                        foreach (var mem in rol)
                        {
                            usr = this.m_securityRepositoryService.GetUser(mem);
                            if (usr != null) message.RcptTo.Add(usr);
                        }
                    }
                    else
                        message.RcptTo.Add(usr);
                }

                if (message.TimeStamp == default(DateTimeOffset))
                    message.TimeStamp = DateTimeOffset.Now;

                alert = this.m_dataPersistenceService.Insert(message, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                this.Received?.Invoke(this, new MailMessageEventArgs(alert));
                this.Inserted?.Invoke(this, new RepositoryEventArgs<MailMessage>(alert));
            }
            catch (Exception e)
            {
#if DEBUG
                this.traceSource.TraceEvent(EventLevel.Error, e.StackTrace);
#endif
                this.traceSource.TraceEvent(EventLevel.Error, e.Message);

                throw;
            }

            return alert;
        }

        /// <summary>
        /// Saves an alert.
        /// </summary>
        /// <param name="message">The alert message to be saved.</param>
        public MailMessage Save(MailMessage message)
        {
            MailMessage alert = null;

            try
            {
                // obsolete the alert
                alert = message.ObsoletionTime.HasValue ?
                    this.m_dataPersistenceService.Delete(message.Key.Value, TransactionMode.Commit, AuthenticationContext.Current.Principal) :
                    this.m_dataPersistenceService.Update(message, TransactionMode.Commit, AuthenticationContext.Current.Principal);

                this.Received?.Invoke(this, new MailMessageEventArgs(alert));
                this.Saved?.Invoke(this, new RepositoryEventArgs<MailMessage>(alert));
            }
            catch (DataPersistenceException)
            {
                alert = this.m_dataPersistenceService.Insert(message, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                this.Received?.Invoke(this, new MailMessageEventArgs(alert));
                this.Saved?.Invoke(this, new RepositoryEventArgs<MailMessage>(alert));
            }

            return alert;
        }

        /// <summary>
        /// Find the specified mail message
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public IQueryResultSet<MailMessage> Find(Expression<Func<MailMessage, bool>> query)
        {
            // Non archived messages
            if (!query.ToString().Contains(nameof(MailMessage.Flags)))
            {
                var parameter = Expression.Parameter(typeof(MailMessage));
                var flagsExpression = Expression.MakeBinary(ExpressionType.NotEqual,
                    Expression.MakeMemberAccess(parameter, typeof(MailMessage).GetProperty(nameof(MailMessage.Flags))),
                    Expression.Constant(MailMessageFlags.Archived));
                query = Expression.Lambda<Func<MailMessage, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, Expression.Invoke(query, parameter), flagsExpression), parameter);
            }
            var results = this.m_dataPersistenceService.Query(query, AuthenticationContext.Current.Principal);
            this.Queried?.Invoke(this, new RepositoryEventArgs<IEnumerable<MailMessage>>(results));
            return results;
        }

        /// <summary>
        /// Get mail message
        /// </summary>
        MailMessage IRepositoryService<MailMessage>.Get(Guid key)
        {
            return this.Get(key);
        }

        /// <summary>
        /// Get a version
        /// </summary>
        MailMessage IRepositoryService<MailMessage>.Get(Guid key, Guid versionKey)
        {
            return this.Get(key);
        }

        /// <summary>
        /// Insert a mail message
        /// </summary>
        MailMessage IRepositoryService<MailMessage>.Insert(MailMessage data)
        {
            return this.Insert(data);
        }

        /// <summary>
        /// OBsolete / delete an alert
        /// </summary>
        MailMessage IRepositoryService<MailMessage>.Delete(Guid key)
        {
            var msg = this.Get(key);
            msg.Flags &= MailMessageFlags.Archived;
            return this.Save(msg);
        }

        /// <summary>
        /// Update mail message
        /// </summary>
        MailMessage IRepositoryService<MailMessage>.Save(MailMessage data)
        {
            return this.Save(data);
        }
    }
}