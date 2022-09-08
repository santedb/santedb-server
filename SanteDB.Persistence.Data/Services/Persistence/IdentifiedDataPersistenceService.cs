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
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.OrmLite.MappedResultSets;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        where TDbModel : class, IDbIdentified, new()
    {
        /// <summary>
        /// Creates a new injected version of the IdentifiedDataPersistenceService
        /// </summary>
        public IdentifiedDataPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare references
        /// </summary>
        protected override TModel BeforePersisting(DataContext context, TModel data) => data;

        /// <summary>
        /// After the object is persisted
        /// </summary>
        protected override TModel AfterPersisted(DataContext context, TModel data) => data;

        /// <summary>
        /// The object <paramref name="key"/> is being purged - delete all references for the object
        /// </summary>
        protected override void DoDeleteReferencesInternal(DataContext context, Guid key)
        { }

        /// <inheritdoc/>
        /// <remarks>There is no effective way to determine stale-ness of the cache entry here - these objects are property dependent</remarks>
        protected override bool ValidateCacheItem(TModel cacheEntry, TDbModel dataModel) => false;

        /// <summary>
        /// Perform query model
        /// </summary>
        protected override IQueryResultSet<TModel> DoQueryModel(Expression<Func<TModel, bool>> query) => new MappedQueryResultSet<TModel>(this).Where(query);
        
        /// <summary>
        /// Convert <paramref name="model" /> to a <typeparamref name="TDbModel"/>
        /// </summary>
        /// <param name="context">The data context in case data access is required</param>
        /// <param name="model">The model to be converted</param>
        /// <param name="referenceObjects">The referenced objects (for reference)</param>
        /// <returns>The <typeparamref name="TDbModel"/> instance</returns>
        protected override TDbModel DoConvertToDataModel(DataContext context, TModel model, params Object[] referenceObjects)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (model == default(TModel))
            {
                throw new ArgumentNullException(nameof(model), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            return this.m_modelMapper.MapModelInstance<TModel, TDbModel>(model);
        }

        /// <summary>
        /// Converts an information model <paramref name="dbModel"/> to <typeparamref name="TModel"/>
        /// </summary>
        /// <param name="context">The data context which is being converted on</param>
        /// <param name="dbModel">The database model to be converted</param>
        /// <param name="referenceObjects">If this method is called from a <see cref="CompositeResult"/> then the other data in the composite result</param>
        /// <returns>The converted model</returns>
        protected override TModel DoConvertToInformationModel(DataContext context, TDbModel dbModel, params Object[] referenceObjects)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (dbModel == default(TDbModel))
            {
                throw new ArgumentNullException(nameof(dbModel), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            var retVal = this.m_modelMapper.MapDomainInstance<TDbModel, TModel>(dbModel);
            retVal.AddAnnotation(DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy);
            return retVal;
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
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            TDbModel retVal = default(TDbModel);
            var cacheKey = this.GetAdHocCacheKey(key);
            if (allowCache && (this.m_configuration?.CachingPolicy?.Targets & Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects) == Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects)
            {
                retVal = this.m_adhocCache?.Get<TDbModel>(cacheKey);
            }
            if (retVal == null)
            {
                retVal = context.FirstOrDefault<TDbModel>(o => o.Key == key);

                if ((this.m_configuration?.CachingPolicy?.Targets & Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects) == Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects)
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
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
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
        /// Obsolete all objects
        /// </summary>
        protected override IEnumerable<TDbModel> DoDeleteAllInternal(DataContext context, Expression<Func<TModel, bool>> expression, DeleteMode deleteMode)
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
                // Convert the query to a domain query so that the object persistence layer can turn the
                // structured LINQ query into a SQL statement
                var domainExpression = this.m_modelMapper.MapModelExpression<TModel, TDbModel, bool>(expression, false);
                if (domainExpression != null)
                {
                    foreach(var itm in context.Query<TDbModel>(domainExpression))
                    {
                        context.Delete(itm);
                        yield return itm;
                    }
                }
                else
                {
                    this.m_tracer.TraceVerbose("Will use slow query construction due to complex mapped fields");
                    var domainQuery = context.GetQueryBuilder(this.m_modelMapper).CreateWhere(expression);
                    foreach(var itm in context.Query<TDbModel>(domainQuery.Build()))
                    {
                        context.Delete(itm);
                        yield return itm;
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
        /// Obsolete the specified object which for the generic identified data persistene service means deletion
        /// </summary>
        /// <param name="context">The context on which the obsoletion should occur</param>
        /// <param name="key">The key of the object to delete</param>
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
                    throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { type = typeof(TModel).Name, id = key }));
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
            return this.DoQueryInternalAs<TDbModel>(context, query);
        }

        /// <summary>
        /// Apply default query filters to the query provided by the caller
        /// </summary>
        /// <remarks>This method is used to append filters for obsoletion and version metadata, filtering on permissions or 
        /// any other restrictions on the filters automatically applied by the persistence layer</remarks>
        /// <returns>The modified query</returns>
        /// <param name="query">The query supplied by the caller</param>
        protected virtual Expression<Func<TModel, bool>> ApplyDefaultQueryFilters(Expression<Func<TModel, bool>> query) => query;

        /// <summary>
        /// Perform the query however return a custom <typeparamref name="TReturn"/>. This function allows you
        /// to modify the query instructions before sending query to the database
        /// </summary>
        protected virtual OrmResultSet<TReturn> DoQueryInternalAs<TReturn>(DataContext context, Expression<Func<TModel, bool>> query, Func<SqlStatement, SqlStatement> queryModifier = null)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (query == null)
            {
                throw new ArgumentNullException(nameof(query), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            // Convert the query to a domain query so that the object persistence layer can turn the
            // structured LINQ query into a SQL statement
            query = this.ApplyDefaultQueryFilters(query);
            var domainQuery = context.CreateSqlStatement().SelectFrom(typeof(TDbModel), TableMapping.Get(typeof(TDbModel)).Columns.ToArray());
            if (queryModifier != null)
            {
                domainQuery = queryModifier(domainQuery);
            }

            var expression = this.m_modelMapper.MapModelExpression<TModel, TDbModel, bool>(query, false);
            if (expression != null)
            {
                domainQuery.Where<TDbModel>(expression);
            }
            else
            {
                this.m_tracer.TraceVerbose("Will use slow query construction due to complex mapped fields");
                domainQuery = context.GetQueryBuilder(this.m_modelMapper).CreateQuery(query);
            }

            return context.Query<TReturn>(domainQuery.Build());
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
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (model == default(TDbModel))
            {
                throw new ArgumentNullException(nameof(model), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (model.Key == Guid.Empty)
            {
                throw new ArgumentException(nameof(model.Key), this.m_localizationService.GetString(ErrorMessageStrings.NON_IDENTITY_UPDATE));
            }

            // perform
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            try
            {
#endif
                var existing = context.FirstOrDefault<TDbModel>(o => o.Key == model.Key);
                if (existing == null)
                {
                    throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { type = typeof(TModel).Name, id = model.Key }));
                }
                existing.CopyObjectData(model, true);
                return context.Update(existing);
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose("Update {0} took {1}ms", model, sw.ElapsedMilliseconds);
            }
#endif
        }

        /// <summary>
        /// Update associated entities
        /// </summary>
        /// <remarks>
        /// Updates the associated items of <typeparamref name="TModelAssociation"/> such that
        /// <paramref name="data"/>'s associations are updated to match the list
        /// provided in <paramref name="associations"/>
        /// </remarks>
        /// <returns>The effective list of relationships on the <paramref name="data"/></returns>
        protected virtual IEnumerable<TModelAssociation> UpdateModelAssociations<TModelAssociation>(DataContext context, TModel data, IEnumerable<TModelAssociation> associations)
            where TModelAssociation : IdentifiedData, ISimpleAssociation, new()
        {
            if (data == null || data.Key.GetValueOrDefault() == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(IdentifiedData.Key), ErrorMessages.ARGUMENT_NULL);
            }

            // Ensure either the relationship points to (key) (either source or target)
            associations = associations.Select(a =>
            {
                if (a is ITargetedAssociation target && target.TargetEntityKey != data.Key && a.SourceEntityKey != data.Key ||
                    a.SourceEntityKey.GetValueOrDefault() == Guid.Empty) // The target is a target association
                {
                    a.SourceEntityKey = data.Key;
                }
                return a;
            }).ToArray();

            // We now want to fetch the perssitence serivce of this
            var persistenceService = typeof(TModelAssociation).GetRelatedPersistenceService() as IAdoPersistenceProvider<TModelAssociation>;
            if (persistenceService == null)
            {
                throw new DataPersistenceException(String.Format(ErrorMessages.RELATED_OBJECT_NOT_AVAILABLE, typeof(TModelAssociation), typeof(TModel)));
            }

            // Next we want to perform a relationship query to establish what is being loaded and what is being persisted
            var existingKeys = associations.Select(k => k.Key).ToArray();
            var existing = persistenceService.Query(context, o => o.SourceEntityKey == data.Key || existingKeys.Contains(o.Key)).Select(o => o.Key).ToArray();

            // Which are new and which are not?
            var removedRelationships = existing.Where(o => associations.Any(a=>a.Key == o && a.BatchOperation == Core.Model.DataTypes.BatchOperationType.Delete) || !associations.Any(a => a.Key == o)).Select(a =>
            {
                return persistenceService.Delete(context, a.Value, DataPersistenceControlContext.Current?.DeleteMode ?? this.m_configuration.DeleteStrategy);
            });
            var addedRelationships = associations.Where(o => o.BatchOperation != Core.Model.DataTypes.BatchOperationType.Delete && (!o.Key.HasValue || !existing.Any(a => a == o.Key))).Select(a =>
            {
                a = persistenceService.Insert(context, a);
                a.BatchOperation = Core.Model.DataTypes.BatchOperationType.Insert;
                return a;
            });
            var updatedRelationships = associations.Where(o => o.BatchOperation != Core.Model.DataTypes.BatchOperationType.Delete && o.Key.HasValue && existing.Any(a => a == o.Key)).Select(a =>
            {
                a = persistenceService.Update(context, a);
                a.BatchOperation = Core.Model.DataTypes.BatchOperationType.Update;
                return a;
            });

            return updatedRelationships.Union(addedRelationships).Except(removedRelationships).ToArray();
        }

        /// <summary>
        /// Update the internal
        /// </summary>
        /// <param name="associations">The associations which were on the inbound record</param>
        /// <param name="context">The context on which the data is to be inserted/updated</param>
        /// <param name="existingExpression">By default, this uses o=>o.SourceKey == <paramref name="sourceKey"/> , you can specify an alternate here to determine existing records</param>
        /// <param name="sourceKey">The source record (from which the <paramref name="associations"/> point to)</param>
        protected virtual IEnumerable<TAssociativeTable> UpdateInternalAssociations<TAssociativeTable>(DataContext context, Guid sourceKey, IEnumerable<TAssociativeTable> associations, Expression<Func<TAssociativeTable, bool>> existingExpression = null)
            where TAssociativeTable : IDbAssociation, new()
        {
            if (sourceKey == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(sourceKey), ErrorMessages.ARGUMENT_NULL);
            }

            // Ensure the source by locking the IEnumerable
            associations = associations.Select(a =>
            {
                if (a.SourceKey == Guid.Empty)
                {
                    a.SourceKey = sourceKey;
                }
                return a;
            }).ToArray();

            // Existing associations in the database
            TAssociativeTable[] existing = null;
            if (existingExpression != null)
            {
                existing = context.Query<TAssociativeTable>(existingExpression).ToArray();
            }
            else
            {
                existing = context.Query<TAssociativeTable>(o => o.SourceKey == sourceKey).ToArray();
            }

            // Which ones are new?
            var removeRelationships = existing.Where(e => !associations.Any(a => a.Equals(e)));
            var addRelationships = associations.Where(a => !existing.Any(e => e.Equals(a)));

            // First, remove the old
            foreach (var itm in removeRelationships)
            {
                this.m_tracer.TraceVerbose("Will remove {0} of {1}", typeof(TAssociativeTable).Name, itm);
                context.Delete(itm);
            }

            // Next, add the new
            foreach (var itm in addRelationships)
            {
                this.m_tracer.TraceVerbose("Will add {0} of {1}", typeof(TAssociativeTable).Name, itm);
                context.Insert(itm);
            }

            return existing.Where(o => !removeRelationships.Any(r => r.Equals(o))).Union(addRelationships);
        }
    }
}