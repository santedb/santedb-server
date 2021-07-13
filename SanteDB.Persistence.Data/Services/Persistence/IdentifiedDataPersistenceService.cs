using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence
{
    /// <summary>
    /// This persistence class represents a persistence service which is capable of storing and maintaining 
    /// an IdentifiedData instance and its equivalent IDbIdentified
    /// </summary>
    public abstract class IdentifiedDataPersistenceService<TModel, TDbModel>
        : BasePersistenceService<TModel, TDbModel>
        where TModel : IdentifiedData, new()
        where TDbModel : DbIdentified, new()
    {

        /// <summary>
        /// Creates a new injected version of the IdentifiedDataPersistenceService
        /// </summary>
        public IdentifiedDataPersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService, IDataCachingService dataCachingService, IQueryPersistenceService queryPersistence) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Convert <paramref name="model" /> to a <typeparamref name="TDbModel"/>
        /// </summary>
        /// <param name="context">The data context in case data access is required</param>
        /// <param name="model">The model to be converted</param>
        /// <param name="referenceObjects">The referenced objects (for reference)</param>
        /// <returns>The <typeparamref name="TDbModel"/> instance</returns>
        protected override TDbModel DoConvertToDataModel(DataContext context, TModel model, params IDbIdentified[] referenceObjects)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (model == default(TModel))
            {
                throw new ArgumentNullException(nameof(model), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            return this.m_modelMapper.MapModelInstance<TModel, TDbModel>(model);
        }

        /// <summary>
        /// Converts an information model <paramref name="dbModel"/> to <typeparamref name="TModel"/>
        /// </summary>
        /// <param name="context">The data context which is being converted on</param>
        /// <param name="dbModel">The database model to be converted</param>
        /// <param name="referenceObjects">The reference objects for lookup</param>
        /// <returns>The converted model</returns>
        protected override TModel DoConvertToInformationModel(DataContext context, TDbModel dbModel, params IDbIdentified[] referenceObjects)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (dbModel == default(TDbModel))
            {
                throw new ArgumentNullException(nameof(dbModel), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            return this.m_modelMapper.MapDomainInstance<TDbModel, TModel>(dbModel);
        }

        /// <summary>
        /// Get a database model version direct from the database
        /// </summary>
        /// <param name="context">The context from which the data should be fetched</param>
        /// <param name="key">The key of data which should be fetched</param>
        /// <param name="versionKey">The version key</param>
        /// <param name="allowCache">True if loading data from the ad-hoc caching service is allowed</param>
        /// <returns>The database model</returns>
        protected override TDbModel DoGetInternal(DataContext context, Guid key, Guid? versionKey, bool allowCache = false)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            TDbModel retVal = default(TDbModel);
            var cacheKey = this.GetAdHocCacheKey(key);
            if (allowCache && (this.m_configuration?.CachingPolicy.Targets & Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects) == Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects)
            {
                retVal = this.m_adhocCache?.Get<TDbModel>(cacheKey) ;
            }
            if (retVal == null)
            {
                retVal = context.FirstOrDefault<TDbModel>(o => o.Key == key);

                if ((this.m_configuration?.CachingPolicy.Targets & Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects) == Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects)
                {
                    this.m_adhocCache.Add<TDbModel>(cacheKey, retVal, this.m_configuration.CachingPolicy?.DataObjectExpiry);
                }
            }

            return retVal;
        }

        /// <summary>
        /// Perform an insert of an identified object
        /// </summary>
        /// <param name="context">The context on which the data should be inserted</param>
        /// <param name="dbModel">The object which is to be inserted</param>
        protected override TDbModel DoInsertInternal(DataContext context, TDbModel dbModel)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }

#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            try
            {
#endif
                return context.Insert(dbModel);
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose("Insert {0} took {1}ms", dbModel, sw.ElapsedMilliseconds);
            }
#endif
        }

        /// <summary>
        /// Obsolete the specified object which for the generic identified data persistene service means deletion
        /// </summary>
        /// <param name="context">The context on which the obsoletion should occur</param>
        /// <param name="key">The key of the object to delete</param>
        protected override TDbModel DoObsoleteInternal(DataContext context, Guid key)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            if (key == Guid.Empty)
            {
                throw new ArgumentException(nameof(key), ErrorMessages.ERR_ARGUMENT_RANGE);
            }

#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            try
            {
#endif

                // Obsolete the data by key
                var dbData = context.FirstOrDefault<TDbModel>(o => o.Key == key);
                if (dbData == null)
                {
                    throw new KeyNotFoundException(ErrorMessages.ERR_NOT_FOUND.Format(key));
                }
                context.Delete(dbData);
                return dbData;
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose("Obsolete {0} took {1}ms", key, sw.ElapsedMilliseconds);
            }
#endif
        }

        /// <summary>
        /// Perform a query for the specified object 
        /// </summary>
        /// <param name="context">The context on which the query should be executed</param>
        /// <param name="query">The query in the model format which should be executed</param>
        /// <param name="allowCache">True if using the ad-hoc cache is permitted </param>
        /// <returns>The delay executed result set which represents the query</returns>
        protected override OrmResultSet<TDbModel> DoQueryInternal(DataContext context, Expression<Func<TModel, bool>> query, bool allowCache = false)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (query == null)
            {
                throw new ArgumentNullException(nameof(query), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            // Convert the query to a domain query so that the object persistence layer can turn the 
            // structured LINQ query into a SQL statement
            var domainQuery = context.CreateSqlStatement<TDbModel>().SelectFrom(typeof(TDbModel));
            var expression = this.m_modelMapper.MapModelExpression<TModel, TDbModel, bool>(query, false);
            if (expression != null)
            {
                domainQuery.Where<TDbModel>(expression);
            }
            else
            {
                this.m_tracer.TraceVerbose("Will use slow query construction due to complex mapped fields");
                domainQuery.Where(context.GetQueryBuilder(this.m_modelMapper).CreateQuery(query));
            }

            return context.Query<TDbModel>(domainQuery.Build());
        }

        /// <summary>
        /// Perform an update of the specified <paramref name="model"/>
        /// </summary>
        /// <param name="context">The database context on which the update should occur</param>
        /// <param name="model">The model which represents the newest version of the object to be updated</param>
        /// <returns>The updated object</returns>
        protected override TDbModel DoUpdateInternal(DataContext context, TDbModel model)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (model == default(TDbModel))
            {
                throw new ArgumentNullException(nameof(model), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (model.Key == Guid.Empty)
            {
                throw new ArgumentException(nameof(model.Key), ErrorMessages.ERR_NON_IDENTITY_UPDATE);
            }

            // perform 
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            try
            {
#endif

                return context.Update(model);
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose("Update {0} took {1}ms", model, sw.ElapsedMilliseconds);
            }
#endif

        }
    }
}
