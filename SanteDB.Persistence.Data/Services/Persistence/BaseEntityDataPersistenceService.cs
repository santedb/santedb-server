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
        protected override IEnumerable<TDbModel> DoDeleteAllInternal(DataContext context, Expression<Func<TModel, bool>> expression, DeleteMode deletionMode)
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

                    foreach (var obj in context.Query(domainExpression))
                    {
                        switch (deletionMode)
                        {
                            case DeleteMode.LogicalDelete:
                                // If logically deleting a logically deleted object it is perma delete
                                if (obj.ObsoletionTime.HasValue)
                                {
                                    goto case DeleteMode.PermanentDelete;
                                }
                                obj.ObsoletedByKey = context.ContextId;
                                obj.ObsoletionTime = DateTimeOffset.Now;
                                context.Update(obj);
                                break;
                            case DeleteMode.PermanentDelete:
                                this.DoDeleteReferencesInternal(context, obj.Key);
                                context.Delete(obj);
                                break;
                        }
                        yield return obj;

                    }
                }
                else
                {
                    this.m_tracer.TraceVerbose("Will use slow query construction due to complex mapped fields");
                    var domainQuery = context.GetQueryBuilder(this.m_modelMapper).CreateQuery(expression);

                    foreach (var obj in context.Query<TDbModel>(domainQuery))
                    {
                        switch (deletionMode)
                        {
                            case DeleteMode.LogicalDelete:
                                if(obj.ObsoletionTime.HasValue)
                                {
                                    goto case DeleteMode.PermanentDelete;
                                }
                                obj.ObsoletedByKey = context.ContextId;
                                obj.ObsoletionTime = DateTimeOffset.Now;
                                context.Update(obj);
                                break;
                            case DeleteMode.PermanentDelete:
                                this.DoDeleteReferencesInternal(context, obj.Key);
                                context.Delete(obj);
                                break;
                        }
                        yield return obj;
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

        /// <inheritdoc/>
        protected override Expression<Func<TModel, bool>> ApplyDefaultQueryFilters(Expression<Func<TModel, bool>> query)
        {
            // If the user has not explicitly set the obsoletion time parameter then we will add it
            if (!query.ToString().Contains(nameof(BaseEntityData.ObsoletionTime)))
            {
                var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(BaseEntityData.ObsoletionTime))), Expression.Constant(null));
                query = Expression.Lambda<Func<TModel, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, obsoletionReference, query.Body), query.Parameters);
            }
            return base.ApplyDefaultQueryFilters(query);
        }


        /// <summary>
        /// Perform an obsolete either logically (if configured) or a hard delete
        /// </summary>
        /// <param name="context">The context on which the delete should be performed</param>
        /// <param name="key">The key of the object to be deleted</param>
        /// <returns>The deleted/obsoleted object</returns>
        protected override TDbModel DoDeleteInternal(DataContext context, Guid key, DeleteMode deletionMode)
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

        /// <summary>
        /// Convert the data model to the information model
        /// </summary>
        protected override TModel DoConvertToInformationModel(DataContext context, TDbModel dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
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