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
 * Date: 2022-9-7
 */
using SanteDB.Core;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Event;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.OrmLite.MappedResultSets;
using SanteDB.OrmLite.Providers;
using SanteDB.Persistence.Data.Configuration;
using SanteDB.Persistence.Data.Model;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence
{
    /// <summary>
    /// Base persistence services
    /// </summary>
    public abstract class BasePersistenceService<TModel, TDbModel> :
        IDataPersistenceService<TModel>,
        IDataPersistenceServiceEx<TModel>,
        IReportProgressChanged,
        IAdoPersistenceProvider<TModel>,
        IMappedQueryProvider<TModel>,
        IDataPersistenceService,
        IQuerySetProvider
        where TModel : IdentifiedData, new()
        where TDbModel : class, IDbIdentified, new()
    {
        /// <summary>
        /// Get tracer for the specified persistence class
        /// </summary>
        protected readonly Tracer m_tracer = Tracer.GetTracer(typeof(BasePersistenceService<TModel, TDbModel>));

        /// <summary>
        /// Data caching service
        /// </summary>
        protected readonly IDataCachingService m_dataCacheService;

        /// <summary>
        /// Query persistence service
        /// </summary>
        protected readonly IQueryPersistenceService m_queryPersistence;

        /// <summary>
        /// Ad-hoc caching service
        /// </summary>
        protected readonly IAdhocCacheService m_adhocCache;

        /// <summary>
        /// Model mapper
        /// </summary>
        protected readonly ModelMapper m_modelMapper;

        /// <summary>
        /// Configuration reference
        /// </summary>
        protected readonly AdoPersistenceConfigurationSection m_configuration;

        /// <summary>
        /// Localization service
        /// </summary>
        protected readonly ILocalizationService m_localizationService;

        /// <summary>
        /// Base persistence service
        /// </summary>
        public BasePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCaching = null, IQueryPersistenceService queryPersistence = null)
        {
            this.m_dataCacheService = dataCaching;
            this.m_queryPersistence = queryPersistence;
            this.m_adhocCache = adhocCacheService;
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
            this.m_localizationService = localizationService;
            this.Provider = this.m_configuration.Provider;
            this.m_modelMapper = new ModelMapper(typeof(AdoPersistenceService).Assembly.GetManifestResourceStream(DataConstants.MapResourceName), "AdoModelMap");
        }

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => $"SanteDB ADO.NET Persistence for {typeof(TModel).Name}";

        /// <summary>
        /// The provider
        /// </summary>
        public IDbProvider Provider { get; set; }

        /// <summary>
        /// Get the query persistence service
        /// </summary>
        public IQueryPersistenceService QueryPersistence => this.m_queryPersistence;

        /// <summary>
        /// Fired after inserting has completed
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<TModel>> Inserted;

        /// <summary>
        /// Fired when inserting
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<TModel>> Inserting;

        /// <summary>
        /// Fired after updating
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<TModel>> Updated;

        /// <summary>
        /// Fired prior to updating
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<TModel>> Updating;

        /// <summary>
        /// Fired after data has been queried
        /// </summary>
        public event EventHandler<QueryResultEventArgs<TModel>> Queried;

        /// <summary>
        /// Fired prior to data querying
        /// </summary>
        public event EventHandler<QueryRequestEventArgs<TModel>> Querying;

        /// <summary>
        /// Fired when data is being retrieved
        /// </summary>
        public event EventHandler<DataRetrievingEventArgs<TModel>> Retrieving;

        /// <summary>
        /// Fired after data is retrieved
        /// </summary>
        public event EventHandler<DataRetrievedEventArgs<TModel>> Retrieved;

        /// <summary>
        /// Fired after deleted
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<TModel>> Deleted;

        /// <summary>
        /// Fired before delete
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<TModel>> Deleting;
        
        /// <summary>
        /// Fired when progress changes
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Perform the query operation
        /// </summary>
        protected abstract OrmResultSet<TDbModel> DoQueryInternal(DataContext context, Expression<Func<TModel, bool>> query, bool allowCache = false);

        /// <summary>
        /// Perform a query by model object
        /// </summary>
        protected abstract IQueryResultSet<TModel> DoQueryModel(Expression<Func<TModel, bool>> query);

        /// <summary>
        /// Perform an internal get operation
        /// </summary>
        protected abstract TDbModel DoGetInternal(DataContext context, Guid key, Guid? versionKey, bool allowCache = false);

        /// <summary>
        /// Convert to a database model
        /// </summary>
        /// <param name="context">The data context to fetch additional data from</param>
        /// <param name="dbModel">The model to be converted</param>
        /// <param name="referenceObjects">Any other objects (via reference or joins) which may be of use</param>
        protected abstract TModel DoConvertToInformationModel(DataContext context, TDbModel dbModel, params Object[] referenceObjects);

        /// <summary>
        /// Convert to data model
        /// </summary>
        protected abstract TDbModel DoConvertToDataModel(DataContext context, TModel model, params Object[] referenceObjects);

        /// <summary>
        /// Perform the insertion of the <paramref name="model"/>
        /// </summary>
        protected abstract TDbModel DoInsertInternal(DataContext context, TDbModel model);

        /// <summary>
        /// Perform the actual update of information
        /// </summary>
        protected abstract TDbModel DoUpdateInternal(DataContext context, TDbModel model);

        /// <summary>
        /// Perform an obsoletion of the specified object
        /// </summary>
        protected abstract TDbModel DoDeleteInternal(DataContext context, Guid key, DeleteMode deletionMode);

        /// <summary>
        /// Validate the <paramref name="cacheEntry"/> matches the version information in the database from <paramref name="dataModel"/>
        /// </summary>
        /// <param name="cacheEntry">The cache entry</param>
        /// <param name="dataModel">The data model from the database</param>
        /// <returns>True if the <paramref name="dataModel"/> matches <paramref name="cacheEntry"/>, false if the cache entry has been updated since</returns>
        protected abstract bool ValidateCacheItem(TModel cacheEntry, TDbModel dataModel);

        /// <summary>
        /// Perform an obsoletion for all objects matching <paramref name="expression"/>
        /// </summary>
        protected abstract IEnumerable<TDbModel> DoDeleteAllInternal(DataContext context, Expression<Func<TModel, bool>> expression, DeleteMode deleteMode);

        /// <summary>
        /// Called before the object is persisted - this allows implementations to change the object before being persisted
        /// </summary>
        protected abstract TModel BeforePersisting(DataContext context, TModel data);

        /// <summary>
        /// Called after the object is persisted - this allows implementations to change the object before it is returned
        /// </summary>
        protected abstract TModel AfterPersisted(DataContext context, TModel data);

        /// <summary>
        /// Fires the <see cref="ProgressChanged"/> event
        /// </summary>
        /// <param name="status">The status of the progress item</param>
        /// <param name="progress">The progress to be set</param>
        protected virtual void FireProgressChanged(String status, float progress)
        {
            this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(progress, status));
        }

        /// <summary>
        /// Perform the actual insert of a model object
        /// </summary>
        protected virtual TModel DoInsertModel(DataContext context, TModel data)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (data == default(TModel))
            {
                throw new ArgumentNullException(nameof(data), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            data = this.BeforePersisting(context, data);

#if DEBUG
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Start();
#endif
                var dbInstance = this.DoConvertToDataModel(context, data);
                dbInstance = this.DoInsertInternal(context, dbInstance);
                var retVal = this.m_modelMapper.MapDomainInstance<TDbModel, TModel>(dbInstance);
                return this.AfterPersisted(context, retVal); // TODO: Perhaps
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose($"PERFORMANCE: DoInsertModel - {sw.ElapsedMilliseconds}ms");
            }
#endif
        }

        /// <summary>
        /// Perform a touch which updates the modification time
        /// </summary>
        protected virtual TModel DoTouchModel(DataContext context, Guid key)
        {
            throw new NotSupportedException(String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(TDbModel), typeof(DbNonVersionedBaseData)));
        }

        /// <summary>
        /// Perform the actual update of a model object
        /// </summary>
        protected virtual TModel DoUpdateModel(DataContext context, TModel data)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (data == default(TModel))
            {
                throw new ArgumentNullException(nameof(data), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            data = this.BeforePersisting(context, data);

#if DEBUG
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Start();
#endif
                var dbInstance = this.DoConvertToDataModel(context, data);
                dbInstance = this.DoUpdateInternal(context, dbInstance);
                var retVal = this.m_modelMapper.MapDomainInstance<TDbModel, TModel>(dbInstance);
                return this.AfterPersisted(context, retVal);

#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose($"PERFORMANCE: DoUpdateModel - {sw.ElapsedMilliseconds}ms");

            }
#endif
        }

        /// <summary>
        /// Do the deletion all model objects which match
        /// </summary>
        protected virtual void DoDeleteAllModel(DataContext context, Expression<Func<TModel, bool>> expression, DeleteMode deleteMode)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

