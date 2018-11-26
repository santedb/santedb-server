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
using SanteDB.Core.Event;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Reporting.PSQL.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Principal;

namespace SanteDB.Persistence.Reporting.PSQL.Services
{
    /// <summary>
    /// Represents a base class for report persistence.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public abstract class ReportPersistenceServiceBase<TModel> : IDataPersistenceService<TModel> where TModel : IdentifiedData
	{
		/// <summary>
		/// The internal reference to the <see cref="TraceSource"/> instance.
		/// </summary>
		protected readonly TraceSource traceSource = new TraceSource(ReportingPersistenceConstants.TraceName);

		/// <summary>
		/// Fired after a data type is inserted.
		/// </summary>
		public event EventHandler<DataPersistedEventArgs<TModel>> Inserted;

		/// <summary>
		/// Fired while a data type is being inserted.
		/// </summary>
		public event EventHandler<DataPersistingEventArgs<TModel>> Inserting;

		/// <summary>
		/// Fired after a data type is obsoleted.
		/// </summary>
		public event EventHandler<DataPersistedEventArgs<TModel>> Obsoleted;

		/// <summary>
		/// Fired while a data type is being obsoleted.
		/// </summary>
		public event EventHandler<DataPersistingEventArgs<TModel>> Obsoleting;

		/// <summary>
		/// Fired after a data type is queried.
		/// </summary>
		public event EventHandler<QueryResultEventArgs<TModel>> Queried;

		/// <summary>
		/// Fired while a data type is being queried.
		/// </summary>
		public event EventHandler<QueryRequestEventArgs<TModel>> Querying;

		/// <summary>
		/// Fired after a data type is been retrieved.
		/// </summary>
		public event EventHandler<DataRetrievedEventArgs<TModel>> Retrieved;

		/// <summary>
		/// Fired while a data type is being retrieved.
		/// </summary>
		public event EventHandler<DataRetrievingEventArgs<TModel>> Retrieving;

		/// <summary>
		/// Fired after a data type is updated.
		/// </summary>
		public event EventHandler<DataPersistedEventArgs<TModel>> Updated;

		/// <summary>
		/// Fired while a data type is being updated.
		/// </summary>
		public event EventHandler<DataPersistingEventArgs<TModel>> Updating;

		/// <summary>
		/// Gets the configuration.
		/// </summary>
		/// <value>The configuration.</value>
		protected static ReportingConfiguration Configuration => ReportingPersistenceService.Configuration;

		/// <summary>
		/// Gets the model mapper.
		/// </summary>
		protected static ModelMapper ModelMapper => ReportingPersistenceService.ModelMapper;

		/// <summary>
		/// Counts the specified query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="authContext">The authentication context.</param>
		/// <returns>System.Int32.</returns>
		/// <exception cref="System.NotImplementedException"></exception>
		public long Count(Expression<Func<TModel, bool>> query, IPrincipal overrideAuthContext = null)
		{
			int totalResults;
			this.QueryInternal(query, 0, 0, out totalResults, true, overrideAuthContext);
			return totalResults;
		}

		/// <summary>
		/// Converts a model instance to a domain instance.
		/// </summary>
		/// <param name="modelInstance">The model instance to convert.</param>
		/// <param name="context">The context.</param>
		/// <param name="principal">The principal.</param>
		/// <returns>Returns the converted model instance.</returns>
		public abstract object FromModelInstance(TModel modelInstance, DataContext context);

		/// <summary>
		/// Gets the specified container identifier.
		/// </summary>
		/// <typeparam name="TIdentifier">The type of the t identifier.</typeparam>
		/// <param name="containerId">The container identifier.</param>
		/// <param name="principal">The principal.</param>
		/// <param name="loadFast">if set to <c>true</c> [load fast].</param>
		/// <returns>Returns the model instance.</returns>
		public TModel Get(Guid containerId, Guid? containerVersion, bool loadFast = false, IPrincipal overrideAuthContext = null)
		{
			var preRetrievalArgs = new DataRetrievingEventArgs<TModel>(containerId, null, overrideAuthContext);

			this.Retrieving?.Invoke(this, preRetrievalArgs);

			if (preRetrievalArgs.Cancel)
			{
				this.traceSource.TraceEvent(TraceEventType.Warning, 0, $"Pre-event handler indicates abort retrieve: {containerId}");
				return null;
			}

			using (var connection = Configuration.Provider.GetReadonlyConnection())
			{
				try
				{
					connection.Open();
					this.traceSource.TraceEvent(TraceEventType.Verbose, 0, $"GET: {containerId}");

					var result = this.Get(connection, containerId, loadFast);

					var postRetrievalEventArgs = new DataRetrievedEventArgs<TModel>(result, overrideAuthContext);

					this.Retrieved?.Invoke(this, postRetrievalEventArgs);

					return result;
				}
				catch (Exception e)
				{
					this.traceSource.TraceEvent(TraceEventType.Error, 0, $"Error: {e}");
					throw;
				}
			}
		}

		/// <summary>
		/// Gets the specified model.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="key">The key.</param>
		/// <param name="principal">The principal.</param>
		/// <param name="loadFast">if set to <c>true</c> [load fast].</param>
		/// <returns>Returns the model instance.</returns>
		public virtual TModel Get(DataContext context, Guid key, bool loadFast = false, IPrincipal overrideAuthContext = null)
		{
			int totalResults;
			return this.QueryInternal(o => o.Key == key, 0, 1, out totalResults, loadFast, overrideAuthContext)?.FirstOrDefault();
		}

		/// <summary>
		/// Inserts the specified storage data.
		/// </summary>
		/// <param name="storageData">The storage data.</param>
		/// <param name="principal">The principal.</param>
		/// <param name="mode">The mode.</param>
		/// <returns>Returns the inserted model instance.</returns>
		public TModel Insert(TModel storageData, TransactionMode mode, IPrincipal overrideAuthContext = null)
		{
			if (storageData == null)
			{
				throw new ArgumentNullException(nameof(storageData), "Value cannot be null");
			}

			var preInsertArgs = new DataPersistingEventArgs<TModel>(storageData, overrideAuthContext);

			this.Inserting?.Invoke(this, preInsertArgs);

			if (preInsertArgs.Cancel)
			{
				traceSource.TraceEvent(TraceEventType.Warning, 0, $"Pre-event handler indicates abort insert for : {storageData}");
				return storageData;
			}

			using (var connection = Configuration.Provider.GetWriteConnection())
			{
				connection.Open();

				using (var transaction = connection.BeginTransaction())
				{
					try
					{
						var existing = this.Get(connection, storageData.Key.Value, false, overrideAuthContext);

						if (existing != null)
						{
							throw new DuplicateNameException(storageData.Key?.ToString());
						}
						else
						{
							this.traceSource.TraceEvent(TraceEventType.Verbose, 0, "INSERT {0}", storageData);
							storageData = this.Insert(connection, storageData);
						}

						if (mode == TransactionMode.Commit)
						{
							transaction.Commit();
						}
						else
						{
							transaction.Rollback();
						}

						this.Inserted?.Invoke(this, new DataPersistedEventArgs<TModel>(storageData, overrideAuthContext));

						return storageData;
					}
					catch (Exception e)
					{
						this.traceSource.TraceEvent(TraceEventType.Error, 0, "Error : {0}", e);
						transaction?.Rollback();
						throw new DataPersistenceException(e.Message, e);
					}
				}
			}
		}

		/// <summary>
		/// Inserts the specified storage data.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="data">The data.</param>
		/// <param name="principal">The principal.</param>
		/// <returns>Returns the inserted model instance.</returns>
		public TModel Insert(DataContext context, TModel data)
		{
			return this.InsertInternal(context, data);
		}

		/// <summary>
		/// Inserts the model.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="model">The model.</param>
		/// <param name="principal">The principal.</param>
		/// <returns>Returns the inserted model.</returns>
		public abstract TModel InsertInternal(DataContext context, TModel model);

		/// <summary>
		/// Obsoletes the specified data.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="model">The model.</param>
		/// <param name="principal">The principal.</param>
		/// <returns>Returns the obsoleted data.</returns>
		public TModel Obsolete(DataContext context, TModel model)
		{
			return this.ObsoleteInternal(context, model);
		}

		/// <summary>
		/// Obsoletes the specified storage data.
		/// </summary>
		/// <param name="storageData">The storage data.</param>
		/// <param name="principal">The principal.</param>
		/// <param name="mode">The mode.</param>
		/// <returns>TModel.</returns>
		public TModel Obsolete(TModel storageData, TransactionMode mode, IPrincipal overrideAuthContext = null)
		{
			if (storageData == null)
			{
				throw new ArgumentNullException(nameof(storageData), "Value cannot be null");
			}

			if (!storageData.Key.HasValue || storageData.Key == Guid.Empty)
			{
				throw new InvalidOperationException("Data missing key");
			}

			var prePersistenceEventArgs = new DataPersistingEventArgs<TModel>(storageData, overrideAuthContext);

			this.Obsoleting?.Invoke(this, prePersistenceEventArgs);

			if (prePersistenceEventArgs.Cancel)
			{
				traceSource.TraceEvent(TraceEventType.Warning, 0, $"Pre-event handler indicates abort for {storageData}");
				return storageData;
			}

			using (var connection = Configuration.Provider.GetWriteConnection())
			{
				connection.Open();

				using (var transaction = connection.BeginTransaction())
				{
					try
					{
						this.traceSource.TraceEvent(TraceEventType.Verbose, 0, $"OBSOLETE {storageData}");

						storageData = this.Obsolete(connection, storageData);

						if (mode == TransactionMode.Commit)
						{
							transaction.Commit();
						}
						else
						{
							transaction.Rollback();
						}

						var args = new DataPersistedEventArgs<TModel>(storageData, overrideAuthContext);

						this.Obsoleted?.Invoke(this, args);

						return storageData;
					}
					catch (Exception e)
					{
						traceSource.TraceEvent(TraceEventType.Error, 0, $"Error obsoleting: {storageData}");
						transaction.Rollback();
						throw new DataPersistenceException(e.Message, e);
					}
				}
			}
		}

		/// <summary>
		/// Obsoletes the specified data.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="model">The model.</param>
		/// <param name="principal">The principal.</param>
		/// <returns>Returns the obsoleted data.</returns>
		public abstract TModel ObsoleteInternal(DataContext context, TModel model);

		/// <summary>
		/// Queries the specified query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="authContext">The authentication context.</param>
		/// <returns>IEnumerable&lt;TModel&gt;.</returns>
		/// <exception cref="System.NotImplementedException"></exception>
		public IEnumerable<TModel> Query(Expression<Func<TModel, bool>> query, IPrincipal overrideAuthContext = null)
		{
			int totalResults;
			return this.QueryInternal(query, 0, null, out totalResults, true, overrideAuthContext);
		}

		/// <summary>
		/// Queries for a model instance.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="count">The count.</param>
		/// <param name="authContext">The authentication context.</param>
		/// <param name="totalCount">The total count.</param>
		/// <returns>Returns a list of model instance which match the given query expression.</returns>
		public IEnumerable<TModel> Query(Expression<Func<TModel, bool>> query, int offset, int? count, out int totalCount, IPrincipal overrideAuthContext = null)
		{
			return this.QueryInternal(query, offset, count, out totalCount, true, overrideAuthContext);
		}

		/// <summary>
		/// Queries for a model instance.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="query">The query.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="count">The count.</param>
		/// <param name="totalResults">The total results.</param>
		/// <param name="countResults">if set to <c>true</c> [count results].</param>
		/// <param name="principal">The principal.</param>
		/// <returns>Returns a list of model instance which match the given query expression.</returns>
		public IEnumerable<TModel> Query(DataContext context, Expression<Func<TModel, bool>> query, int offset, int count, out int totalResults, bool countResults)
		{
			return this.QueryInternal(context, query, offset, count, out totalResults, countResults);
		}

		/// <summary>
		/// Queries for a model instance.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="query">The query.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="count">The count.</param>
		/// <param name="totalResults">The total results.</param>
		/// <param name="countResults">if set to <c>true</c> [count results].</param>
		/// <param name="principal">The principal.</param>
		/// <returns>Returns a list of model instance which match the given query expression.</returns>
		public abstract IEnumerable<TModel> QueryInternal(DataContext context, Expression<Func<TModel, bool>> query, int offset, int? count, out int totalResults, bool countResults);

		/// <summary>
		/// Queries for the specified model.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="count">The count.</param>
		/// <param name="authContext">The authentication context.</param>
		/// <param name="totalCount">The total count.</param>
		/// <param name="fastQuery">if set to <c>true</c> [fast query].</param>
		/// <returns>Returns a list of the specified model instance which match the given query expression.</returns>
		/// <exception cref="System.ArgumentNullException">query</exception>
		/// <exception cref="DataPersistenceException">Cannot perform LINQ query</exception>
		public virtual IEnumerable<TModel> QueryInternal(Expression<Func<TModel, bool>> query, int offset, int? count, out int totalCount, bool fastQuery, IPrincipal overrideAuthContext)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			var preArgs = new QueryRequestEventArgs<TModel>(query, offset, count, null, overrideAuthContext);

			this.Querying?.Invoke(this, preArgs);

			if (preArgs.Cancel)
			{
				this.traceSource.TraceEvent(TraceEventType.Warning, 0, "Pre-Event handler indicates abort query {0}", query);
				totalCount = 0;
				return null;
			}

			// Query object
			using (var connection = Configuration.Provider.GetReadonlyConnection())
			{
				try
				{
					connection.Open();

					this.traceSource.TraceEvent(TraceEventType.Verbose, 0, "QUERY {0}", query);

					if ((count ?? 1000) > 25)
						connection.PrepareStatements = true;
					if (fastQuery)
						connection.AddData("loadFast", true);

					var results = this.Query(connection, query, offset, count ?? 1000, out totalCount, true);

					var postData = new QueryResultEventArgs<TModel>(query, results.AsQueryable(), offset, count, totalCount, null, overrideAuthContext);

					this.Queried?.Invoke(this, postData);

					var retVal = postData.Results.AsParallel().ToList();

					this.traceSource.TraceEvent(TraceEventType.Verbose, 0, $"Returning {offset}..{offset + (count ?? 1000)} or {totalCount} results");

					return retVal;
				}
				catch (Exception e)
				{
					this.traceSource.TraceEvent(TraceEventType.Error, 0, $"Error: {e}");
					throw;
				}
			}
		}

