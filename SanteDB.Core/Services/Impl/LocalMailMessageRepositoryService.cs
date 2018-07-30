﻿/*
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
 * User: fyfej
 * Date: 2017-9-1
 */
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Data;
using MARC.HI.EHRS.SVC.Core.Services;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using SanteDB.Core.Mail;

namespace SanteDB.Core.Services.Impl
{
	/// <summary>
	/// Represents a local alert service.
	/// </summary>
	public class LocalMailMessageRepositoryService : IMailMessageRepositoryService
	{
		/// <summary>
		/// The internal reference to the <see cref="TraceSource"/> instance.
		/// </summary>
		private TraceSource traceSource = new TraceSource("SanteDB.Core");

		/// <summary>
		/// Fired when an alert was raised and is being processed.
		/// </summary>
		public event EventHandler<MailMessageEventArgs> Committed;

		/// <summary>
		/// Fired when an alert is received.
		/// </summary>
		public event EventHandler<MailMessageEventArgs> Received;

		/// <summary>
		/// Broadcasts an alert.
		/// </summary>
		/// <param name="message">The alert message to be broadcast.</param>
		public void Broadcast(MailMessage message)
		{
			this.Committed?.Invoke(this, new MailMessageEventArgs(message));
		}

		/// <summary>
		/// Searches for alerts.
		/// </summary>
		/// <param name="predicate">The predicate to use to search for alerts.</param>
		/// <param name="offset">The offset of the search.</param>
		/// <param name="count">The count of the search results.</param>
		/// <param name="totalCount">The total count of the alerts.</param>
		/// <returns>Returns a list of alerts.</returns>
		public IEnumerable<MailMessage> Find(Expression<Func<MailMessage, bool>> predicate, int offset, int? count, out int totalCount)
		{
			var persistenceService = ApplicationContext.Current.GetService<IDataPersistenceService<MailMessage>>();

			if (persistenceService == null)
			{
				throw new InvalidOperationException(string.Format("{0} not found", nameof(IDataPersistenceService<MailMessage>)));
			}

			return persistenceService.Query(predicate, offset, count, AuthenticationContext.Current.Principal, out totalCount);
		}

		/// <summary>
		/// Gets an alert.
		/// </summary>
		/// <param name="id">The id of the alert to be retrieved.</param>
		/// <returns>Returns an alert.</returns>
		public MailMessage Get(Guid id)
		{
			var persistenceService = ApplicationContext.Current.GetService<IDataPersistenceService<MailMessage>>();

			if (persistenceService == null)
			{
				throw new InvalidOperationException(string.Format("{0} not found", nameof(IDataPersistenceService<MailMessage>)));
			}

			return persistenceService.Get<Guid>(new Identifier<Guid>(id), AuthenticationContext.Current.Principal, false);
		}

		/// <summary>
		/// Inserts an alert message.
		/// </summary>
		/// <param name="message">The alert message to be inserted.</param>
		/// <returns>Returns the inserted alert.</returns>
		public MailMessage Insert(MailMessage message)
		{
			var persistenceService = ApplicationContext.Current.GetService<IDataPersistenceService<MailMessage>>();

			if (persistenceService == null)
			{
				throw new InvalidOperationException(string.Format("{0} not found", nameof(IDataPersistenceService<MailMessage>)));
			}

			MailMessage alert;

			try
			{
				alert = persistenceService.Insert(message, AuthenticationContext.Current.Principal, TransactionMode.Commit);
				this.Received?.Invoke(this, new MailMessageEventArgs(alert));
			}
			catch (Exception e)
			{
#if DEBUG
				this.traceSource.TraceEvent(TraceEventType.Error, 0, e.StackTrace);
#endif
				this.traceSource.TraceEvent(TraceEventType.Error, 0, e.Message);

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
			var persistenceService = ApplicationContext.Current.GetService<IDataPersistenceService<MailMessage>>();

			if (persistenceService == null)
			{
				throw new InvalidOperationException($"{nameof(IDataPersistenceService<MailMessage>)} not found");
			}

			MailMessage alert;

			try
			{
				// obsolete the alert
				alert = message.ObsoletionTime.HasValue ? 
					persistenceService.Obsolete(message, AuthenticationContext.Current.Principal, TransactionMode.Commit) : 
					persistenceService.Update(message, AuthenticationContext.Current.Principal, TransactionMode.Commit);

				this.Received?.Invoke(this, new MailMessageEventArgs(alert));
			}
			catch (DataPersistenceException)
			{
				alert = persistenceService.Insert(message, AuthenticationContext.Current.Principal, TransactionMode.Commit);
				this.Received?.Invoke(this, new MailMessageEventArgs(alert));
			}

			return alert;
		}
	}
}