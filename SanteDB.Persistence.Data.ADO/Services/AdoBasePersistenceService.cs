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
using SanteDB.Core;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Event;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Configuration;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Services.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Diagnostics;
using System.Diagnostics.Tracing;
using SanteDB.Core.Model.Serialization;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Represents a data persistence service which stores data in the local SQLite data store
    /// </summary>
    [ServiceProvider("ADO.NET Generic Persistence Provider")]
    public abstract class AdoBasePersistenceService<TData> : 
        IDataPersistenceService<TData>, 
        IStoredQueryDataPersistenceService<TData>, 
        IFastQueryDataPersistenceService<TData>,
        IUnionQueryDataPersistenceService<TData>,
        IAdoPersistenceService
    where TData : IdentifiedData
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => $"ADO.NET Data Persistence Service for {typeof(TData).FullName}";

        // Current requests
        private static long m_currentRequests = 0;

        // Get the ado persistence service
        protected AdoPersistenceService m_persistenceService;

        // Lock for editing 
        protected object m_synkLock = new object();

        // Get tracer
        protected Tracer m_tracer = new Tracer(AdoDataConstants.TraceSourceName);

        // Configuration
        protected static AdoPersistenceConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AdoPersistenceConfigurationSection>();

        // Mapper
        protected ModelMapper m_mapper;

        /// <summary>
        /// ADO Base persistence service
        /// </summary>
        public AdoBasePersistenceService()
        {
            this.m_persistenceService = ApplicationServiceContext.Current.GetService<AdoPersistenceService>();
            this.m_mapper = this.m_persistenceService.GetMapper();
        }

        public event EventHandler<DataPersistingEventArgs<TData>> Inserting;
        public event EventHandler<DataPersistedEventArgs<TData>> Inserted;
        public event EventHandler<DataPersistingEventArgs<TData>> Updating;
        public event EventHandler<DataPersistedEventArgs<TData>> Updated;
        public event EventHandler<DataPersistingEventArgs<TData>> Obsoleting;
        public event EventHandler<DataPersistedEventArgs<TData>> Obsoleted;
        public event EventHandler<DataRetrievingEventArgs<TData>> Retrieving;
        public event EventHandler<DataRetrievedEventArgs<TData>> Retrieved;
        public event EventHandler<QueryRequestEventArgs<TData>> Querying;
        public event EventHandler<QueryResultEventArgs<TData>> Queried;

        /// <summary>
        /// Maps the data to a model instance
        /// </summary>
        /// <returns>The model instance.</returns>
        /// <param name="dataInstance">Data instance.</param>
        public abstract TData ToModelInstance(Object dataInstance, DataContext context);

        /// <summary>
        /// Froms the model instance.
        /// </summary>
        /// <returns>The model instance.</returns>
        /// <param name="modelInstance">Model instance.</param>
        public abstract Object FromModelInstance(TData modelInstance, DataContext context);

        /// <summary>
        /// Performthe actual insert.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public abstract TData InsertInternal(DataContext context, TData data);

        /// <summary>
        /// Perform the actual update.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public abstract TData UpdateInternal(DataContext context, TData data);

        /// <summary>
        /// Performs the actual obsoletion
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public abstract TData ObsoleteInternal(DataContext context, TData data);

        /// <summary>
        /// Performs the actual query
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="query">Query.</param>
        public abstract IEnumerable<TData> QueryInternal(DataContext context, Expression<Func<TData, bool>> query, Guid queryId, int offset, int? count, out int totalResults, ModelSort<TData>[] orderBy, bool countResults = true);

        /// <summary>
        /// Get the specified key.
        /// </summary>
        /// <param name="key">Key.</param>
        internal virtual TData Get(DataContext context, Guid key)
        {
            int tr = 0;
            var cacheService = new AdoPersistenceCache(context);

            var cacheItem = cacheService?.GetCacheItem<TData>(key);
            if (cacheItem != null)
            {
                if (cacheItem.LoadState < context.LoadState)
                {
                    cacheItem.LoadAssociations(context);
                    cacheService?.Add(cacheItem);
                }
                return cacheItem;
            }
            else
            {
                cacheItem = this.QueryInternal(context, o => o.Key == key, Guid.Empty, 0, 1, out tr, null, countResults: false)?.FirstOrDefault();
                if (cacheService != null)
                    cacheService.Add(cacheItem);
                return cacheItem;
            }
        }

        /// <summary>
        /// Inserts the specified data
        /// </summary>
        public TData Insert(TData data, TransactionMode mode, IPrincipal overrideAuthContext)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            DataPersistingEventArgs<TData> preArgs = new DataPersistingEventArgs<TData>(data, overrideAuthContext);
            this.Inserting?.Invoke(this, preArgs);
            if (preArgs.Cancel)
            {
                this.m_tracer.TraceEvent(EventLevel.Warning, "Pre-Event handler indicates abort insert for {0}", data);
                return data;
            }

            // Persist object
            using (var connection = m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    this.ThrowIfExceeded();

                    connection.Open();
                    using (IDbTransaction tx = connection.BeginTransaction())
                        try
                        {

                            // Disable inserting duplicate classified objects
                            connection.EstablishProvenance(overrideAuthContext, (data as BaseEntityData)?.CreatedByKey);
                            var existing = data.TryGetExisting(connection, true);
                            if (existing != null)
                            {
                                if (m_configuration.AutoUpdateExisting)
                                {
                                    this.m_tracer.TraceEvent(EventLevel.Warning, "INSERT WOULD RESULT IN DUPLICATE CLASSIFIER: UPDATING INSTEAD {0}", data);
                                    data.Key = existing.Key;
                                    data = this.Update(connection, data);
                                }
                                else
                                    throw new DuplicateNameException(data.Key?.ToString());
                            }
                            else
                            {
                                this.m_tracer.TraceEvent(EventLevel.Verbose, "INSERT {0}", data);
                                data = this.Insert(connection, data);
                            }
                            data.LoadState = LoadState.FullLoad; // We just persisted so it is fully loaded

                            if (mode == TransactionMode.Commit)
                            {
                                tx.Commit();
                                foreach (var itm in connection.CacheOnCommit)
                                    ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Add(itm);
                            }
                            else
                                tx.Rollback();

                            var args = new DataPersistedEventArgs<TData>(data, overrideAuthContext);

                            this.Inserted?.Invoke(this, args);

                            return data;

                        }
                        catch (DbException e)
                        {

#if DEBUG
                            this.m_tracer.TraceEvent(EventLevel.Error,  "Error : {0} -- {1}", e, this.ObjectToString(data));
#else
                            this.m_tracer.TraceEvent(EventLevel.Error,  "Error : {0}", e.Message);
#endif
                            tx?.Rollback();

                            this.TranslateDbException(e);
                            throw;
                        }
                        catch (Exception e)
                        {
                            this.m_tracer.TraceEvent(EventLevel.Error,  "Error : {0} -- {1}", e, this.ObjectToString(data));

                            tx?.Rollback();
                            throw new DataPersistenceException(e.Message, e);
                        }
                        finally
                        {
                        }
                }
                finally
                {
                    Interlocked.Decrement(ref m_currentRequests);
                }
            }
        }

        /// <summary>
        /// Throw if requests are greater than maximum allowed
        /// </summary>
        private void ThrowIfExceeded()
        {
            if (this.m_persistenceService.GetConfiguration().MaxRequests == 0 ||
                Interlocked.Read(ref m_currentRequests) < this.m_persistenceService.GetConfiguration().MaxRequests)
                Interlocked.Increment(ref m_currentRequests);
            else
                throw new LimitExceededException("Data layer restricted maximum system requests");
        }

        /// <summary>
        /// Update the specified object
        /// </summary>
        /// <param name="storageData"></param>
        /// <param name="principal"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public TData Update(TData data, TransactionMode mode, IPrincipal overrideAuthContext)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            else if (data.Key == Guid.Empty)
                throw new InvalidOperationException("Data missing key");

            DataPersistingEventArgs<TData> preArgs = new DataPersistingEventArgs<TData>(data, overrideAuthContext);
            this.Updating?.Invoke(this, preArgs);
            if (preArgs.Cancel)
            {
                this.m_tracer.TraceEvent(EventLevel.Warning, "Pre-Event handler indicates abort update for {0}", data);
                return data;
            }

            // Persist object
            using (var connection = m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    this.ThrowIfExceeded();

                    connection.Open();
                    using (IDbTransaction tx = connection.BeginTransaction())
                        try
                        {
                            //connection.Connection.Open();

                            this.m_tracer.TraceEvent(EventLevel.Verbose, "UPDATE {0}", data);

                            connection.EstablishProvenance(overrideAuthContext, (data as NonVersionedEntityData)?.UpdatedByKey ?? (data as BaseEntityData)?.CreatedByKey);
                            data = Update(connection, data);
                            data.LoadState = LoadState.FullLoad; // We just persisted this so it is fully loaded

                            if (mode == TransactionMode.Commit)
                            {
                                tx.Commit();
                                foreach (var itm in connection.CacheOnCommit)
                                    ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Add(itm);

                            }
                            else
                                tx.Rollback();

                            var args = new DataPersistedEventArgs<TData>(data, overrideAuthContext);

                            this.Updated?.Invoke(this, args);

                            return data;
                        }
                        catch (DbException e)
                        {

#if DEBUG
                            this.m_tracer.TraceEvent(EventLevel.Error,  "Error : {0} -- {1}", e, this.ObjectToString(data));
#else
                            this.m_tracer.TraceEvent(EventLevel.Error,  "Error : {0}", e.Message);
#endif
                            tx?.Rollback();

                            this.TranslateDbException(e);
                            throw new DataPersistenceException($"Error updating {data}", e);
                        }
                        catch (Exception e)
                        {

#if DEBUG
                            this.m_tracer.TraceEvent(EventLevel.Error,  "Error : {0} -- {1}", e, this.ObjectToString(data));
#else
                        this.m_tracer.TraceEvent(EventLevel.Error,  "Error : {0}", e.Message);
#endif
                            tx?.Rollback();

                            // if the exception is key not found, we want the caller to know
                            // so that a potential insert can take place
                            if (e is KeyNotFoundException)
                            {
                                throw new KeyNotFoundException($"Record {data} was not found for update", e);
                            }

                            // if the exception is anything else, we want to throw a data persistence exception
                            throw new DataPersistenceException($"Error updating {data}", e);

                        }
                        finally
                        {
                        }
                }
                finally
                {
                    Interlocked.Decrement(ref m_currentRequests);
                }
            }
        }

        
        /// <summary>
        /// Translates a DB exception to an appropriate SanteDB exception
        /// </summary>
        protected void TranslateDbException(DbException e)
        {
            if (e.Data["SqlState"] != null)
            {
                switch (e.Data["SqlState"].ToString())
                {
                    case "O9001": // SanteDB => Data Validation Error
                        throw new DetectedIssueException(
                            new DetectedIssue(DetectedIssuePriorityType.Error, e.Data["SqlState"].ToString(), e.Message, DetectedIssueKeys.InvalidDataIssue));
                    case "O9002": // SanteDB => Codification error
                        throw new DetectedIssueException(new List<DetectedIssue>() {
                                        new DetectedIssue(DetectedIssuePriorityType.Error, e.Data["SqlState"].ToString(),  e.Message, DetectedIssueKeys.CodificationIssue),
                                        new DetectedIssue(DetectedIssuePriorityType.Information, e.Data["SqlState"].ToString(), "HINT: Select a code that is from the correct concept set or add the selected code to the concept set", DetectedIssueKeys.CodificationIssue)
                                    });
                    case "23502": // PGSQL - NOT NULL 
                        throw new DetectedIssueException(
                                        new DetectedIssue(DetectedIssuePriorityType.Error, e.Data["SqlState"].ToString(), e.Message, DetectedIssueKeys.InvalidDataIssue)
                                    );
                    case "23503": // PGSQL - FK VIOLATION
                        throw new DetectedIssueException(
                                        new DetectedIssue(DetectedIssuePriorityType.Error, e.Data["SqlState"].ToString(), e.Message, DetectedIssueKeys.FormalConstraintIssue)
                                    );
                    case "23505": // PGSQL - UQ VIOLATION
                        throw new DetectedIssueException(
                                        new DetectedIssue(DetectedIssuePriorityType.Error, e.Data["SqlState"].ToString(), e.Message, DetectedIssueKeys.AlreadyDoneIssue)
                                    );
                    case "23514": // PGSQL - CK VIOLATION
                        throw new DetectedIssueException(new List<DetectedIssue>()
                        {
                            new DetectedIssue(DetectedIssuePriorityType.Error, e.Data["SqlState"].ToString(), e.Message, DetectedIssueKeys.FormalConstraintIssue),
                            new DetectedIssue(DetectedIssuePriorityType.Information, e.Data["SqlState"].ToString(), "HINT: The code you're using may be incorrect for the given context", DetectedIssueKeys.CodificationIssue)
                        });
                    default:
                        throw new DataPersistenceException(e.Message, e);
                }
            }
            else
            {
                throw new DetectedIssueException(new DetectedIssue(DetectedIssuePriorityType.Error, "dbexception", e.Message, DetectedIssueKeys.OtherIssue));
            }
        }

        /// <summary>
        /// Convert object to string
        /// </summary>
        private String ObjectToString(TData data)
        {
            if (data == null) return "null";
            IEnumerable<Type> extraTypes = new Type[] { typeof(TData) };
            if (data is Bundle)
                extraTypes = extraTypes.Union((data as Bundle).Item.Select(o => o.GetType()));

            XmlSerializer xsz = XmlModelSerializerFactory.Current.CreateSerializer(data.GetType(), extraTypes.ToArray());
            using (MemoryStream ms = new MemoryStream())
            {
                xsz.Serialize(ms, data);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// Obsoletes the specified object
        /// </summary>
        public TData Obsolete(TData data, TransactionMode mode, IPrincipal overrideAuthContext)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            else if (data.Key == Guid.Empty)
                throw new InvalidOperationException("Data missing key");

            DataPersistingEventArgs<TData> preArgs = new DataPersistingEventArgs<TData>(data, overrideAuthContext);
            this.Obsoleting?.Invoke(this, preArgs);
            if (preArgs.Cancel)
            {
                this.m_tracer.TraceEvent(EventLevel.Warning, "Pre-Event handler indicates abort for {0}", data);
                return data;
            }

            // Obsolete object
            using (var connection = m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    this.ThrowIfExceeded();

                    connection.Open();
                    using (IDbTransaction tx = connection.BeginTransaction())
                        try
                        {
                            //connection.Connection.Open();

                            this.m_tracer.TraceEvent(EventLevel.Verbose, "OBSOLETE {0}", data);
                            connection.EstablishProvenance(overrideAuthContext, (data as BaseEntityData)?.ObsoletedByKey);
                            data = this.Obsolete(connection, data);

                            if (mode == TransactionMode.Commit)
                            {
                                tx.Commit();
                                foreach (var itm in connection.CacheOnCommit)
                                    ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Remove(itm.Key.Value);
                            }
                            else
                                tx.Rollback();

                            var args = new DataPersistedEventArgs<TData>(data, overrideAuthContext);

                            this.Obsoleted?.Invoke(this, args);

                            return data;
                        }
                        catch (Exception e)
                        {
                            this.m_tracer.TraceEvent(EventLevel.Error,  "Error : {0}", e);
                            tx?.Rollback();
                            throw new DataPersistenceException(e.Message, e);
                        }
                        finally
                        {
                        }
                }
                finally
                {
                    Interlocked.Decrement(ref m_currentRequests);
                }
            }
        }

        /// <summary>
        /// Gets the specified object
        /// </summary>
        public virtual TData Get(Guid containerId, Guid? versionId, bool loadFast, IPrincipal overrideAuthContext)
        {

            var cacheItem = ApplicationServiceContext.Current.GetService<IDataCachingService>()?.GetCacheItem<TData>(containerId) as TData;
            if (loadFast && cacheItem != null && 
                versionId == Guid.Empty)
            {
                return cacheItem;
            }
            else
            {

#if DEBUG
                Stopwatch sw = new Stopwatch();
                sw.Start();
#endif

                DataRetrievingEventArgs<TData> preArgs = new DataRetrievingEventArgs<TData>(containerId, versionId, overrideAuthContext);
                this.Retrieving?.Invoke(this, preArgs);
                if (preArgs.Cancel)
                {
                    this.m_tracer.TraceEvent(EventLevel.Warning, "Pre-Event handler indicates abort retrieve {0}", containerId);
                    return preArgs.Result;
                }

                // Query object
                using (var connection = m_configuration.Provider.GetReadonlyConnection())
                    try
                    {
                        this.ThrowIfExceeded();
                        connection.Open();
                        this.m_tracer.TraceEvent(EventLevel.Verbose, "GET {0}", containerId);

                        if (loadFast)
                        {
                            connection.AddData("loadFast", true);
                            connection.LoadState = LoadState.PartialLoad;
                        }
                        else
                            connection.LoadState = LoadState.FullLoad;

                        var result = this.Get(connection, containerId);
                        var postData = new DataRetrievedEventArgs<TData>(result, overrideAuthContext);
                        this.Retrieved?.Invoke(this, postData);

                        foreach (var itm in connection.CacheOnCommit)
                            ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Add(itm);

                        return result;

                    }
                    catch (NotSupportedException e)
                    {
                        throw new DataPersistenceException("Cannot perform LINQ query", e);
                    }
                    catch (Exception e)
                    {
                        this.m_tracer.TraceEvent(EventLevel.Error,  "Error : {0}", e);
                        throw;
                    }
                    finally
                    {
#if DEBUG
                        sw.Stop();
                        this.m_tracer.TraceEvent(EventLevel.Verbose, "Retrieve took {0} ms", sw.ElapsedMilliseconds);
#endif
                        Interlocked.Decrement(ref m_currentRequests);
                    }
            }
        }

        /// <summary>
        /// Performs the specified query
        /// </summary>
        public long Count(Expression<Func<TData, bool>> query, IPrincipal overrideAuthContext)
        {
            var tr = 0;
            this.Query(query, 0, 0, out tr, overrideAuthContext);
            return tr;
        }

        /// <summary>
        /// Performs query returning all results
        /// </summary>
        public virtual IEnumerable<TData> Query(Expression<Func<TData, bool>> query, IPrincipal overrideAuthContext)
        {
            var tr = 0;
            return this.QueryInternal(query, Guid.Empty, 0, null, out tr, true, overrideAuthContext, null, null);

        }

        /// <summary>
        /// Performs the specified query
        /// </summary>
        public virtual IEnumerable<TData> Query(Expression<Func<TData, bool>> query, int offset, int? count, out int totalCount, IPrincipal overrideAuthContext, params ModelSort<TData>[] orderBy)
        {
            return this.QueryInternal(query, Guid.Empty, offset, count, out totalCount, false, overrideAuthContext, orderBy, null);
        }

        /// <summary>
        /// Instructs the service 
        /// </summary>
        protected virtual IEnumerable<TData> QueryInternal(Expression<Func<TData, bool>> query, Guid queryId, int offset, int? count, out int totalCount, bool fastQuery, IPrincipal overrideAuthContext, ModelSort<TData>[] orderBy, Expression<Func<TData, bool>>[] unionWith)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif

            QueryRequestEventArgs<TData> preArgs = new QueryRequestEventArgs<TData>(query, offset, count, queryId, overrideAuthContext);
            this.Querying?.Invoke(this, preArgs);
            if (preArgs.Cancel)
            {
                this.m_tracer.TraceEvent(EventLevel.Warning, "Pre-Event handler indicates abort query {0}", query);
                totalCount = preArgs.TotalResults;
                return preArgs.Results;
            }
            
            // Query object
            using (var connection = m_configuration.Provider.GetReadonlyConnection())
                try
                {
                    this.ThrowIfExceeded();
                    connection.Open();

                    this.m_tracer.TraceEvent(EventLevel.Verbose, "QUERY {0}", query);

                    // Is there an obsoletion item already specified?
                    if ((count ?? 1000) > 25 && this.m_persistenceService.GetConfiguration().PrepareStatements)
                        connection.PrepareStatements = true;
                    if (fastQuery)
                    {
                        connection.AddData("loadFast", true);
                        connection.LoadState = LoadState.PartialLoad;
                    }
                    else
                        connection.LoadState = LoadState.FullLoad;

                    // Other results we want to intersect with?
                    if (unionWith != null)
                        connection.AddData("UNION", unionWith);

                    var results = this.Query(connection, preArgs.Query, queryId, preArgs.Offset, preArgs.Count ?? 1000, out totalCount, orderBy, true);
                    var postData = new QueryResultEventArgs<TData>(query, results.AsQueryable(), offset, count, totalCount, queryId, overrideAuthContext);
                    this.Queried?.Invoke(this, postData);

                    var retVal = postData.Results.ToList();

                    // Add to cache
                    foreach (var i in retVal.Where(i => i != null))
                        connection.AddCacheCommit(i);

                    foreach (var itm in connection.CacheOnCommit)
                        ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Add(itm);

                    this.m_tracer.TraceEvent(EventLevel.Verbose, "Returning {0}..{1} or {2} results", offset, offset + (count ?? 1000), totalCount);

                    return retVal;

                }
                catch (NotSupportedException e)
                {
                    throw new DataPersistenceException("Cannot perform LINQ query", e);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error,  "Error : {0}", e);
                    throw;
                }
                finally
                {
#if DEBUG
                    sw.Stop();
                    this.m_tracer.TraceEvent(EventLevel.Verbose, "Query {0} took {1} ms", query, sw.ElapsedMilliseconds);
#endif
                    Interlocked.Decrement(ref m_currentRequests);
                }
        }


        /// <summary>
        /// Performthe actual insert.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public TData Insert(DataContext context, TData data)
        {
            var retVal = this.InsertInternal(context, data);
            //if (retVal != data) System.Diagnostics.Debugger.Break();
            context.AddCacheCommit(retVal);
            return retVal;
        }
        /// <summary>
        /// Perform the actual update.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public TData Update(DataContext context, TData data)
        {
            //// Make sure we're updating the right thing
            //if (data.Key.HasValue)
            //{
            //    var cacheItem = ApplicationServiceContext.Current.GetService<IDataCachingService>()?.GetCacheItem(data.GetType(), data.Key.Value);
            //    if (cacheItem != null)
            //    {
            //        cacheItem.CopyObjectData(data);
            //        data = cacheItem as TData;
            //    }
            //}

            var retVal = this.UpdateInternal(context, data);
            //if (retVal != data) System.Diagnostics.Debugger.Break();
            context.AddCacheCommit(retVal);
            return retVal;

        }
        /// <summary>
        /// Performs the actual obsoletion
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public TData Obsolete(DataContext context, TData data)
        {
            var retVal = this.ObsoleteInternal(context, data);
            //if (retVal != data) System.Diagnostics.Debugger.Break();
            context.AddCacheCommit(retVal);
            return retVal;
        }
        /// <summary>
        /// Performs the actual query
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="query">Query.</param>
        public IEnumerable<TData> Query(DataContext context, Expression<Func<TData, bool>> query, Guid queryId, int offset, int count, out int totalResults, ModelSort<TData>[] orderBy, bool countResults)
        {
            var retVal = this.QueryInternal(context, query, queryId, offset, count, out totalResults, orderBy, countResults);
            return retVal;
        }

        /// <summary>
        /// Insert the object for generic methods
        /// </summary>
        object IAdoPersistenceService.Insert(DataContext context, object data)
        {
            return this.InsertInternal(context, (TData)data);
        }

        /// <summary>
        /// Update the object for generic methods
        /// </summary>
        object IAdoPersistenceService.Update(DataContext context, object data)
        {
            return this.UpdateInternal(context, (TData)data);
        }

        /// <summary>
        /// Obsolete the object for generic methods
        /// </summary>
        object IAdoPersistenceService.Obsolete(DataContext context, object data)
        {
            return this.ObsoleteInternal(context, (TData)data);
        }

        /// <summary>
        /// Get the specified data
        /// </summary>
        object IAdoPersistenceService.Get(DataContext context, Guid id)
        {
            return this.Get(context, id);
        }

        /// <summary>
        /// Insert the object
        /// </summary>
        object IDataPersistenceService.Insert(object data)
        {
            return this.Insert((TData)data, TransactionMode.Commit, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Update the specified data
        /// </summary>
        object IDataPersistenceService.Update(object data)
        {
            return this.Update((TData)data, TransactionMode.Commit, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Obsolete specified data
        /// </summary>
        object IDataPersistenceService.Obsolete(object data)
        {
            return this.Obsolete((TData)data, TransactionMode.Commit, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Get the specified data
        /// </summary>
        object IDataPersistenceService.Get(Guid id)
        {
            return this.Get(id, Guid.Empty, false, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Generic to model instance for other callers
        /// </summary>
        /// <returns></returns>
        object IAdoPersistenceService.ToModelInstance(object domainInstance, DataContext context)
        {
            return this.ToModelInstance(domainInstance, context);
        }

        /// <summary>
        /// Perform generic query
        /// </summary>
        IEnumerable IDataPersistenceService.Query(Expression query, int offset, int? count, out int totalResults)
        {
            return this.Query((Expression<Func<TData, bool>>)query, offset, count, out totalResults, AuthenticationContext.Current.Principal);
        }


        #region Event Handler Helpers

        /// <summary>
        /// Fire retrieving
        /// </summary>
        protected void FireRetrieving(DataRetrievingEventArgs<TData> e)
        {
            this.Retrieving?.Invoke(this, e);
        }

        /// <summary>
        /// Fire retrieving
        /// </summary>
        protected void FireRetrieved(DataRetrievedEventArgs<TData> e)
        {
            if(e.Data != null)
                this.Retrieved?.Invoke(this, e);
        }

        /// <summary>
        /// Query from the IMS with specified query id
        /// </summary>
        public IEnumerable<TData> Query(Expression<Func<TData, bool>> query, Guid queryId, int offset, int? count, out int totalCount, IPrincipal overrideAuthContext, params ModelSort<TData>[] orderBy)
        {
            return this.QueryInternal(query, queryId, offset, count, out totalCount, false, overrideAuthContext, orderBy, null);

        }

        /// <summary>
        /// Perform a lean query
        /// </summary>
        public IEnumerable<TData> QueryFast(Expression<Func<TData, bool>> query, Guid queryId, int offset, int? count, out int totalCount, IPrincipal overrideAuthContext)
        {
            return this.QueryInternal(query, queryId, offset, count, out totalCount, true, overrideAuthContext, null, null);
        }

        /// <summary>
        /// Intersect the specified queries together
        /// </summary>
        public IEnumerable<TData> Union(Expression<Func<TData, bool>>[] queries, Guid queryId, int offset, int? count, out int totalCount, IPrincipal overrideAuthContext, params ModelSort<TData>[] orderBy)
        {
            return this.QueryInternal(queries.First(), queryId, offset, count, out totalCount, false, overrideAuthContext, orderBy, queries.Skip(1).ToArray());
        }


        #endregion

    }
}

