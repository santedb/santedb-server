using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
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
        public BaseEntityDataPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override bool ValidateCacheItem(TModel cacheEntry, TDbModel dataModel) => cacheEntry.CreationTime >= dataModel.CreationTime;

        /// <summary>
        /// Perform an insert on the specified object
        /// </summary>
        /// <param name="context">The context object to be actioned</param>
        /// <param name="dbModel">The objet to be inserted</param>
        /// <returns>The inserted object with any key data</returns>
        protected override TDbModel DoInsertInternal(DataContext context, TDbModel dbModel)
        {
            if (dbModel == null)
            {
                throw new ArgumentNullException(nameof(dbModel), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
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
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            var existing = context.FirstOrDefault<TDbModel>(o => o.Key == model.Key);
            if (existing == null)
            {
                throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { type = typeof(TModel).Name, id = model.Key }));
            }

            // Un-delete the object
            existing.CopyObjectData(model, true);
            existing.ObsoletedByKey = null;
            existing.ObsoletionTime = null;
            existing.ObsoletionTimeSpecified = existing.ObsoletedByKeySpecified = true;

            return base.DoUpdateInternal(context, existing);
        }

        /// <summary>
        /// Obsolete all objects
        /// </summary>
        protected override void DoDeleteAllInternal(DataContext context, Expression<Func<TModel, bool>> expression, DeleteMode deletionMode)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            if (expression == null)
            {
                throw new ArgumentException(nameof(expression), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_RANGE));
            }

#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            try
            {
#endif

                // If the user has not explicitly set the obsoletion time parameter then we will add it
                if (!expression.ToString().Contains(nameof(BaseEntityData.ObsoletionTime)))
                {
                    var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(expression.Parameters[0], typeof(TModel).GetProperty(nameof(BaseEntityData.ObsoletionTime))), Expression.Constant(null));
                    expression = Expression.Lambda<Func<TModel, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, obsoletionReference, expression.Body), expression.Parameters);
                }

                // Convert the query to a domain query so that the object persistence layer can turn the
                // structured LINQ query into a SQL statement
                var domainExpression = this.m_modelMapper.MapModelExpression<TModel, TDbModel, bool>(expression, false);
                if (domainExpression != null)
                {
                    switch (deletionMode)
                    {
                        case DeleteMode.NullifyDelete:
                            context.UpdateAll<TDbModel, IDbHasStatus>(domainExpression, o => o.StatusConceptKey == StatusKeys.Nullified);
                            goto case DeleteMode.LogicalDelete;
                        case DeleteMode.ObsoleteDelete:
                            context.UpdateAll<TDbModel, IDbHasStatus>(domainExpression, o => o.StatusConceptKey == StatusKeys.Obsolete);
                            goto case DeleteMode.LogicalDelete;
                        case DeleteMode.LogicalDelete:
                            context.UpdateAll(domainExpression, o => o.ObsoletionTime == DateTimeOffset.Now, o => o.ObsoletedByKey == context.ContextId);
                            break;

                        case DeleteMode.PermanentDelete:
                            foreach (var obj in context.Query<TDbModel>(domainExpression))
                            {
                                this.DoDeleteReferencesInternal(context, obj.Key);
                                context.Delete(obj);
                            }
                            break;
                    }
                }
                else
                {
                    this.m_tracer.TraceVerbose("Will use slow query construction due to complex mapped fields");
                    var domainQuery = context.GetQueryBuilder(this.m_modelMapper).CreateQuery(expression);

                    if (deletionMode == DeleteMode.PermanentDelete)
                    {
                        foreach (var obj in context.Query<TDbModel>(domainQuery))
                        {
                            this.DoDeleteReferencesInternal(context, obj.Key);
                            context.Delete(obj);
                        }
                    }
                    else
                    {
                        context.UpdateAll<TDbModel>(context.Query<TDbModel>(domainQuery), o =>
                        {
                            if (o is IDbHasStatus hasStatus)
                            {
                                switch (deletionMode)
                                {
                                    case DeleteMode.NullifyDelete:
                                        hasStatus.StatusConceptKey = StatusKeys.Nullified;
                                        break;

                                    case DeleteMode.ObsoleteDelete:
                                        hasStatus.StatusConceptKey = StatusKeys.Obsolete;
                                        break;
                                }
                            }
                            o.ObsoletedByKey = context.ContextId;
                            o.ObsoletionTime = DateTimeOffset.Now;
                            return o;
                        });
                    }
                }

