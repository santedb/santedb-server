/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
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
using System.Diagnostics.Tracing;
using System.Threading;
using SanteDB.Core.Exceptions;
using System.Data.Common;
using System.Security.Principal;
using SanteDB.Core.Security;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Core persistence service which contains helpful functions
    /// </summary>
    public abstract class CorePersistenceService<TModel, TDomain, TQueryReturn> : AdoBasePersistenceService<TModel>, IBulkDataPersistenceService, IAdoCopyProvider
        where TModel : IdentifiedData, new()
        where TDomain : class, new()
    {

        /// <summary>
        /// Constructor
        /// </summary>
        public CorePersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        // Query persistence
        protected Core.Services.IQueryPersistenceService m_queryPersistence = ApplicationServiceContext.Current.GetService<Core.Services.IQueryPersistenceService>();

        /// <summary>
        /// Get the order by function
        /// </summary>
        protected virtual SqlStatement AppendOrderBy(SqlStatement rawQuery, ModelSort<TModel>[] orderBy)
        {
            if (orderBy != null && orderBy.Length > 0)
                foreach (var ob in orderBy)
                {
                    var sorter = this.m_settingsProvider.GetMapper().MapModelExpression<TModel, TDomain, dynamic>(ob.SortProperty, false);
                    if (sorter != null)
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
            var retVal = this.m_settingsProvider.GetMapper().MapDomainInstance<TDomain, TModel>(dInstance);
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
        protected virtual IEnumerable<Object> DoQueryInternal(DataContext context, Expression<Func<TModel, bool>> primaryQuery, Guid queryId, int offset, int? count, out int totalResults, ModelSort<TModel>[] orderBy, bool includeCount = true, bool overrideFuzzyTotalSetting = false)
        {
#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif
            // Query has been registered?
            if (queryId != Guid.Empty && this.m_queryPersistence?.IsRegistered(queryId) == true)
                return this.GetStoredQueryResults(queryId, offset, count, out totalResults);

            OrmResultSet<TQueryReturn> retVal = null;
            Expression<Func<TModel, bool>>[] queries = new Expression<Func<TModel, bool>>[] { primaryQuery };
            // Are we intersecting?
            if (context.Data.TryGetValue("UNION", out object others) &&
                others is Expression<Func<TModel, bool>>[])
            {
                context.Data.Remove("UNION");
                queries = queries.Concat((Expression<Func<TModel, bool>>[])others).ToArray();
            }

            try
            {
                // Fetch queries
                foreach (var q in queries)
                {
                    var query = q;
                    SqlStatement domainQuery = null;


                    // Is obsoletion time already specified? If so and the entity is an iversioned entity we don't want obsolete data coming back
                    var queryString = query.ToString();
                    if (!queryString.Contains("ObsoletionTime") && typeof(IVersionedEntity).IsAssignableFrom(typeof(TModel)) && !queryString.Contains("VersionKey"))
                    {
                        var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(BaseEntityData.ObsoletionTime))), Expression.Constant(null));
                        //var obsoletionReference = Expression.MakeUnary(ExpressionType.Not, Expression.MakeMemberAccess(Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(BaseEntityData.ObsoletionTime))), typeof(Nullable<DateTimeOffset>).GetProperty("HasValue")), typeof(bool));
                        query = Expression.Lambda<Func<TModel, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, obsoletionReference, query.Body), query.Parameters);
                    }
                    else if (!queryString.Contains("ObsoleteVersionSequenceId") && typeof(IVersionedAssociation).IsAssignableFrom(typeof(TModel)))
                    {
                        var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(IVersionedAssociation.ObsoleteVersionSequenceId))), Expression.Constant(null));
                        //var obsoletionReference = Expression.MakeUnary(ExpressionType.Not, Expression.MakeMemberAccess(Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(IVersionedAssociation.ObsoleteVersionSequenceId))), typeof(Nullable<decimal>).GetProperty("HasValue")), typeof(bool));
                        query = Expression.Lambda<Func<TModel, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, obsoletionReference, query.Body), query.Parameters);
                    }

                    // Domain query
                    Type[] selectTypes = { typeof(TQueryReturn) };
                    if (selectTypes[0].IsConstructedGenericType)
                        selectTypes = selectTypes[0].GenericTypeArguments;

                    domainQuery = context.CreateSqlStatement<TDomain>().SelectFrom(selectTypes);
                    var expression = this.m_settingsProvider.GetMapper().MapModelExpression<TModel, TDomain, bool>(query, false);
                    if (expression != null)
                    {
                        Type lastJoined = typeof(TDomain);
                        if (typeof(CompositeResult).IsAssignableFrom(typeof(TQueryReturn)))
                            foreach (var p in typeof(TQueryReturn).GenericTypeArguments.Select(o => this.m_settingsProvider.GetMapper().MapModelType(o)))
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
                        if (q == queries.Last())
                            domainQuery = this.m_settingsProvider.GetQueryBuilder().CreateQuery(query, orderBy);
                        else
                            domainQuery = this.m_settingsProvider.GetQueryBuilder().CreateQuery(query);
                    }

                    if (retVal == null)
                        retVal = this.DomainQueryInternal<TQueryReturn>(context, domainQuery);
                    else
                        retVal = retVal.Union(this.DomainQueryInternal<TQueryReturn>(context, domainQuery));
                }

                this.AppendOrderBy(retVal.Statement, orderBy);

                // Count = 0 means we're not actually fetching anything so just hit the db
                if (count != 0)
                {

                    if (context.Data.TryGetValue("principal", out object pRaw))
                    {
                        var currentPrincipal = pRaw as IPrincipal;
                        includeCount = includeCount && currentPrincipal != AuthenticationContext.SystemPrincipal;
                        overrideFuzzyTotalSetting = currentPrincipal == AuthenticationContext.SystemPrincipal;
                    }

                    // Stateful query identifier = We need to add query results
                    if (queryId != Guid.Empty && ApplicationServiceContext.Current.GetService<IQueryPersistenceService>() != null)
                    {
                        // Create on a separate thread the query results
                        var keys = retVal.Keys<Guid>().ToArray();
                        totalResults = keys.Length;
                        this.m_queryPersistence?.RegisterQuerySet(queryId, keys, queries, totalResults);

                    }
                    else if (count.HasValue && includeCount && !overrideFuzzyTotalSetting && !this.m_settingsProvider.GetConfiguration().UseFuzzyTotals) // Get an exact total
                    {
                        totalResults = retVal.Count();
                    }
                    else
                        totalResults = 0;

                    // Fuzzy totals - This will only fetch COUNT + 1 as the total results
                    if (count.HasValue)
                    {
                        if ((overrideFuzzyTotalSetting || this.m_settingsProvider.GetConfiguration().UseFuzzyTotals) && totalResults == 0)
                        {
                            var fuzzResults = retVal.Skip(offset).Take(count.Value + 1).OfType<Object>().ToList();
                            totalResults = offset + fuzzResults.Count();
                            return fuzzResults.Take(count.Value);
                        }
                        else
                            return retVal.Skip(offset).Take(count.Value).OfType<Object>();
                    }
                    else
                        return retVal.Skip(offset).OfType<Object>();

                }
                else
                {
                    totalResults = retVal.Count();
                    return new List<Object>();
                }
            }
            catch (Exception ex)
            {
                this.m_tracer.TraceError("Error performing underlying query: {0}", ex);
                if (retVal != null)
                    this.m_tracer.TraceEvent(EventLevel.Error, context.GetQueryLiteral(retVal.ToSqlStatement()));
                context.Dispose(); // No longer important
                throw new DataPersistenceException($"Error executing query {String.Join("UNION", queries.Select(o => o.ToString()))}", ex);
            }