		/// <summary>
		/// Converts a domain instance to a model instance.
		/// </summary>
		/// <param name="domainInstance">The domain instance to convert.</param>
		/// <param name="context">The context.</param>
		/// <param name="principal">The principal.</param>
		/// <returns>Returns the converted model instance.</returns>
		public abstract TModel ToModelInstance(object domainInstance, DataContext context);

		/// <summary>
		/// Updates the specified storage data.
		/// </summary>
		/// <param name="storageData">The storage data.</param>
		/// <param name="principal">The principal.</param>
		/// <param name="mode">The mode.</param>
		/// <returns>Returns the updated model instance.</returns>
		public TModel Update(TModel storageData, TransactionMode mode, IPrincipal overrideAuthContext = null)
		{
			if (storageData == null)
			{
				throw new ArgumentNullException(nameof(storageData), "Value cannot be null");
			}

			if (!storageData.Key.HasValue || storageData.Key == Guid.Empty)
			{
				throw new InvalidOperationException("Data missing key");
			}

			var preUpdateArgs = new DataPersistingEventArgs<TModel>(storageData, overrideAuthContext);

			this.Updating?.Invoke(this, preUpdateArgs);

			if (preUpdateArgs.Cancel)
			{
				traceSource.TraceEvent(TraceEventType.Warning, 0, $"Pre-event handler indicates abort update for : {storageData}");
				return storageData;
			}

			using (var connection = Configuration.Provider.GetWriteConnection())
			{
				connection.Open();

				using (var tx = connection.BeginTransaction())
				{
					try
					{
						this.traceSource.TraceEvent(TraceEventType.Verbose, 0, "UPDATE {0}", storageData);

						storageData = this.Update(connection, storageData);

						if (mode == TransactionMode.Commit)
						{
							tx.Commit();
						}
						else
						{
							tx.Rollback();
						}

						this.Updated?.Invoke(this, new DataPersistedEventArgs<TModel>(storageData, overrideAuthContext));

						return storageData;
					}
					catch (Exception e)
					{
						this.traceSource.TraceEvent(TraceEventType.Error, 0, "Error : {0}", e);
						tx?.Rollback();
						throw new DataPersistenceException(e.Message, e);
					}
				}
			}
		}