#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose("Obsolete all {0} took {1}ms", expression, sw.ElapsedMilliseconds);
            }
#endif
        }

        /// <summary>
        /// Perform query and return the specified result set type
        /// </summary>
        protected override OrmResultSet<TReturn> DoQueryInternalAs<TReturn>(DataContext context, Expression<Func<TModel, bool>> query, Func<SqlStatement, SqlStatement> queryModifier = null)
        {
            // If the user has not explicitly set the obsoletion time parameter then we will add it
            if (!query.ToString().Contains(nameof(BaseEntityData.ObsoletionTime)))
            {
                var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(BaseEntityData.ObsoletionTime))), Expression.Constant(null));
                query = Expression.Lambda<Func<TModel, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, obsoletionReference, query.Body), query.Parameters);
            }

            return base.DoQueryInternalAs<TReturn>(context, query, queryModifier);
        }

        /// <summary>
        /// Perform an obsolete either logically (if configured) or a hard delete
        /// </summary>
        /// <param name="context">The context on which the delete should be performed</param>
        /// <param name="key">The key of the object to be deleted</param>
        /// <returns>The deleted/obsoleted object</returns>
        protected override TDbModel DoDeleteInternal(DataContext context, Guid key, DeleteMode deletionMode)
        {
            // Logical deletion is enabled? Then the obsolete is an update
            if (this.m_configuration.VersioningPolicy.HasFlag(Data.Configuration.AdoVersioningPolicyFlags.LogicalDeletion))
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
                }
                if (key == Guid.Empty)
                {
                    throw new ArgumentException(nameof(key), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_RANGE));
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
                        throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { id = key, type = typeof(TModel).Name }));
                    }

                    switch (deletionMode)
                    {
                        case DeleteMode.ObsoleteDelete:
                            if (dbData is IDbHasStatus obsoleteState)
                            {
                                obsoleteState.StatusConceptKey = StatusKeys.Obsolete;
                            }
                            goto case DeleteMode.LogicalDelete; // fallthrough hack for C#
                        case DeleteMode.NullifyDelete:
                            if (dbData is IDbHasStatus deleteState)
                            {
                                deleteState.StatusConceptKey = StatusKeys.Nullified;
                            }
                            goto case DeleteMode.LogicalDelete; // fallthrough case hack for C#
                        case DeleteMode.LogicalDelete:
                            dbData.ObsoletedByKey = context.ContextId;
                            dbData.ObsoletionTime = DateTimeOffset.Now;
                            dbData = context.Update(dbData);
                            break;

                        case DeleteMode.PermanentDelete:
                            context.Delete(dbData);
                            break;
                    }

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
                return base.DoDeleteInternal(context, key, deletionMode);
            }
        }

        /// <summary>
        /// Convert the data model to the information model
        /// </summary>
        protected override TModel DoConvertToInformationModel(DataContext context, TDbModel dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch (DataPersistenceQueryContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.CreatedBy = retVal.CreatedBy.GetRelatedPersistenceService().Get(context, dbModel.CreatedByKey);
                    retVal.SetLoaded(nameof(BaseEntityData.CreatedBy));
                    retVal.ObsoletedBy = retVal.ObsoletedBy.GetRelatedPersistenceService().Get(context, dbModel.ObsoletedByKey.GetValueOrDefault());
                    retVal.SetLoaded(nameof(BaseEntityData.ObsoletedBy));
                    break;
            }

            return retVal;
        }
    }
}