#if DEBUG
            finally
            {
                sw.Stop();
            }
#endif
        }

        /// <summary>
        /// Get stored query results
        /// </summary>
        protected IEnumerable<object> GetStoredQueryResults(Guid queryId, int offset, int? count, out int totalResults)
        {
            totalResults = (int)this.m_queryPersistence.QueryResultTotalQuantity(queryId);
            var keyResults = this.m_queryPersistence.GetQueryResults(queryId, offset, count.Value);
            return keyResults.OfType<Object>();
        }


        /// <summary>
        /// Perform a domain query
        /// </summary>
        protected OrmResultSet<TResult> DomainQueryInternal<TResult>(DataContext context, SqlStatement domainQuery)
        {

            // Build and see if the query already exists on the stack???
            domainQuery = domainQuery.Build();

            var results = context.Query<TResult>(domainQuery);

            // Cache query result
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
                return o => o.SourceEntityKey == sourceId && o.EffectiveVersionSequenceId <= versionSequenceId && o.ObsoleteVersionSequenceId == null;
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
            return this.m_settingsProvider.GetMapper().MapModelInstance<TModel, TDomain>(modelInstance);
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
                this.m_tracer.TraceEvent(EventLevel.Informational, "Missing persister for type {0}", typeof(TAssociation).Name);
                return;
            }
            // Ensure the source key is set
            foreach (var itm in storage)
                if (itm.SourceEntityKey == Guid.Empty ||
                    itm.SourceEntityKey == null)
                    itm.SourceEntityKey = source.Key;

            // Get existing
            var existing = context.Query<TDomainAssociation>(o => o.SourceKey == source.Key).ToList();
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

        /// <summary>
        /// Obsolete the object
        /// </summary>
        public virtual void Obsolete(TransactionMode transactionMode, IPrincipal principal, params Guid[] keysToObsolete)
        {
            // Obsolete object
            using (var connection = this.m_settingsProvider.GetConfiguration().Provider.GetWriteConnection())
            {
                connection.Open();
                using (var tx = connection.BeginTransaction())
                    try
                    {
                        var provenanceId = connection.EstablishProvenance(principal, null);

                        this.BulkObsoleteInternal(connection, keysToObsolete);

                        if (transactionMode == TransactionMode.Commit)
                            tx.Commit();
                    }
                    catch (DbException e)
                    {
                        tx?.Rollback();
                        throw new DataPersistenceException($"Error bulk obsoleting data", this.TranslateDbException(e));
                    }
                    catch (Exception e)
                    {
                        tx?.Rollback();
                        throw new DataPersistenceException($"Error bulk obsoleting data", e);
                    }
            }
        }

        /// <summary>
        /// Perform the bulk obsoletion operation
        /// </summary>
        protected abstract void BulkObsoleteInternal(DataContext context, Guid[] keysToObsolete);

        /// <summary>
        /// Purge the specified keys
        /// </summary>
        public virtual void Purge(TransactionMode transactionMode, IPrincipal principal, params Guid[] keysToPurge)
        {
            // Purge object
            using (var connection = this.m_settingsProvider.GetConfiguration().Provider.GetWriteConnection())
            {
                connection.Open();
                using (var tx = connection.BeginTransaction())
                    try
                    {
                        connection.EstablishProvenance(principal, null);
                        this.BulkPurgeInternal(connection, keysToPurge);
                        if (transactionMode == TransactionMode.Commit)
                            tx.Commit();
                    }
                    catch (DbException e)
                    {
                        tx?.Rollback();
                        throw new DataPersistenceException($"Error bulk obsoleting data", this.TranslateDbException(e));
                    }
                    catch (Exception e)
                    {
                        tx?.Rollback();
                        throw new DataPersistenceException($"Error bulk obsoleting data", e);
                    }
            }
        }

        /// <summary>
        /// Purge the specified object 
        /// </summary>
        protected abstract void BulkPurgeInternal(DataContext connection, Guid[] keysToPurge);

        /// <summary>
        /// Purge the specified object 
        /// </summary>
        protected abstract void BulkPurgeInternal(DataContext connection, Expression<Func<TModel, bool>> expression);

        /// <summary>
        /// Purge keys matching the specified pattern
        /// </summary>
        public virtual void Purge(TransactionMode mode, IPrincipal principal, Expression query)
        {
            if (query is Expression<Func<TModel, bool>> castQuery)
                using (var connection = this.m_settingsProvider.GetConfiguration().Provider.GetWriteConnection())
                {
                    connection.Open();
                    try
                    {
                        this.BulkPurgeInternal(connection, castQuery);
                    }
                    catch (DbException e)
                    {
                        throw new DataPersistenceException($"Error bulk purging data", this.TranslateDbException(e));
                    }
                    catch (Exception e)
                    {
                        throw new DataPersistenceException($"Error bulk purging data", e);
                    }
                }
            else
                throw new ArgumentException($"Expression must be of type Expression<Func<{typeof(TModel).Name},bool>>", nameof(query));
        }

        /// <summary>
        /// Query the specified keys only
        /// </summary>
        public virtual IEnumerable<Guid> QueryKeys(Expression query, int offset, int? count, out int totalResults)
        {
            if (query is Expression<Func<TModel, bool>> castQuery)
                using (var connection = this.m_settingsProvider.GetConfiguration().Provider.GetReadonlyConnection())
                {
                    connection.Open();
                    try
                    {
                        return this.QueryKeysInternal(connection, castQuery, offset, count, out totalResults);
                    }
                    catch (DbException e)
                    {
                        throw new DataPersistenceException($"Error bulk query data", this.TranslateDbException(e));
                    }
                    catch (Exception e)
                    {
                        throw new DataPersistenceException($"Error bulk query data", e);
                    }
                }
            else
                throw new ArgumentException($"Expression must be of type Expression<Func<{typeof(TModel).Name},bool>>", nameof(query));
        }


        /// <summary>
        /// Perform the query for bulk keys with an open context
        /// </summary>
        protected abstract IEnumerable<Guid> QueryKeysInternal(DataContext context, Expression<Func<TModel, bool>> query, int offset, int? count, out int totalResults);

        /// <summary>
        /// Copy the data from <paramref name="fromContext"/> to <paramref name="toContext"/>
        /// </summary>
        public abstract void Copy(Guid[] keysToCopy, DataContext fromContext, DataContext toContext);

        /// <summary>
        /// Copy the specified keys from this provider to the context
        /// </summary>
        public void CopyTo(Guid[] keysToCopy, DataContext toContext)
        {
            try
            {
                // Obsolete object
                using (var connection = this.m_settingsProvider.GetConfiguration().Provider.GetReadonlyConnection())
                {
                    connection.Open();
                    this.Copy(keysToCopy, connection, toContext);
                }
            }
            catch (DbException e)
            {
                throw new DataPersistenceException("Could copy keys to data context", this.TranslateDbException(e));
            }
        }


    }
}
