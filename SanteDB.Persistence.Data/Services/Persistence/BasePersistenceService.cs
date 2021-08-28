using SanteDB.Core;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Event;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.OrmLite.Providers;
using SanteDB.Persistence.Data.Configuration;
using SanteDB.Persistence.Data.Model;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
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
        IStoredQueryDataPersistenceService<TModel>,
        IAdoPersistenceProvider<TModel>,
        IAdoQueryProvider<TModel>,
        IDataPersistenceService
        where TModel : IdentifiedData, new()
        where TDbModel : class, IDbIdentified, new()
    {

        /// <summary>
        /// Get tracer for the specified persistence class
        /// </summary>
        protected Tracer m_tracer = Tracer.GetTracer(typeof(BasePersistenceService<TModel, TDbModel>));

        /// <summary>
        /// Data caching service
        /// </summary>
        protected IDataCachingService m_dataCacheService;

        /// <summary>
        /// Query persistence service
        /// </summary>
        protected IQueryPersistenceService m_queryPersistence;

        /// <summary>
        /// Ad-hoc caching service
        /// </summary>
        protected IAdhocCacheService m_adhocCache;

        /// <summary>
        /// Model mapper
        /// </summary>
        protected ModelMapper m_modelMapper;
        
        /// <summary>
        /// Configuration reference
        /// </summary>
        protected AdoPersistenceConfigurationSection m_configuration;

        /// <summary>
        /// Providers 
        /// </summary>
        private static IDictionary<Type, IAdoPersistenceProvider> s_providers = new ConcurrentDictionary<Type, IAdoPersistenceProvider>();

        /// <summary>
        /// Base persistence service
        /// </summary>
        public BasePersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCaching = null, IQueryPersistenceService queryPersistence = null)
        {
            this.m_dataCacheService = dataCaching;
            this.m_queryPersistence = queryPersistence;
            this.m_adhocCache = adhocCacheService;
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
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
        /// Fired after obsoletion occurs
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<TModel>> Obsoleted;
        /// <summary>
        /// Fired after obsoleting
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<TModel>> Obsoleting;
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
        /// Perform the query operation
        /// </summary>
        protected abstract OrmResultSet<TDbModel> DoQueryInternal(DataContext context, Expression<Func<TModel, bool>> query, bool allowCache = false);

        /// <summary>
        /// Perform an internal get operation
        /// </summary>
        protected abstract TDbModel DoGetInternal(DataContext context, Guid key, Guid? versionKey, bool allowCache = false);

        /// <summary>
        /// Convert to a database model
        /// </summary>
        /// <param name="context">The data context to fetch additional data from</param>
        /// <param name="dbModel">The model to be converted</param>
        /// <param name="referenceObjects">Any other ojects (via reference or joins) which may be of use</param>
        protected abstract TModel DoConvertToInformationModel(DataContext context, TDbModel dbModel, params IDbIdentified[] referenceObjects);

        /// <summary>
        /// Convert to data model
        /// </summary>
        protected abstract TDbModel DoConvertToDataModel(DataContext context, TModel model, params IDbIdentified[] referenceObjects);

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
        protected abstract TDbModel DoObsoleteInternal(DataContext context, Guid key);

        /// <summary>
        /// Perform a query on the model
        /// </summary>
        protected virtual IQueryResultSet<TModel> DoQueryModel(DataContext context, Expression<Func<TModel, bool>> query)
        {
            return new AdoQueryResultSet<TModel>(this).Where(query);
        }

        /// <summary>
        /// Prepare all references 
        /// </summary>
        protected abstract TModel PrepareReferences(DataContext context, TModel data);

        /// <summary>
        /// Load a model object
        /// </summary>
        protected IAdoPersistenceProvider<TRelated> GetRelatedPersistenceService<TRelated>()
        {
            if(!s_providers.TryGetValue(typeof(TRelated), out IAdoPersistenceProvider provider))
            {
                provider = ApplicationServiceContext.Current.GetService<IAdoPersistenceProvider<TRelated>>();
                if(provider != null)
                {
                    s_providers.Add(typeof(TRelated), provider);
                }
            }
            return provider as IAdoPersistenceProvider<TRelated>;
        }


        /// <summary>
        /// Perform the actual insert of a model object
        /// </summary>
        protected virtual TModel DoInsertModel(DataContext context, TModel data)
        {
            if(context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(data == default(TModel))
            {
                throw new ArgumentNullException(nameof(data), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            data = this.PrepareReferences(context, data);

#if PERFMON
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Start();
#endif
                var dbInstance = this.DoConvertToDataModel(context, data);
                dbInstance = this.DoInsertInternal(context, dbInstance);
                var retVal = this.DoConvertToInformationModel(context, dbInstance);
                return retVal;
#if PERFMON
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Verbose, $"PERFORMANCE: DoInsertModel - {sw.EllapsedMilliseconds}ms", data, new StackTrace());
            }
#endif

        }

        /// <summary>
        /// Perform the actual update of a model object
        /// </summary>
        protected virtual TModel DoUpdateModel(DataContext context, TModel data)
        {

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (data == default(TModel))
            {
                throw new ArgumentNullException(nameof(data), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            data = this.PrepareReferences(context, data);

#if PERFMON
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Start();
#endif
            var dbInstance = this.DoConvertToDataModel(context, data);
            dbInstance = this.DoUpdateInternal(context, dbInstance);
            var retVal = this.DoConvertToInformationModel(context, dbInstance);
            return retVal;

#if PERFMON
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Verbose, $"PERFORMANCE: DoUpdateModel - {sw.EllapsedMilliseconds}ms", data, new StackTrace());
            }
#endif
        }

        /// <summary>
        /// Perform the actual obsolete of a model object
        /// </summary>
        protected virtual TModel DoObsoleteModel(DataContext context, Guid key)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            

#if PERFMON
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Start();
#endif
            var dbInstance = this.DoObsoleteInternal(context, key);
            var retVal = this.DoConvertToInformationModel(context, dbInstance);
            return retVal;
#if PERFMON
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Verbose, $"PERFORMANCE: DoObsoleteModel - {sw.EllapsedMilliseconds}ms", data, new StackTrace());
            }
