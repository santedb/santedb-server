using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Security;
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
    /// Represents a persistence service which has behaviors to properly persist <see cref="BaseEntityData"/>
    /// </summary>
    /// <typeparam name="TModel">The model in RIM Objects</typeparam>
    /// <typeparam name="TDbModel">The physical model class</typeparam>
    public abstract class BaseEntityDataPersistenceService<TModel, TDbModel> : IdentifiedDataPersistenceService<TModel, TDbModel>
        where TModel : BaseEntityData, new()
        where TDbModel : class, IDbBaseData, new()
    {
        /// <summary>
        /// Creates a new base entity data with the specified data classes injected
        /// </summary>
        public BaseEntityDataPersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }


        /// <summary>
        /// Perform an insert on the specified object
        /// </summary>
        /// <param name="context">The context object to be actioned</param>
        /// <param name="dbModel">The objet to be inserted</param>
        /// <returns>The inserted object with any key data</returns>
        protected override TDbModel DoInsertInternal(DataContext context, TDbModel dbModel)
        {
            if(dbModel == null)
            {
                throw new ArgumentNullException(nameof(dbModel), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            // Set the creation time and provenance data
            dbModel.CreationTime = DateTimeOffset.Now;
            dbModel.CreatedByKey = context.ContextId;

            return base.DoInsertInternal(context, dbModel);
        }

        /// <summary>
        /// Update the base entity data - when logical deletion is used this re-activates or un-deletes it
        /// </summary>
        /// <param name="context">The context on which the update should occur</param>
        /// <param name="model">The object which is to be updated</param>
        /// <returns>The updated object</returns>
        protected override TDbModel DoUpdateInternal(DataContext context, TDbModel model)
        {
            if(model == null)
            {
                throw new ArgumentNullException(nameof(model), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            var existing = context.FirstOrDefault<TDbModel>(o => o.Key == model.Key);
            if(existing == null)
            {
                throw new KeyNotFoundException(ErrorMessages.ERR_NOT_FOUND.Format(model));
            }

            // Un-delete the object
            existing.CopyObjectData(model, true);
            existing.ObsoletedByKey = null;
            existing.ObsoletionTime = null;
            existing.ObsoletionTimeSpecified = existing.ObsoletedByKeySpecified = true;

            return base.DoUpdateInternal(context, existing);

        }

        /// <summary>
        /// Performs the specified query on the <paramref name="context"/>
        /// </summary>
        /// <param name="context">The context on which the query should be executed</param>
        /// <param name="query">The query which is to be executed</param>
        /// <param name="allowCache">True if caching should be used</param>
        /// <returns>A delay-load result set which contains the results of <paramref name="query"/></returns>
        protected override OrmResultSet<TDbModel> DoQueryInternal(DataContext context, Expression<Func<TModel, bool>> query, bool allowCache = false)
        {
            if(query == null)
            {
                throw new ArgumentNullException(nameof(query), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            // If the user has not explicitly set the obsoletion time parameter then we will add it
            if(!query.ToString().Contains(nameof(BaseEntityData.ObsoletionTime)))
            {
                var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(BaseEntityData.ObsoletionTime))), Expression.Constant(null));
                query = Expression.Lambda<Func<TModel, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, obsoletionReference, query.Body), query.Parameters);
            }

            return base.DoQueryInternal(context, query, allowCache);
        }

        /// <summary>
        /// Perform an obsolete either logically (if configured) or a hard delete
        /// </summary>
        /// <param name="context">The context on which the delete should be performed</param>
        /// <param name="key">The key of the object to be deleted</param>
        /// <returns>The deleted/obsoleted object</returns>
        protected override TDbModel DoObsoleteInternal(DataContext context, Guid key)
        {
            // Logical deletion is enabled? Then the obsolete is an update
            if (this.m_configuration.VersioningPolicy.HasFlag(Data.Configuration.AdoVersioningPolicyFlags.LogicalDeletion))
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
                    
                    dbData.ObsoletedByKey = context.ContextId;
                    dbData.ObsoletionTime = DateTimeOffset.Now;

                    context.Update(dbData);
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
            else
            {
                return base.DoObsoleteInternal(context, key);
            }
        }

        /// <summary>
        /// Convert the data model to the information model
        /// </summary>
        protected override TModel DoConvertToInformationModel(DataContext context, TDbModel dbModel, params IDbIdentified[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch(this.m_configuration.LoadStrategy)
            {
                case Configuration.LoadStrategyType.FullLoad:
                    retVal.CreatedBy = base.GetRelatedPersistenceService<SecurityProvenance>().Get(context, dbModel.CreatedByKey, null);
                    retVal.SetLoadIndicator(nameof(BaseEntityData.CreatedBy));
                    retVal.ObsoletedBy = base.GetRelatedPersistenceService<SecurityProvenance>().Get(context, dbModel.ObsoletedByKey.GetValueOrDefault(), null);
                    retVal.SetLoadIndicator(nameof(BaseEntityData.ObsoletedBy));
                    break;
            }

            return retVal;
        }
    }
}
