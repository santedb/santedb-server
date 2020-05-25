/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Represents a local alert service.
    /// </summary>
    [ServiceProvider("Local Mail Message")]
    public class LocalMailMessageRepository : IMailMessageRepositoryService,
        IRepositoryService<MailMessage>
	{
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Local Mail Message Repository";

        /// <summary>
        /// The internal reference to the <see cref="TraceSource"/> instance.
        /// </summary>
        private Tracer traceSource = new Tracer(SanteDBConstants.ServiceTraceSourceName);

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
		public IEnumerable<MailMessage> Find(Expression<Func<MailMessage, bool>> predicate, int offset, int? count, out int totalCount, params ModelSort<MailMessage>[] orderBy)
		{
			var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<MailMessage>>();

			if (persistenceService == null)
			{
				throw new InvalidOperationException(string.Format("{0} not found", nameof(IDataPersistenceService<MailMessage>)));
			}

            // Non archived messages
            var qry = new NameValueCollection(QueryExpressionBuilder.BuildQuery(predicate).ToArray());
            if (!qry.ContainsKey("flags"))
                qry.Add("flags", $"!{(int)MailMessageFlags.Archived}");
			var retVal =  persistenceService.Query(QueryExpressionParser.BuildLinqExpression<MailMessage>(qry), offset, count,  out totalCount, AuthenticationContext.Current.Principal, orderBy);
            this.Queried?.Invoke(this, new RepositoryEventArgs<IEnumerable<MailMessage>>(retVal));
            return retVal;
		}

		/// <summary>
		/// Gets an alert.
		/// </summary>
		/// <param name="id">The id of the alert to be retrieved.</param>
		/// <returns>Returns an alert.</returns>
		public MailMessage Get(Guid id)
		{
			var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<MailMessage>>();

			if (persistenceService == null)
			{
				throw new InvalidOperationException(string.Format("{0} not found", nameof(IDataPersistenceService<MailMessage>)));
			}

			var retVal = persistenceService.Get(id, null, false, AuthenticationContext.Current.Principal);
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
			var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<MailMessage>>();
            var securityService = ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>();
            var roleService = ApplicationServiceContext.Current.GetService<IRoleProviderService>();

            if (String.IsNullOrEmpty(message.To) || string.IsNullOrEmpty(message.From))
                throw new InvalidOperationException("Mail messages must of TO and FROM fields");
            if (persistenceService == null)
			{
				throw new InvalidOperationException(string.Format("{0} not found", nameof(IDataPersistenceService<MailMessage>)));
			}

			MailMessage alert;

			try
			{
                // Clear the RCPT TO and set via TO
                message.RcptTo.Clear();
                foreach(var rcp in message.To.Split(';').Select(o=>o.ToLower()).Distinct())
                {
                    var usr = securityService.GetUser(rcp);
                    if (usr == null) // Group?
                    {
                        var rol = roleService.FindUsersInRole(rcp);
                        foreach (var mem in rol)
                        {
                            usr = securityService.GetUser(mem);
                            if (usr != null) message.RcptTo.Add(usr);
                        }
                    }
                    else
                        message.RcptTo.Add(usr);
                }

                if (message.TimeStamp == default(DateTimeOffset))
                    message.TimeStamp = DateTimeOffset.Now;

				alert = persistenceService.Insert(message, TransactionMode.Commit, AuthenticationContext.Current.Principal);
				this.Received?.Invoke(this, new MailMessageEventArgs(alert));
                this.Inserted?.Invoke(this, new RepositoryEventArgs<MailMessage>(alert));
			}
			catch (Exception e)
			{
#if DEBUG
				this.traceSource.TraceEvent(EventLevel.Error,  e.StackTrace);
#endif
				this.traceSource.TraceEvent(EventLevel.Error,  e.Message);

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
			var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<MailMessage>>();

			if (persistenceService == null)
			{
				throw new InvalidOperationException($"{nameof(IDataPersistenceService<MailMessage>)} not found");
			}

			MailMessage alert;

			try
			{
				// obsolete the alert
				alert = message.ObsoletionTime.HasValue ? 
					persistenceService.Obsolete(message, TransactionMode.Commit, AuthenticationContext.Current.Principal) : 
					persistenceService.Update(message, TransactionMode.Commit, AuthenticationContext.Current.Principal);

				this.Received?.Invoke(this, new MailMessageEventArgs(alert));
                this.Saved?.Invoke(this, new RepositoryEventArgs<MailMessage>(alert));
			}
			catch (DataPersistenceException)
			{
				alert = persistenceService.Insert(message, TransactionMode.Commit, AuthenticationContext.Current.Principal);
				this.Received?.Invoke(this, new MailMessageEventArgs(alert));
                this.Saved?.Invoke(this, new RepositoryEventArgs<MailMessage>(alert));
            }

            return alert;
		}

        /// <summary>
        /// Find the specified mail message
        /// </summary>
        IEnumerable<MailMessage> IRepositoryService<MailMessage>.Find(Expression<Func<MailMessage, bool>> query)
        {
            int tr = 0;
            return this.Find(query, 0, 100, out tr);
        }

        /// <summary>
        /// Find with restrictions
        /// </summary>
        IEnumerable<MailMessage> IRepositoryService<MailMessage>.Find(Expression<Func<MailMessage, bool>> query, int offset, int? count, out int totalResults, params ModelSort<MailMessage>[] orderBy)
        {
            return this.Find(query, offset, count, out totalResults, orderBy);
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
        MailMessage IRepositoryService<MailMessage>.Obsolete(Guid key)
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