/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model;
using SanteDB.Persistence.Data.ADO.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using SanteDB.Core.Model.Query;
using System.Diagnostics.Tracing;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Core persistence service which contains helpful functions
    /// </summary>
    public abstract class CorePersistenceService<TModel, TDomain, TQueryReturn> : AdoBasePersistenceService<TModel>
        where TModel : IdentifiedData, new()
        where TDomain : class, new()
    {

        // Query persistence
        protected Core.Services.IQueryPersistenceService m_queryPersistence = ApplicationServiceContext.Current.GetService<Core.Services.IQueryPersistenceService>();

        /// <summary>
        /// Get the order by function
        /// </summary>
        protected virtual SqlStatement AppendOrderBy(SqlStatement rawQuery, ModelSort<TModel>[] orderBy)
        {
            if (orderBy != null)
                foreach (var ob in orderBy)
                {
                    var sorter = m_mapper.MapModelExpression<TModel, TDomain, dynamic>(ob.SortProperty, false);
                    if(sorter != null)
                        rawQuery = rawQuery.OrderBy(sorter, ob.SortOrder);
                }
            return rawQuery;
        }

        /// <summary>
        /// Maps the data to a model instance
        /// </summary>
        /// <returns>The model instance.</returns>
        /// <param name="dataInstance">Data instance.</param>
        public override TModel ToModelInstance(object dataInstance, DataContext context)
        {
            var dInstance = (dataInstance as CompositeResult)?.Values.OfType<TDomain>().FirstOrDefault() ?? dataInstance as TDomain;
            var retVal = m_mapper.MapDomainInstance<TDomain, TModel>(dInstance);
            retVal.LoadAssociations(context);
            this.m_tracer.TraceEvent(EventLevel.Verbose, "Model instance {0} created", dataInstance);

            return retVal;
        }

        /// <summary>
		/// Performthe actual insert.
		/// </summary>
		/// <param name="context">Context.</param>
		/// <param name="data">Data.</param>
		public override TModel InsertInternal(DataContext context, TModel data)
        {
            var domainObject = this.FromModelInstance(data, context) as TDomain;

            domainObject = context.Insert<TDomain>(domainObject);

            if (domainObject is IDbIdentified)
                data.Key = (domainObject as IDbIdentified)?.Key;
            //data.CopyObjectData(this.ToModelInstance(domainObject, context));
            //data.Key = domainObject.Key
            return data;
        }

        /// <summary>
        /// Perform the actual update.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel UpdateInternal(DataContext context, TModel data)
        {
            // Sanity 
            if (data.Key == Guid.Empty)
                throw new AdoFormalConstraintException(AdoFormalConstraintType.NonIdentityUpdate);

            // Map and copy
            var newDomainObject = this.FromModelInstance(data, context) as TDomain;
            context.Update<TDomain>(newDomainObject);
            return data;
        }

        /// <summary>
        /// Performs the actual obsoletion
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel ObsoleteInternal(DataContext context, TModel data)
        {
            if (data.Key == Guid.Empty)
                throw new AdoFormalConstraintException(AdoFormalConstraintType.NonIdentityUpdate);

            context.Delete<TDomain>((TDomain)this.FromModelInstance(data, context));

            return data;
        }

        /// <summary>
        /// Performs the actual query
        /// </summary>
        public override IEnumerable<TModel> QueryInternal(DataContext context, Expression<Func<TModel, bool>> query, Guid queryId, int offset, int? count, out int totalResults, ModelSort<TModel>[] orderBy, bool countResults = true)
        {
            int resultCount = 0;
            var results = this.DoQueryInternal(context, query, queryId, offset, count, out resultCount, orderBy, countResults).ToList();
            totalResults = resultCount;

            if (!this.m_persistenceService.GetConfiguration().SingleThreadFetch)
                return results.AsParallel().Select(o =>
                {
                    var subContext = context;
                    var newSubContext = results.Count() > 1;

                    try
                    {
                        if (newSubContext) subContext = subContext.OpenClonedContext();

                        if (o is Guid)
                            return this.Get(subContext, (Guid)o);
                        else
                            return this.CacheConvert(o, subContext);
                    }
                    catch (Exception e)
                    {
                        this.m_tracer.TraceEvent(EventLevel.Error,  "Error performing sub-query: {0}", e);
                        throw;
                    }
                    finally
                    {
                        if (newSubContext)
                            subContext.Dispose();
                    }
                });
            else
                return results.Select(o =>
                {
                    if (o is Guid)
                        return this.Get(context, (Guid)o);
                    else
                        return this.CacheConvert(o, context);
                });
        }

        /// <summary>
        /// Perform the query 
        /// </summary>
        protected virtual IEnumerable<Object> DoQueryInternal(DataContext context, Expression<Func<TModel, bool>> query, Guid queryId, int offset, int? count, out int totalResults, ModelSort<TModel>[] orderBy, bool incudeCount = true)
        {
#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif

            SqlStatement domainQuery = null;
            try
            {

                // Query has been registered?
                if (queryId != Guid.Empty && this.m_queryPersistence?.IsRegistered(queryId) == true)
                {
                    totalResults = (int)this.m_queryPersistence.QueryResultTotalQuantity(queryId);
                    var resultKeys = this.m_queryPersistence.GetQueryResults(queryId, offset, count.Value);
                    return resultKeys.OfType<Object>();
                }

                // Is obsoletion time already specified?
                if (!query.ToString().Contains("ObsoletionTime") && typeof(BaseEntityData).IsAssignableFrom(typeof(TModel)))
                {
                    var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(BaseEntityData.ObsoletionTime))), Expression.Constant(null));
                    query = Expression.Lambda<Func<TModel, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, obsoletionReference, query.Body), query.Parameters);
                }

                // Domain query
                Type[] selectTypes = { typeof(TQueryReturn) };
                if (selectTypes[0].IsConstructedGenericType)
                    selectTypes = selectTypes[0].GenericTypeArguments;

                domainQuery = context.CreateSqlStatement<TDomain>().SelectFrom(selectTypes);
                var expression = m_mapper.MapModelExpression<TModel, TDomain, bool>(query, false);
                if (expression != null)
                {
                    Type lastJoined = typeof(TDomain);
                    if (typeof(CompositeResult).IsAssignableFrom(typeof(TQueryReturn)))
                        foreach (var p in typeof(TQueryReturn).GenericTypeArguments.Select(o => this.m_persistenceService.GetMapper().MapModelType(o)))
                            if (p != typeof(TDomain))
                            {
                                // Find the FK to join
                                domainQuery.InnerJoin(lastJoined, p);
                                lastJoined = p;
                            }

                    domainQuery.Where<TDomain>(expression);
                }
                else
                {
                    m_tracer.TraceEvent(EventLevel.Verbose, "Will use slow query construction due to complex mapped fields");
                    domainQuery = this.m_persistenceService.GetQueryBuilder().CreateQuery(query, orderBy);
                }

                // Count = 0 means we're not actually fetching anything so just hit the db
                if (count != 0)
                {
                    domainQuery = this.AppendOrderBy(domainQuery, orderBy);

                    // Query id just get the UUIDs in the db
                    if (queryId != Guid.Empty && count != 0)
                    {
                        ColumnMapping pkColumn = null;
                        if (typeof(CompositeResult).IsAssignableFrom(typeof(TQueryReturn)))
                        {
                            foreach (var p in typeof(TQueryReturn).GenericTypeArguments.Select(o => this.m_persistenceService.GetMapper().MapModelType(o)))
                                if (!typeof(DbSubTable).IsAssignableFrom(p) && !typeof(IDbVersionedData).IsAssignableFrom(p))
                                {
                                    pkColumn = TableMapping.Get(p).Columns.SingleOrDefault(o => o.IsPrimaryKey);
                                    break;
                                }
                        }
                        else
                            pkColumn = TableMapping.Get(typeof(TQueryReturn)).Columns.SingleOrDefault(o => o.IsPrimaryKey);

                        var keyQuery = this.m_persistenceService.GetQueryBuilder().CreateQuery(query, orderBy, pkColumn).Build();

                        var resultKeys = context.Query<Guid>(keyQuery.Build());

                        //ApplicationServiceContext.Current.GetService<IThreadPoolService>().QueueNonPooledWorkItem(a => this.m_queryPersistence?.RegisterQuerySet(queryId.ToString(), resultKeys.Select(o => new Guid(o)).ToArray(), query), null);
                        // Another check
                        this.m_queryPersistence?.RegisterQuerySet(queryId, resultKeys.Take(1000).ToArray(), query, resultKeys.Count());

                        ApplicationServiceContext.Current.GetService<IThreadPoolService>().QueueNonPooledWorkItem(o =>
                        {
                            int ofs = 1000;
                            var rkeys = o as Guid[];
                            while (ofs < rkeys.Length)
                            {
                                this.m_queryPersistence.AddResults(queryId, rkeys.Skip(ofs).Take(1000).ToArray());
                                ofs += 1000;
                            }
                        }, resultKeys.ToArray());

                        if (incudeCount)
                            totalResults = (int)resultKeys.Count();
                        else
                            totalResults = 0;

                        var retVal = resultKeys.Skip(offset);
                        if (count.HasValue)
                            retVal = retVal.Take(count.Value);
                        return retVal.OfType<Object>();
                    }
                    else if (incudeCount)
                    {
                        totalResults = context.Count(domainQuery);
                        if (totalResults == 0)
                            return new List<Object>();
                    }
                    else
                        totalResults = 0;

                    if (offset > 0)
                        domainQuery.Offset(offset);
                    if (count.HasValue)
                        domainQuery.Limit(count.Value);

                    return this.DomainQueryInternal<TQueryReturn>(context, domainQuery, ref totalResults).OfType<Object>();
                }
                else
                {
                    totalResults = context.Count(domainQuery);
                    return new List<Object>();
                }
            }
            catch (Exception ex)
            {
                if(domainQuery != null)
                    this.m_tracer.TraceEvent(EventLevel.Error,  context.GetQueryLiteral(domainQuery.Build()));
                context.Dispose(); // No longer important

                throw;
            }