#if DEBUG
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Start();
#endif
                foreach(var itm in this.DoDeleteAllInternal(context, expression, deleteMode))
                {
                    this.m_dataCacheService?.Remove(this.DoConvertToInformationModel(context, itm));
                }
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose($"PERFORMANCE: DoObsoleteModel - {sw.ElapsedMilliseconds}ms");

            }
#endif
        }

        /// <summary>
        /// Perform the actual obsolete of a model object
        /// </summary>
        protected virtual TModel DoDeleteModel(DataContext context, Guid key, DeleteMode deleteMode)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

#if DEBUG
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Start();
#endif
                if (deleteMode == DeleteMode.PermanentDelete)
                {
                    this.DoDeleteReferencesInternal(context, key);
                }
                var dbInstance = this.DoDeleteInternal(context, key, deleteMode);

                if (!this.m_configuration.FastDelete)
                {
                    return this.DoConvertToInformationModel(context, dbInstance);
                }
                else
                {
                    return this.m_modelMapper.MapDomainInstance<TDbModel, TModel>(dbInstance);
                }
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose($"PERFORMANCE: DoObsoleteModel - {sw.ElapsedMilliseconds}ms");

            }
#endif
        }

        /// <summary>
        /// The object <paramref name="key"/> is being purged - delete all references for the object
        /// </summary>
        protected abstract void DoDeleteReferencesInternal(DataContext context, Guid key);

        /// <summary>
        /// Perform a get operation returning a model
        /// </summary>
        protected virtual TModel DoGetModel(DataContext context, Guid key, Guid? versionKey, bool allowCached = false)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (key == Guid.Empty) // empty get
            {
                return null;
            }