#endif
        }

        /// <summary>
        /// Perform a get operation returning a model
        /// </summary>
        protected virtual TModel DoGetModel(DataContext context, Guid key, Guid? versionKey, bool allowCached = false)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (key == Guid.Empty) // empty get
            {
                return null;
            }
#if PERFMON
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Start();
#endif
            // Attempt fetch from master cache
            TModel retVal = null;
            if (allowCached && (this.m_configuration.CachingPolicy?.Targets & AdoDataCachingPolicyTarget.ModelObjects) == AdoDataCachingPolicyTarget.ModelObjects)
            {
                retVal = this.m_dataCacheService?.GetCacheItem<TModel>(key);
            }

            // Fetch from database
            if (retVal == null || versionKey.HasValue)
            {
                var dbInstance = this.DoGetInternal(context, key, versionKey, allowCached);
                retVal = this.DoConvertToInformationModel(context, dbInstance);
            }

            return retVal;
#if PERFMON
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Verbose, $"PERFORMANCE: DoGetModel - {sw.EllapsedMilliseconds}ms", data, new StackTrace());
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
                throw new ArgumentNullException(nameof(query), ErrorMessages.ERR_ARGUMENT_NULL);
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
                    this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Error, "Data error executing count operation", query, e);
                    throw this.TranslateDbException(e);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Error, "General error executing count operation", query, e);
                    throw new DataPersistenceException(ErrorMessages.ERR_DATA_GENERAL, e);
                }
            }
        }

        /// <summary>
        /// Return true if the specified object exists
        /// </summary>
        public virtual bool Exists(DataContext context, Guid id, bool allowCache = false)
        {
            if(context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            bool retVal = false;
            if(allowCache && (this.m_configuration.CachingPolicy?.Targets & AdoDataCachingPolicyTarget.ModelObjects) == AdoDataCachingPolicyTarget.ModelObjects)
            {
                retVal |= this.m_dataCacheService?.Exists<TModel>(id) == true ||
                    this.m_adhocCache.Exists(this.GetAdHocCacheKey(id));
            }

            if(!retVal)
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

            if(principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            // Pre-persistence object argument
            var preEvent = new DataRetrievingEventArgs<TModel>(key, versionKey, principal);
            this.Retrieving?.Invoke(this, preEvent);
            if(preEvent.Cancel)
            {
                return preEvent.Result;
            }

            // Fetch from cache?
            TModel retVal = this.m_dataCacheService?.GetCacheItem<TModel>(key);
            if (retVal == null || versionKey.HasValue)
            {
                // Try-fetch
                using (var context = this.Provider.GetReadonlyConnection())
                {
                    try
                    {
                        context.Open();

                        // Is there an ad-hoc version from the database?
                        retVal = this.DoGetModel(context, key, versionKey, true);
                        retVal.HarmonizeKeys(KeyHarmonizationMode.PropertyOverridesKey);

                    }
                    catch (DbException e)
                    {
                        this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Error, "Data error executing get operation", key, e);
                        throw this.TranslateDbException(e);
                    }
                    catch (Exception e)
                    {
                        this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Error, "General error executing get operation", key, e);
                        throw new DataPersistenceException(ErrorMessages.ERR_DATA_GENERAL, e);
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
                throw new ArgumentException(nameof(data), ErrorMessages.ERR_ARGUMENT_INCOMPATIBLE_TYPE.Format(typeof(TModel)));
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
            if(data == default(TModel))
            {
                throw new ArgumentNullException(nameof(data), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(principal == null )
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            // Fire pre-event
            var preEvent = new DataPersistingEventArgs<TModel>(data, mode, principal);
            this.Inserting?.Invoke(this, preEvent);
            if(preEvent.Cancel)
            {
                this.m_tracer.TraceVerbose("Pre-Persistence Event for INSERT {0} indicates cancel", data);
                return preEvent.Data;
            }

            using(var context = this.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    using (var tx = context.BeginTransaction()) {

                        // Establish provenance object
                        if (data is BaseEntityData be)
                        {
                            context.EstablishProvenance(principal, be.CreatedByKey);
                        }
                        else
                        {
                            context.EstablishProvenance(principal, null);
                        }

                        data.HarmonizeKeys(KeyHarmonizationMode.KeyOverridesProperty);
                        // Is this an update or insert?
                        if (this.m_configuration.AutoUpdateExisting && data.Key.HasValue && this.Exists(context, data.Key.Value))
                        {
                            this.m_tracer.TraceVerbose("Object {0} already exists - updating instead", data);
                            data = this.DoUpdateModel(context, data);
                        }
                        else
                        {
                            data = this.DoInsertModel(context, data);
                        }
                        data.HarmonizeKeys(KeyHarmonizationMode.PropertyOverridesKey);

                        if (mode == TransactionMode.Commit)
                        {
                            tx.Commit();
                            // Cache - invalidate (force a reload)
                            this.m_dataCacheService?.Remove(data.Key.Value);
                        }
                    }
                    
                    // Post event
                    var postEvt = new DataPersistedEventArgs<TModel>(data, mode, principal);
                    this.Inserted?.Invoke(this, postEvt);

                    return postEvt.Data;
                }
                catch (DbException e)
                {
                    this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Error, "Data error executing insert operation", data, e);
                    throw this.TranslateDbException(e);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Error, "General error executing insert operation", data, e);
                    throw new DataPersistenceException(ErrorMessages.ERR_DATA_GENERAL, e);
                }
            }
        }

        /// <summary>
        /// Obsolete the specified data object
        /// </summary>
        public object Obsolete(Guid key)
        {
            return this.Obsolete(key, TransactionMode.Commit, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Obsolete the specified 
        /// </summary>
        /// <param name="id">The data object which is to be obsoleted</param>
        /// <param name="mode">The method of transaction control</param>
        /// <param name="principal">The principal which is obsoleting the data</param>
        /// <returns>The obsoleted data</returns>
        public TModel Obsolete(Guid id, TransactionMode mode, IPrincipal principal)
        {
            if(principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            var preEvent = new DataPersistingEventArgs<TModel>(new TModel() { Key = id }, mode, principal);
            this.Obsoleting?.Invoke(this, preEvent);
            if(preEvent.Cancel)
            {
                this.m_tracer.TraceVerbose("Pre-Persistence event indicates cancel on Obsolete for {0}", id);
                return preEvent.Data;
            }

            using(var context = this.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    TModel retVal = default(TModel);
                    using(var tx = context.BeginTransaction())
                    {

                        // Establish provenance object
                        context.EstablishProvenance(principal, null);

                        retVal = this.DoObsoleteModel(context, id);

                        if(mode == TransactionMode.Commit)
                        {
                            tx.Commit();
                            // Cache
                            this.m_dataCacheService?.Remove(id);
                        }
                    }

                    
                    // Post event
                    var postEvt = new DataPersistedEventArgs<TModel>(retVal, mode, principal);
                    this.Obsoleted?.Invoke(this, postEvt);

                    return postEvt.Data;
                }
                catch (DbException e)
                {
                    this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Error, "Data error executing obsolete operation", id, e);
                    throw this.TranslateDbException(e);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Error, "General error executing obsolete operation", id, e);
                    throw new DataPersistenceException(ErrorMessages.ERR_DATA_GENERAL, e);
                }
            }
        }

        /// <summary>
        /// Query the specified data store 
        /// </summary>
        public IEnumerable Query(Expression query, int offset, int? count, out int totalResults)
        {
            if(query is Expression<Func<TModel, bool>> expr)
            {
                var retVal = this.Query(expr, AuthenticationContext.Current.Principal).AsResultSet<TModel>();
                totalResults = retVal.Count();
                return retVal.Skip(offset).Take(count ?? 100);
            }
            else
            {
                throw new ArgumentException(nameof(query), ErrorMessages.ERR_ARGUMENT_INCOMPATIBLE_TYPE.Format(typeof(Expression<Func<TModel, bool>>)));
            }
        }

        /// <summary>
        /// Perform the specified query
        /// </summary>
        public IEnumerable Query(Expression query)
        {
            if (query is Expression<Func<TModel, bool>> expr)
            {
                return this.Query(expr, AuthenticationContext.Current.Principal);
            }
            else
            {
                throw new ArgumentException(nameof(query), ErrorMessages.ERR_ARGUMENT_INCOMPATIBLE_TYPE.Format(typeof(Expression<Func<TModel, bool>>)));
            }
        }

        /// <summary>
        /// Executes the specified query returning the query set
        /// </summary>
        public IQueryResultSet<TModel> Query(Expression<Func<TModel, bool>> query, IPrincipal principal)
        {
            if(query == null)
            {
                throw new ArgumentNullException(nameof(query), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            if(principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            var preEvt = new QueryRequestEventArgs<TModel>(query, principal);
            this.Querying?.Invoke(this, preEvt);
            if(preEvt.Cancel)
            {
                this.m_tracer.TraceVerbose("Pre-Query Event Signalled Cancel: {0}", query);
                return preEvt.Results.AsResultSet<TModel>();
            }

            using(var context = this.Provider.GetReadonlyConnection())
            {
                try
                {
                    var results = this.DoQueryModel(context, query);

                    var postEvt = new QueryResultEventArgs<TModel>(query, results, principal);
                    this.Queried?.Invoke(this, postEvt);
                    return postEvt.Results.AsResultSet<TModel>();
                }
                catch (DbException e)
                {
                    this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Error, "Data error executing query operation", query, e);
                    throw this.TranslateDbException(e);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Error, "General error executing query operation", query, e);
                    throw new DataPersistenceException(ErrorMessages.ERR_DATA_GENERAL, e);
                }
            }
        }

        /// <summary>
        /// Query the specified repository
        /// </summary>
        public IEnumerable<TModel> Query(Expression<Func<TModel, bool>> query, int offset, int? count, out int totalResults, IPrincipal principal, params ModelSort<TModel>[] orderBy)
        {
            var retVal = this.Query(query, principal).AsResultSet<TModel>();
            totalResults = retVal.Count(); // perform fast count
            return retVal.Skip(offset).Take(count ?? 100);
        }

        /// <summary>
        /// Query legacy interface (query by id)
        /// </summary>
        public IEnumerable<TModel> Query(Expression<Func<TModel, bool>> query, Guid queryId, int offset, int? count, out int totalCount, IPrincipal overrideAuthContext, params ModelSort<TModel>[] orderBy)
        {
            var retVal = this.Query(query, overrideAuthContext).AsResultSet<TModel>();

            foreach (var s in orderBy)
            {
                if (s.SortOrder == SortOrderType.OrderBy)
                {
                    retVal = retVal.OrderBy(s.SortProperty);
                }
                else
                {
                    retVal = retVal.OrderByDescending(s.SortProperty);
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
            if(domainInstance == null)
            {
                return null;
            }
            else if(context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            if (domainInstance is TDbModel dbModel)
            {
                return this.DoConvertToInformationModel(context, dbModel, null);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(domainInstance), ErrorMessages.ERR_ARGUMENT_INCOMPATIBLE_TYPE.Format(typeof(TDbModel)));
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
                throw new ArgumentException(nameof(data), ErrorMessages.ERR_ARGUMENT_INCOMPATIBLE_TYPE.Format(typeof(TModel)));
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
            if(data == default(TModel))
            {
                throw new ArgumentNullException(nameof(data), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            var preEvt = new DataPersistingEventArgs<TModel>(data, mode, principal);
            this.Updating?.Invoke(this, preEvt);
            if(preEvt.Cancel)
            {
                this.m_tracer.TraceVerbose("Pre-Persistence Event for Update indicates cancel for {0}", data);
                return preEvt.Data;
            }

            using(var context = this.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    using(var tx = context.BeginTransaction())
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

                        data.HarmonizeKeys(KeyHarmonizationMode.KeyOverridesProperty);
                        data = this.DoUpdateModel(context, data);
                        data.HarmonizeKeys(KeyHarmonizationMode.PropertyOverridesKey);
                        if (mode == TransactionMode.Commit)
                        {
                            tx.Commit();

                            // Cache - invalidate
                            this.m_dataCacheService?.Remove(data.Key.Value);
                        }
                    }

                    // Broadcast
                    var postEvt = new DataPersistedEventArgs<TModel>(data, mode, principal);
                    this.Updated?.Invoke(this, postEvt);

                    return postEvt.Data;
                }
                catch (DbException e)
                {
                    this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Error, "Data error executing update operation", data, e);
                    throw this.TranslateDbException(e);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Error, "General error executing update operation", data, e);
                    throw new DataPersistenceException(ErrorMessages.ERR_DATA_GENERAL, e);
                }
            }
        }


        /// <summary>
        /// Translates a DB exception to an appropriate SanteDB exception
        /// </summary>
        protected Exception TranslateDbException(DbException e)
        {
            this.m_tracer.TraceError("Will Translate DBException: {0} - {1}", e.Data["SqlState"] ?? e.ErrorCode, e.Message);
            if (e.Data["SqlState"] != null)
            {
                switch (e.Data["SqlState"].ToString())
                {
                    case "O9001": // SanteDB => Data Validation Error
                        return new DetectedIssueException(
                            new DetectedIssue(DetectedIssuePriorityType.Error, e.Data["SqlState"].ToString(), e.Message, DetectedIssueKeys.InvalidDataIssue));
                    case "O9002": // SanteDB => Codification error
                        return new DetectedIssueException(new List<DetectedIssue>() {
                                        new DetectedIssue(DetectedIssuePriorityType.Error, e.Data["SqlState"].ToString(),  e.Message, DetectedIssueKeys.CodificationIssue),
                                        new DetectedIssue(DetectedIssuePriorityType.Information, e.Data["SqlState"].ToString(), "HINT: Select a code that is from the correct concept set or add the selected code to the concept set", DetectedIssueKeys.CodificationIssue)
                                    });
                    case "23502": // PGSQL - NOT NULL 
                        return new DetectedIssueException(
                                        new DetectedIssue(DetectedIssuePriorityType.Error, e.Data["SqlState"].ToString(), e.Message, DetectedIssueKeys.InvalidDataIssue)
                                    );
                    case "23503": // PGSQL - FK VIOLATION
                        return new DetectedIssueException(
                                        new DetectedIssue(DetectedIssuePriorityType.Error, e.Data["SqlState"].ToString(), e.Message, DetectedIssueKeys.FormalConstraintIssue)
                                    );
                    case "23505": // PGSQL - UQ VIOLATION
                        return new DetectedIssueException(
                                        new DetectedIssue(DetectedIssuePriorityType.Error, e.Data["SqlState"].ToString(), e.Message, DetectedIssueKeys.AlreadyDoneIssue)
                                    );
                    case "23514": // PGSQL - CK VIOLATION
                        return new DetectedIssueException(new List<DetectedIssue>()
                        {
                            new DetectedIssue(DetectedIssuePriorityType.Error, e.Data["SqlState"].ToString(), e.Message, DetectedIssueKeys.FormalConstraintIssue),
                            new DetectedIssue(DetectedIssuePriorityType.Information, e.Data["SqlState"].ToString(), "HINT: The code you're using may be incorrect for the given context", DetectedIssueKeys.CodificationIssue)
                        });
                    default:
                        return new DataPersistenceException(e.Message, e);
                }
            }
            else
            {
                return new DetectedIssueException(new DetectedIssue(DetectedIssuePriorityType.Error, "dbexception", e.Message, DetectedIssueKeys.OtherIssue));
            }
        }

        /// <summary>
        /// Get the ad-hoc cache key
        /// </summary>
        protected string GetAdHocCacheKey(Guid id) => $"{typeof(TModel).Name}.{id}";

        /// <summary>
        /// Get the ad-hoc cache key
        /// </summary>
        protected string GetAdHocCacheKey(IDbIdentified internalData) => $"{internalData.GetType().Name}.{internalData.Key}";

        /// <summary>
        /// Execute the specified query on the specified object
        /// </summary>
        public IOrmResultSet ExecuteQueryOrm(DataContext context, Expression<Func<TModel, bool>> query)
        {
            return this.DoQueryInternal(context, query, true);
        }

        /// <summary>
        /// Convert the specified object to model
        /// </summary>
        public TModel ToModelInstance(DataContext context, object result)
        {
            if(context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (result == null)
            {
                return null;
            }
            else if (result is TDbModel dbModel)
            {
                var retVal = this.DoConvertToInformationModel(context, dbModel);
                return retVal;
            }
            else
            {
                throw new ArgumentException(nameof(result), ErrorMessages.ERR_ARGUMENT_INCOMPATIBLE_TYPE);
            }
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
        public Expression MapSortExpression(Expression<Func<TModel, dynamic>> sortExpression)
        {
            if(sortExpression == null)
            {
                throw new ArgumentNullException(nameof(sortExpression), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            return this.m_modelMapper.MapModelExpression<TModel, TDbModel, dynamic>(sortExpression);
        }

        /// <summary>
        /// ADO Persistence provider for query
        /// </summary>
        IQueryResultSet<TModel> IAdoPersistenceProvider<TModel>.Query(DataContext context, Expression<Func<TModel, bool>> filter) => this.DoQueryModel(context, filter);

        /// <summary>
        /// ADO Persistence provider for insert
        /// </summary>
        TModel IAdoPersistenceProvider<TModel>.Insert(DataContext context, TModel data) => this.DoInsertModel(context, data);

        /// <summary>
        /// ADO Persistence provider for update
        /// </summary>
        TModel IAdoPersistenceProvider<TModel>.Update(DataContext context, TModel data) => this.DoUpdateModel(context, data);

        /// <summary>
        /// ADO persistence obsolete
        /// </summary>
        TModel IAdoPersistenceProvider<TModel>.Obsolete(DataContext context, Guid key) => this.DoObsoleteModel(context, key);

        /// <summary>
        /// ADO persistence get
        /// </summary>
        TModel IAdoPersistenceProvider<TModel>.Get(DataContext context, Guid key, Guid? versionKey) => this.DoGetModel(context, key, versionKey, false);
    }
}