#if DEBUG
            finally
            {
                sw.Stop();
            }
#endif
        }

        /// <summary>
        /// Perform a domain query
        /// </summary>
        protected IEnumerable<TResult> DomainQueryInternal<TResult>(DataContext context, SqlStatement domainQuery, ref int totalResults)
        {

            // Build and see if the query already exists on the stack???
            domainQuery = domainQuery.Build();
            var cachedQueryResults = context.CacheQuery(domainQuery);
            if (cachedQueryResults != null)
            {
                totalResults = cachedQueryResults.Count();
                return cachedQueryResults.OfType<TResult>();
            }

            var results = context.Query<TResult>(domainQuery).ToList();

            // Cache query result
            context.AddQuery(domainQuery, results.OfType<Object>());
            return results;

        }


        /// <summary>
        /// Build source query
        /// </summary>
        protected Expression<Func<TAssociation, bool>> BuildSourceQuery<TAssociation>(Guid sourceId) where TAssociation : ISimpleAssociation
        {
            return o => o.SourceEntityKey == sourceId;
        }

        /// <summary>
        /// Build source query
        /// </summary>
        protected Expression<Func<TAssociation, bool>> BuildSourceQuery<TAssociation>(Guid sourceId, decimal? versionSequenceId) where TAssociation : IVersionedAssociation
        {
            if (versionSequenceId == null)
                return o => o.SourceEntityKey == sourceId && o.ObsoleteVersionSequenceId == null;
            else
                return o => o.SourceEntityKey == sourceId && o.EffectiveVersionSequenceId <= versionSequenceId && (o.ObsoleteVersionSequenceId == null || o.ObsoleteVersionSequenceId > versionSequenceId);
        }

        /// <summary>
        /// Tru to load from cache
        /// </summary>
        protected virtual TModel CacheConvert(Object o, DataContext context)
        {
            if (o == null) return null;

            var cacheService = new AdoPersistenceCache(context);

            var idData = (o as CompositeResult)?.Values.OfType<IDbIdentified>().FirstOrDefault() ?? o as IDbIdentified;
            var objData = (o as CompositeResult)?.Values.OfType<IDbBaseData>().FirstOrDefault() ?? o as IDbBaseData;
            if (objData?.ObsoletionTime != null || idData == null || idData.Key == Guid.Empty)
                return this.ToModelInstance(o, context);
            else
            {
                var cacheItem = cacheService?.GetCacheItem<TModel>(idData?.Key ?? Guid.Empty);
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
                    cacheItem = this.ToModelInstance(o, context);
                    if (context.Transaction == null)
                        cacheService?.Add(cacheItem);
                }
                return cacheItem;
            }
        }

        /// <summary>
        /// Froms the model instance.
        /// </summary>
        /// <returns>The model instance.</returns>
        /// <param name="modelInstance">Model instance.</param>
        /// <param name="context">Context.</param>
        public override object FromModelInstance(TModel modelInstance, DataContext context)
        {
            return m_mapper.MapModelInstance<TModel, TDomain>(modelInstance);
        }

        /// <summary>
        /// Update associated items
        /// </summary>
        protected virtual void UpdateAssociatedItems<TAssociation, TDomainAssociation>(IEnumerable<TAssociation> storage, TModel source, DataContext context)
            where TAssociation : IdentifiedData, ISimpleAssociation, new()
            where TDomainAssociation : DbAssociation, new()
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TAssociation>>() as AdoBasePersistenceService<TAssociation>;
            if (persistenceService == null)
            {
                this.m_tracer.TraceEvent(EventLevel.Informational,  "Missing persister for type {0}", typeof(TAssociation).Name);
                return;
            }
            // Ensure the source key is set
            foreach (var itm in storage)
                if (itm.SourceEntityKey == Guid.Empty ||
                    itm.SourceEntityKey == null)
                    itm.SourceEntityKey = source.Key;

            // Get existing
            var existing = context.Query<TDomainAssociation>(o => o.SourceKey == source.Key);
            // Remove old associations
            var obsoleteRecords = existing.Where(o => !storage.Any(ecn => ecn.Key == o.Key));
            foreach (var del in obsoleteRecords) // Obsolete records = delete as it is non-versioned association
                context.Delete(del);

            // Update those that need it
            var updateRecords = storage.Where(o => existing.Any(ecn => ecn.Key == o.Key && o.Key != Guid.Empty));
            foreach (var upd in updateRecords)
                persistenceService.UpdateInternal(context, upd);

            // Insert those that do not exist
            var insertRecords = storage.Where(o => !existing.Any(ecn => ecn.Key == o.Key));
            foreach (var ins in insertRecords)
                persistenceService.InsertInternal(context, ins);

        }


    }
}