#if DEBUG
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Start();
#endif
                // Attempt fetch from master cache
                TModel retVal = null;
                var useCache = allowCached &&
                    this.m_configuration.CachingPolicy?.Targets.HasFlag(AdoDataCachingPolicyTarget.ModelObjects) == true;

                if (useCache)
                {
                    retVal = this.m_dataCacheService?.GetCacheItem<TModel>(key);
                    if(!this.ValidateCacheItemLoadMode(retVal))
                    {
                        retVal = null;
                    }
                }

                // Fetch from database
                if (retVal == null || versionKey.HasValue)
                {
                    var dbInstance = this.DoGetInternal(context, key, versionKey, allowCached);
                    if (dbInstance == null) // not found
                    {
                        retVal = null;
                    }
                    else
                    {
                        retVal = this.DoConvertToInformationModel(context, dbInstance);
                    }

                    // Add the cache object if caching is allowed on this query and if the load strategy used is less than the load strategy which would have already 
                    // been used to load it before
                    if (useCache && !versionKey.HasValue)
                    {
                        this.m_dataCacheService?.Add(retVal);
                    }
                }

                return retVal;
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose($"PERFORMANCE: DoGetModel - {sw.ElapsedMilliseconds}ms");

            }
#endif
        }

        /// <summary>
        /// Count the
        /// </summary>
        /// <param name="query">The query for which count is to be executed</param>
        /// <param name="authContext">The principal/authentication context being used</param>
        /// <returns>The count of matching records</returns>
        public virtual long Count(Expression<Func<TModel, bool>> query, IPrincipal authContext = null)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            using (var context = this.Provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();
                    return this.DoQueryInternal(context, query).Count();
                }
                catch (DbException e)
                {
                    throw e.TranslateDbException();
                }
                catch (Exception e)
                {
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.DATA_GENERAL), e);
                }
            }
        }

        /// <summary>
        /// Return true if the specified object exists
        /// </summary>
        public virtual bool Exists(DataContext context, Guid id, bool allowCache = false)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            bool retVal = false;
            if (allowCache && (this.m_configuration.CachingPolicy?.Targets & AdoDataCachingPolicyTarget.ModelObjects) == AdoDataCachingPolicyTarget.ModelObjects)
            {
                retVal |= this.m_dataCacheService?.Exists<TModel>(id) == true ||
                    this.m_adhocCache.Exists(this.GetAdHocCacheKey(id));
            }

            if (!retVal)
            {
                retVal |= this.DoQueryInternal(context, o => o.Key == id).Any();
            }

            return retVal;
        }

        /// <summary>
        /// Gets the specified object
        /// </summary>
        public object Get(Guid id)
        {
            return this.Get(id, null, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Primary GET function for the data persistence layer
        /// </summary>
        /// <param name="key">The key of the object to retrieve</param>
        /// <param name="versionKey">The specific version of the object</param>
        /// <param name="principal">The principal executing the query</param>
        /// <returns>The fetched object</returns>
        public virtual TModel Get(Guid key, Guid? versionKey, IPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            // Pre-persistence object argument
            var preEvent = new DataRetrievingEventArgs<TModel>(key, versionKey, principal);
            this.Retrieving?.Invoke(this, preEvent);
            if (preEvent.Cancel)
            {
                return preEvent.Result;
            }

            // Fetch from cache?
            TModel retVal = this.m_dataCacheService?.GetCacheItem<TModel>(key);
           
            if (retVal == null || versionKey.GetValueOrDefault() != Guid.Empty || !this.ValidateCacheItemLoadMode(retVal))
            {
                // Try-fetch
                using (var context = this.Provider.GetReadonlyConnection())
                {
                    try
                    {
                        context.Open();

                        // Is there an ad-hoc version from the database?
                        retVal = this.DoGetModel(context, key, versionKey, true);
                        retVal?.HarmonizeKeys(KeyHarmonizationMode.PropertyOverridesKey);
                    }
                    catch (DbException e)
                    {
                        throw e.TranslateDbException();
                    }
                    catch (Exception e)
                    {
                        throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.DATA_GENERAL), e);
                    }
                }
            }

            this.Retrieved?.Invoke(this, new DataRetrievedEventArgs<TModel>(retVal, principal));
            return retVal;
        }

        /// <summary>
        /// Insert the specified object into the context
        /// </summary>
        public object Insert(object data)
        {
            if (data is TModel model)
            {
                return this.Insert(model, TransactionMode.Commit, AuthenticationContext.Current.Principal);
            }
            else
            {
                throw new ArgumentException(nameof(data), String.Format(ErrorMessages.ARGUMENT_INVALID_TYPE, typeof(TModel), data.GetType()));
            }
        }

        /// <summary>
        /// Perform an insert operation
        /// </summary>
        /// <param name="data">The data which is to be inserted</param>
        /// <param name="mode">The mode of insertion (commit or rollback for testing)</param>
        /// <param name="principal">The principal to use to persist</param>
        /// <returns>The persisted object</returns>
        public TModel Insert(TModel data, TransactionMode mode, IPrincipal principal)
        {
            if (data == default(TModel))
            {
                throw new ArgumentNullException(nameof(data), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            // Fire pre-event
            var preEvent = new DataPersistingEventArgs<TModel>(data, mode, principal);
            this.Inserting?.Invoke(this, preEvent);
            if (preEvent.Cancel)
            {
                this.m_tracer.TraceVerbose("Pre-Persistence Event for INSERT {0} indicates cancel", data);
                return preEvent.Data;
            }


            try
            {
                using (var context = this.Provider.GetWriteConnection())
                {
                    context.Open();

                    using (var tx = context.BeginTransaction())
                    {
                        // Establish provenance object
                        if (data is BaseEntityData be)
                        {
                            context.EstablishProvenance(principal, be.CreatedByKey);
                        }
                        else
                        {
                            context.EstablishProvenance(principal, null);
                        }

                        data = data.HarmonizeKeys(KeyHarmonizationMode.KeyOverridesProperty);
                        // Is this an update or insert?
                        if (this.m_configuration.AutoUpdateExisting && data.Key.HasValue && this.Exists(context, data.Key.Value))
                        {
                            this.m_tracer.TraceVerbose("Object {0} already exists - updating instead", data);
                            data = this.DoUpdateModel(context, data);
                            data.BatchOperation = Core.Model.DataTypes.BatchOperationType.Update;
                        }
                        else
                        {
                            data = this.DoInsertModel(context, data);
                            data.BatchOperation = Core.Model.DataTypes.BatchOperationType.Insert;

                        }
                        data = data.HarmonizeKeys(KeyHarmonizationMode.PropertyOverridesKey);

                        if (mode == TransactionMode.Commit)
                        {
                            tx.Commit();
                            this.m_dataCacheService?.Add(data);
                        }
                    }
                }

                // Post event
                var postEvt = new DataPersistedEventArgs<TModel>(data, mode, principal);
                this.Inserted?.Invoke(this, postEvt);

                return postEvt.Data;
            }
            catch (DbException e)
            {
                throw e.TranslateDbException();
            }
            catch (Exception e)
            {
                throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.DATA_GENERAL), e);
            }
        }

        /// <summary>
        /// Query the specified data store
        /// </summary>
        public IEnumerable Query(Expression query, int offset, int? count, out int totalResults)
        {
            if (query is Expression<Func<TModel, bool>> expr)
            {
                var retVal = this.Query(expr, AuthenticationContext.Current.Principal);
                totalResults = retVal.Count();
                return retVal.Skip(offset).Take(count ?? 100);
            }
            else
            {
                throw new ArgumentException(nameof(query), String.Format(ErrorMessages.ARGUMENT_INVALID_TYPE, typeof(Expression<Func<TModel, bool>>), query.GetType()));
            }
        }

        /// <summary>
        /// Perform the specified query
        /// </summary>
        public IQueryResultSet Query(Expression query)
        {
            if (query is Expression<Func<TModel, bool>> expr)
            {
                return this.Query(expr, AuthenticationContext.Current.Principal);
            }
            else
            {
                throw new ArgumentException(nameof(query), String.Format(ErrorMessages.ARGUMENT_INVALID_TYPE, typeof(Expression<Func<TModel, bool>>), query.GetType()));
            }
        }

        /// <summary>
        /// Executes the specified query returning the query set
        /// </summary>
        public IQueryResultSet<TModel> Query(Expression<Func<TModel, bool>> query, IPrincipal principal)
        { 
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            var preEvt = new QueryRequestEventArgs<TModel>(query, principal);
            this.Querying?.Invoke(this, preEvt);
            if (preEvt.Cancel)
            {

                this.m_tracer.TraceVerbose("Pre-Query Event Signalled Cancel: {0}", query);
                return preEvt.Results;
            }

            var results = this.DoQueryModel(query);
            var postEvt = new QueryResultEventArgs<TModel>(query, results, principal);
            this.Queried?.Invoke(this, postEvt);
            return postEvt.Results;
        }

        /// <summary>
        /// Query the specified repository
        /// </summary>
        public IEnumerable<TModel> Query(Expression<Func<TModel, bool>> query, int offset, int? count, out int totalResults, IPrincipal principal, params ModelSort<TModel>[] orderBy)
        {
            var retVal = this.Query(query, principal);
            totalResults = retVal.Count(); // perform fast count
            return retVal.Skip(offset).Take(count ?? 100);
        }

        /// <summary>
        /// Query legacy interface (query by id)
        /// </summary>
        public IEnumerable<TModel> Query(Expression<Func<TModel, bool>> query, Guid queryId, int offset, int? count, out int totalCount, IPrincipal overrideAuthContext, params ModelSort<TModel>[] orderBy)
        {
            var retVal = this.Query(query, overrideAuthContext);

            if (retVal is IOrderableQueryResultSet<TModel> orderable)
            {
                foreach (var s in orderBy)
                {
                    if (s.SortOrder == SortOrderType.OrderBy)
                    {
                        retVal = orderable.OrderBy(s.SortProperty);
                    }
                    else
                    {
                        retVal = orderable.OrderByDescending(s.SortProperty);
                    }
                }
            }

            // Count and return
            retVal = retVal.AsStateful(queryId);
            totalCount = retVal.Count();
            return retVal.Skip(offset).Take(count ?? 100);
        }

        /// <summary>
        /// Convert an instance to model
        /// </summary>
        /// <param name="domainInstance">Convert the object to model instance</param>
        /// <param name="context">The data context to be used for querying</param>
        /// <returns>The object as a model instance</returns>
        public object ToModelInstance(object domainInstance, DataContext context)
        {
            if (domainInstance == null)
            {
                return null;
            }
            else if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            if (domainInstance is TDbModel dbModel)
            {
                return this.DoConvertToInformationModel(context, dbModel, null);
            }
            else
            {
                throw new ArgumentException(nameof(domainInstance), String.Format(ErrorMessages.ARGUMENT_INVALID_TYPE, typeof(TDbModel), domainInstance.GetType()));
            }
        }

        /// <summary>
        /// Updates the provided object to match in the data store
        /// </summary>
        public object Update(object data)
        {
            if (data is TModel model)
            {
                return this.Update(model, TransactionMode.Commit, AuthenticationContext.Current.Principal);
            }
            else
            {
                throw new ArgumentException(nameof(data), String.Format(ErrorMessages.ARGUMENT_INVALID_TYPE, typeof(TModel), data.GetType()));
            }
        }

        /// <summary>
        /// Perform the actual update
        /// </summary>
        /// <param name="data">The data which is to be updated</param>
        /// <param name="mode">The transaction control mode</param>
        /// <param name="principal">The principal doing the update</param>
        /// <returns>The updated object</returns>
        public TModel Update(TModel data, TransactionMode mode, IPrincipal principal)
        {
            if (data == default(TModel))
            {
                throw new ArgumentNullException(nameof(data), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            var preEvt = new DataPersistingEventArgs<TModel>(data, mode, principal);
            this.Updating?.Invoke(this, preEvt);
            if (preEvt.Cancel)
            {
                return preEvt.Data;
            }

            using (var context = this.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    using (var tx = context.BeginTransaction())
                    {
                        // Establish provenance object
                        if (data is BaseEntityData be)
                        {
                            context.EstablishProvenance(principal, be.CreatedByKey);
                        }
                        else
                        {
                            context.EstablishProvenance(principal, null);
                        }

                        data = data.HarmonizeKeys(KeyHarmonizationMode.KeyOverridesProperty);
                        data = this.DoUpdateModel(context, data);
                        data.BatchOperation = Core.Model.DataTypes.BatchOperationType.Update;
                        data = data.HarmonizeKeys(KeyHarmonizationMode.PropertyOverridesKey);
                        if (mode == TransactionMode.Commit)
                        {
                            tx.Commit();
                            this.m_dataCacheService?.Add(data);

                        }
                    }

                    // Broadcast
                    var postEvt = new DataPersistedEventArgs<TModel>(data, mode, principal);
                    this.Updated?.Invoke(this, postEvt);

                    return postEvt.Data;
                }
                catch (DbException e)
                {
                    throw e.TranslateDbException();
                }
                catch (Exception e)
                {
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.DATA_GENERAL), e);
                }
            }
        }



        /// <summary>
        /// Get the ad-hoc cache key
        /// </summary>
        protected string GetAdHocCacheKey(Guid id) => $"{typeof(TModel).Name}.{id}";

        /// <summary>
        /// Execute the specified query on the specified object
        /// </summary>
        public virtual IOrmResultSet ExecuteQueryOrm(DataContext context, Expression<Func<TModel, bool>> query)
        {
            return this.DoQueryInternal(context, query, true);
        }

        /// <summary>
        /// Convert the specified object to model
        /// </summary>
        public virtual TModel ToModelInstance(DataContext context, object result)
        {
            var useCache = this.m_configuration.CachingPolicy?.Targets.HasFlag(AdoDataCachingPolicyTarget.ModelObjects) == true;
            // TODO: Add caching here
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (result == null)
            {
                return null;
            }
            else if (result is TDbModel dbModel)
            {
                // Retrieve cache version - check updated time
                var existing = this.m_dataCacheService?.GetCacheItem<TModel>(dbModel.Key);

                if (!useCache || existing == null || !this.ValidateCacheItem(existing, dbModel) || !this.ValidateCacheItemLoadMode(existing))
                {
                    var retVal = this.DoConvertToInformationModel(context, dbModel);

                    if (useCache)
                    {
                        this.m_dataCacheService?.Add(retVal);
                    }

                    return retVal;
                }
                else
                {
                    return existing;
                }
            }
            else if (result is CompositeResult composite)
            {
                var dbObject = composite.Values.OfType<TDbModel>().First();
                var existing = this.m_dataCacheService?.GetCacheItem<TModel>(dbObject.Key);
                if (!useCache || existing == null || !this.ValidateCacheItem(existing, dbObject) || !this.ValidateCacheItemLoadMode(existing))
                {
                    var retVal = this.DoConvertToInformationModel(context, dbObject, composite.Values.ToArray());

                    if (useCache)
                    {
                        this.m_dataCacheService?.Add(retVal);
                    }

                    return retVal;
                }
                else
                {
                    return existing;
                }
            }
            else
            {
                throw new ArgumentException(nameof(result), String.Format(ErrorMessages.ARGUMENT_INVALID_TYPE, typeof(TDbModel), result.GetType()));
            }
        }

        /// <summary>
        /// Validate the cache state of an object from cache
        /// </summary>
        private bool ValidateCacheItemLoadMode(TModel existing)
        {
            var loadMode = existing?.GetAnnotations<LoadMode>();
            return loadMode?.Any() != true || loadMode?.Max() >= (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy) == true;
        }

        /// <summary>
        /// Gets the specified identified object
        /// </summary>
        public TModel Get(DataContext context, Guid key)
        {
            return this.DoGetModel(context, key, null, true);
        }

        /// <summary>
        /// Map the sorting expression
        /// </summary>
        public virtual Expression MapExpression<TResult>(Expression<Func<TModel, TResult>> sortExpression)
        {
            if (sortExpression == null)
            {
                throw new ArgumentNullException(nameof(sortExpression), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            return this.m_modelMapper.MapModelExpression<TModel, TDbModel, TResult>(sortExpression);
        }


        /// <summary>
        /// ADO Persistence provider for query
        /// </summary>
        IQueryResultSet<TModel> IAdoPersistenceProvider<TModel>.Query(DataContext context, Expression<Func<TModel, bool>> filter)
             => new MappedQueryResultSet<TModel>(this, context).Where(filter);

        /// <summary>
        /// Query according to the provided <paramref name="query"/>
        /// </summary>
        IQueryResultSet IQuerySetProvider.Query(SqlStatement query)
             => new MappedQueryResultSet<TModel>(this).Execute<TDbModel>(query);

        /// <summary>
        /// ADO Persistence provider for insert
        /// </summary>
        TModel IAdoPersistenceProvider<TModel>.Insert(DataContext context, TModel data) => this.DoInsertModel(context, data);

        /// <summary>
        /// ADO Persistence provider for update
        /// </summary>
        TModel IAdoPersistenceProvider<TModel>.Update(DataContext context, TModel data) => this.DoUpdateModel(context, data);

        /// <summary>
        /// ADO persistence delete
        /// </summary>
        TModel IAdoPersistenceProvider<TModel>.Delete(DataContext context, Guid key, DeleteMode deleteMode) => this.DoDeleteModel(context, key, deleteMode);

        /// <summary>
        /// ADO non-generic delete
        /// </summary>
        public IdentifiedData Delete(DataContext context, Guid key, DeleteMode deleteMode) => this.DoDeleteModel(context, key, deleteMode);

        /// <summary>
        /// <summary>
        /// Ensure that the object exists in the database
        /// </summary>
        protected TData EnsureExists<TData>(DataContext context, TData data)
            where TData : IdentifiedData, new()
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (data == default(TData))
            {
                return default(TData);
            }

            var persistenceService = typeof(TData).GetRelatedPersistenceService() as IAdoPersistenceProvider<TData>;
            if (!data.Key.HasValue || !persistenceService.Exists(context, data.Key.Value))
            {
                if (this.m_configuration.AutoInsertChildren)
                {
                    return persistenceService.Insert(context, data);
                }
                else
                {
                    throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.RELATED_OBJECT_NOT_FOUND, new { name = typeof(TData).Name, source = data.Key }));
                }
            }
            else
            {
                return data;
            }
        }

        /// <summary>
        /// Delete the specified object
        /// </summary>
        /// <param name="key"></param>
        /// <param name="mode"></param>
        /// <param name="principal"></param>
        /// <param name="deletionMode"></param>
        /// <returns></returns>
        public TModel Delete(Guid key, TransactionMode mode, IPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            var preEvent = new DataPersistingEventArgs<TModel>(new TModel() { Key = key }, mode, principal);
            this.Deleting?.Invoke(this, preEvent);
            if (preEvent.Cancel)
            {
                this.m_tracer.TraceVerbose("Pre-Persistence event indicates cancel on Delete for {0}", key);
                return preEvent.Data;
            }

            using (var context = this.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    TModel retVal = default(TModel);
                    using (var tx = context.BeginTransaction())
                    {
                        // Establish provenance object
                        context.EstablishProvenance(principal, null);

                        retVal = this.DoDeleteModel(context, key, DataPersistenceControlContext.Current?.DeleteMode ?? this.m_configuration.DeleteStrategy);
                        retVal.BatchOperation = Core.Model.DataTypes.BatchOperationType.Delete;
                        if (mode == TransactionMode.Commit)
                        {
                            tx.Commit();
                            this.m_dataCacheService?.Remove(retVal);

                        }
                    }

                    // Post event
                    var postEvt = new DataPersistedEventArgs<TModel>(retVal, mode, principal);
                    this.Deleted?.Invoke(this, postEvt);

                    return postEvt.Data;
                }
                catch (DbException e)
                {
                    throw e.TranslateDbException();
                }
                catch (Exception e)
                {
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.DATA_GENERAL), e);
                }
            }
        }

        private TModel DoDeleteModel(DataContext context, Guid key, object p)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Delete all objects according to the current <see cref="DataPersistenceControlContext"/>
        /// </summary>
        /// <param name="expression">The records which should be deleted</param>
        /// <param name="mode">The transaction mode</param>
        /// <param name="principal">The principal</param>
        public void DeleteAll(Expression<Func<TModel, bool>> expression, TransactionMode mode, IPrincipal principal)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            using (var context = this.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    using (var tx = context.BeginTransaction())
                    {
                        // Establish provenance object
                        context.EstablishProvenance(principal, null);

                        this.DoDeleteAllModel(context, expression, DataPersistenceControlContext.Current?.DeleteMode ?? this.m_configuration.DeleteStrategy);

                        if (mode == TransactionMode.Commit)
                        {
                            tx.Commit();
                        }
                    }
                }
                catch (DbException e)
                {
                    throw e.TranslateDbException();
                }
                catch (Exception e)
                {
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.DATA_GENERAL), e);
                }
            }
        }

        /// <summary>
        /// Touch the object with <paramref name="key"/> but don't update it
        /// </summary>
        /// <param name="key">The key of the object to touch</param>
        /// <param name="mode">The mode (commit or rollback)</param>
        /// <param name="principal">The user touching the object</param>
        public TModel Touch(Guid key, TransactionMode mode, IPrincipal principal)
        {
            if (key == default(Guid))
            {
                throw new ArgumentNullException(nameof(key), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            using (var context = this.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    using (var tx = context.BeginTransaction())
                    {
                        // Establish provenance object
                        context.EstablishProvenance(principal, null);

                        var retVal = this.DoTouchModel(context, key);

                        if (mode == TransactionMode.Commit)
                        {
                            tx.Commit();
                            this.m_dataCacheService?.Add(retVal);
                        }
                        return retVal;
                    }

                }
                catch (DbException e)
                {
                    throw e.TranslateDbException();
                }
                catch (Exception e)
                {
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.DATA_GENERAL), e);
                }
            }
        }

        /// <inheritdoc/>
        public IdentifiedData Insert(DataContext context, IdentifiedData data) => this.DoInsertModel(context, data.Convert<TModel>());

        /// <inheritdoc/>
        public IdentifiedData Update(DataContext context, IdentifiedData data) => this.DoUpdateModel(context, data.Convert<TModel>());

        /// <summary>
        /// Touch the specified object
        /// </summary>
        public TModel Touch(DataContext context, Guid id) => this.DoTouchModel(context, id);

        /// <inheritdoc/>
        public bool Exists(DataContext context, Guid key) => context.Any<TDbModel>(o => o.Key == key);

        /// <summary>
        /// Perform a delete of the specified object
        /// </summary>
        public object Delete(Guid id) => this.Delete(id, TransactionMode.Commit, AuthenticationContext.Current.Principal);
    }
}