		/// <summary>
		/// Updates the specified storage data.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="data">The data.</param>
		/// <param name="principal">The principal.</param>
		/// <returns>Returns the updated model instance.</returns>
		public TModel Update(DataContext context, TModel data)
		{
			return this.UpdateInternal(context, data);
		}

		/// <summary>
		/// Updates the specified storage data.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="model">The model.</param>
		/// <param name="principal">The principal.</param>
		/// <returns>Returns the updated model instance.</returns>
		public abstract TModel UpdateInternal(DataContext context, TModel model);

		/// <summary>
		/// Converts an object to a byte array.
		/// </summary>
		/// <param name="content">The object to convert.</param>
		/// <returns>Returns the converted byte array.</returns>
		protected virtual byte[] ToByteArray(object content)
		{
			byte[] value = null;

			var binaryFormatter = new BinaryFormatter();

			using (var memoryStream = new MemoryStream())
			{
				binaryFormatter.Serialize(memoryStream, content);
				value = memoryStream.ToArray();
			}

			return value;
		}

		/// <summary>
		/// Converts a byte array to an object.
		/// </summary>
		/// <param name="content">The byte array to convert.</param>
		/// <returns>Returns the converted object.</returns>
		protected virtual object ToObject(byte[] content)
		{
			object value = null;

			var binaryFormatter = new BinaryFormatter();

			using (var memoryStream = new MemoryStream(content))
			{
				value = binaryFormatter.Deserialize(memoryStream);
			}

			return value;
		}
	}
}