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
using SanteDB.Core.Event;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model;
using SanteDB.Persistence.Data.ADO.Data.Model.Acts;
using SanteDB.Persistence.Data.ADO.Data.Model.Concepts;
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;
using SanteDB.Persistence.Data.ADO.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Versioned domain data
    /// </summary>
    public abstract class VersionedDataPersistenceService<TModel, TDomain, TDomainKey> : BaseDataPersistenceService<TModel, TDomain, CompositeResult<TDomain, TDomainKey>>
        where TDomain : class, IDbVersionedData, new()
        where TModel : VersionedEntityData<TModel>, new()
        where TDomainKey : IDbIdentified, new()
    {

        /// <summary>
        /// Insert the data
        /// </summary>
        public override TModel InsertInternal(DataContext context, TModel data)
        {
            // first we map the TDataKey entity
            var nonVersionedPortion = m_mapper.MapModelInstance<TModel, TDomainKey>(data);

            // Domain object
            var domainObject = this.FromModelInstance(data, context) as TDomain;

            // First we must assign non versioned portion data
            if (nonVersionedPortion.Key == Guid.Empty &&
                domainObject.Key != Guid.Empty)
                nonVersionedPortion.Key = domainObject.Key;

            if (nonVersionedPortion.Key == null ||
                nonVersionedPortion.Key == Guid.Empty)
            {
                data.Key = Guid.NewGuid();
                domainObject.Key = nonVersionedPortion.Key = data.Key.Value;
            }
            if (domainObject.VersionKey == null ||
                domainObject.VersionKey == Guid.Empty)
            {
                data.VersionKey = Guid.NewGuid();
                domainObject.VersionKey = data.VersionKey.Value;
            }

            // Now we want to insert the non versioned portion first
            nonVersionedPortion = context.Insert(nonVersionedPortion);

            // Ensure created by exists
            data.CreatedByKey = domainObject.CreatedByKey = context.ContextId;

            if (data.CreationTime == DateTimeOffset.MinValue || data.CreationTime.Year < 100)
                data.CreationTime = DateTimeOffset.Now;
            domainObject.CreationTime = data.CreationTime;
            domainObject.VersionSequenceId = null;
            domainObject = context.Insert(domainObject);
            data.VersionSequence = domainObject.VersionSequenceId;
            data.VersionKey = domainObject.VersionKey;
            data.Key = domainObject.Key;
            data.CreationTime = (DateTimeOffset)domainObject.CreationTime;
            return data;

        }

        /// <summary>
        /// Update the data with new version information
        /// </summary>
        public override TModel UpdateInternal(DataContext context, TModel data)
        {
            if (data.Key == Guid.Empty)
                throw new AdoFormalConstraintException(AdoFormalConstraintType.NonIdentityUpdate);

            // This is technically an insert and not an update
            SqlStatement currentVersionQuery = context.CreateSqlStatement<TDomain>().SelectFrom()
                .Where(o => o.Key == data.Key && !o.ObsoletionTime.HasValue)
                .OrderBy<TDomain>(o => o.VersionSequenceId, Core.Model.Map.SortOrderType.OrderByDescending);

            var existingObject = context.FirstOrDefault<TDomain>(currentVersionQuery); // Get the last version (current)
            var nonVersionedObect = context.FirstOrDefault<TDomainKey>(o => o.Key == data.Key);

            if (existingObject == null)
                throw new KeyNotFoundException(data.Key.ToString());
            else if ((existingObject as IDbReadonly)?.IsReadonly == true ||
                (nonVersionedObect as IDbReadonly)?.IsReadonly == true)
                throw new AdoFormalConstraintException(AdoFormalConstraintType.UpdatedReadonlyObject);

            // Are we re-classing this object?
            nonVersionedObect.CopyObjectData((object)data, false, true);

            // Map existing
            var storageInstance = this.FromModelInstance(data, context);

            // Create a new version
            var newEntityVersion = new TDomain();
            newEntityVersion.CopyObjectData(storageInstance);

            // Client did not change on update, so we need to update!!!
            if (!data.VersionKey.HasValue ||
               data.VersionKey.Value == existingObject.VersionKey ||
               context.Any<TDomain>(o => o.VersionKey == data.VersionKey))
                data.VersionKey = newEntityVersion.VersionKey = Guid.NewGuid();

            data.VersionSequence = newEntityVersion.VersionSequenceId = null;
            newEntityVersion.Key = data.Key.Value;
            data.PreviousVersionKey = newEntityVersion.ReplacesVersionKey = existingObject.VersionKey;
            data.CreatedByKey = newEntityVersion.CreatedByKey = context.ContextId;
            // Obsolete the old version 
            existingObject.ObsoletedByKey = context.ContextId;
            existingObject.ObsoletionTime = DateTimeOffset.Now;
            newEntityVersion.CreationTime = DateTimeOffset.Now;

            context.Update(existingObject);

            newEntityVersion = context.Insert<TDomain>(newEntityVersion);
            nonVersionedObect = context.Update<TDomainKey>(nonVersionedObect);

            // Pull database generated fields
            data.VersionSequence = newEntityVersion.VersionSequenceId;
            data.CreationTime = newEntityVersion.CreationTime;

            return data;
            //return base.Update(context, data);
        }


        /// <summary>
        /// Order by
        /// </summary>
        /// <param name="rawQuery"></param>
        /// <returns></returns>
        protected override SqlStatement AppendOrderBy(SqlStatement rawQuery, ModelSort<TModel>[] orderBy)
        {
            rawQuery = base.AppendOrderBy(rawQuery, orderBy);
            return rawQuery.OrderBy<TDomain>(o => o.VersionSequenceId, Core.Model.Map.SortOrderType.OrderByDescending);
        }

        /// <summary>
        /// Query internal for versioned data elements
        /// </summary>
        protected override IEnumerable<Object> DoQueryInternal(DataContext context, Expression<Func<TModel, bool>> primaryQuery, Guid queryId, int offset, int? count, out int totalResults, ModelSort<TModel>[] orderBy, bool countResults = true, bool overrideFuzzyTotalSetting = false)
        {

#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif

            // Queries to be performed
            OrmResultSet<CompositeResult<TDomain, TDomainKey>> retVal = null;
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
                // Execute queries
                foreach (var q in queries)
                {
                    var query = q;
                    // Is obsoletion time already specified? (this is important for versioned objects if we want to get the most current version of the object)
                    if (!query.ToString().Contains("ObsoletionTime") && !query.ToString().Contains("VersionKey"))
                    {
                        var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(BaseEntityData.ObsoletionTime))), Expression.Constant(null));
                        query = Expression.Lambda<Func<TModel, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, obsoletionReference, query.Body), query.Parameters);
                    }


                    // Query has been registered?
                    if (queryId != Guid.Empty && this.m_queryPersistence?.IsRegistered(queryId) == true)
                        return this.GetStoredQueryResults(queryId, offset, count, out totalResults);

                    SqlStatement domainQuery = null;
                    var expr = m_mapper.MapModelExpression<TModel, TDomain, bool>(query, false);
                    if (expr != null)
                        domainQuery = context.CreateSqlStatement<TDomain>().SelectFrom(typeof(TDomain), typeof(TDomainKey))
                            .InnerJoin<TDomain, TDomainKey>(o => o.Key, o => o.Key)
                            .Where<TDomain>(expr).Build();
                    else
                        domainQuery = this.m_persistenceService.GetQueryBuilder().CreateQuery(query).Build();

                    // Create or extend queries
                    if (retVal == null)
                        retVal = this.DomainQueryInternal<CompositeResult<TDomain, TDomainKey>>(context, domainQuery);
                    else
                        retVal = retVal.Union(this.DomainQueryInternal<CompositeResult<TDomain, TDomainKey>>(context, domainQuery));
                }

                // HACK: More than one query which indicates union was used, we need to wrap in a select statement to be adherent to SQL standard on Firebird and PSQL
                if (queries.Count() > 1)
                {
                    var query = this.AppendOrderBy(context.CreateSqlStatement("SELECT * FROM (").Append(retVal.ToSqlStatement()).Append(") AS domain_query "), orderBy);
                    retVal = this.DomainQueryInternal<CompositeResult<TDomain, TDomainKey>>(context, query);
                }
                else
                {
                    this.AppendOrderBy(retVal.Statement, orderBy);
                }

                // Only perform count
                if (count == 0)
                {
                    totalResults = retVal.Count();
                    return new List<CompositeResult<TDomain, TDomainKey>>();
                }
                else
                {

                    if (queryId != Guid.Empty && ApplicationContext.Current.GetService<IQueryPersistenceService>() != null)
                    {
                        var keys = retVal.Keys<Guid>(false).ToArray();
                        totalResults = keys.Count();
                        this.m_queryPersistence?.RegisterQuerySet(queryId, keys, queries, totalResults);
                    }
                    else if (count.HasValue && countResults && !m_configuration.UseFuzzyTotals)
                        totalResults = retVal.Count();
                    else
                        totalResults = 0;

                    // Fuzzy totals - This will only fetch COUNT + 1 as the total results
                    if (count.HasValue)
                    {
                        if ((overrideFuzzyTotalSetting || m_configuration.UseFuzzyTotals) && totalResults == 0)
                        {
                            var fuzzResults = retVal.Skip(offset).Take(count.Value + 1).OfType<Object>().ToList();
                            totalResults = fuzzResults.Count();
                            return fuzzResults.Take(count.Value);
                        }
                        else // We already counted as part of the queryId so no need to take + 1
                            return retVal.Skip(offset).Take(count.Value).OfType<Object>();
                    }
                    else
                        return retVal.Skip(offset).OfType<Object>();
                }
            }
            catch (Exception ex)
            {
                if (retVal != null)
                    this.m_tracer.TraceEvent(EventLevel.Error, context.GetQueryLiteral(retVal.ToSqlStatement()));
                context.Dispose(); // No longer important

                throw new DataPersistenceException("Error executing query" , ex);
            }
#if DEBUG
            finally
            {
                sw.Stop();
            }
#endif

        }

        /// <summary>
        /// Perform a version aware get
        /// </summary>
        internal override TModel Get(DataContext context, Guid key)
        {
            // Attempt to get a cahce item
            var cacheService = new AdoPersistenceCache(context);
            var retVal = cacheService.GetCacheItem<TModel>(key);
            if (retVal != null)
                return retVal;
            else
            {
                var domainQuery = context.CreateSqlStatement<TDomain>().SelectFrom(typeof(TDomain), typeof(TDomainKey))
                    .InnerJoin<TDomain, TDomainKey>(o => o.Key, o => o.Key)
                    .Where<TDomain>(o => o.Key == key && o.ObsoletionTime == null)
                    .OrderBy<TDomain>(o => o.VersionSequenceId, Core.Model.Map.SortOrderType.OrderByDescending);
                return this.CacheConvert(context.FirstOrDefault<CompositeResult<TDomain, TDomainKey>>(domainQuery), context);
            }
        }

        /// <summary>
        /// Gets the specified object
        /// </summary>
        public override TModel Get(Guid containerId, Guid? versionId, bool loadFast, IPrincipal principal = null)
        {
            var tr = 0;

            if (containerId != Guid.Empty)
            {

                var cacheItem = ApplicationServiceContext.Current.GetService<IDataCachingService>()?.GetCacheItem<TModel>(containerId) as TModel;
                if (cacheItem != null && (cacheItem.VersionKey.HasValue && versionId == cacheItem.VersionKey.Value || versionId == Guid.Empty) &&
                    (loadFast && cacheItem.LoadState >= LoadState.PartialLoad || !loadFast && cacheItem.LoadState == LoadState.FullLoad))
                    return cacheItem;
            }

#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif

            DataRetrievingEventArgs<TModel> preArgs = new DataRetrievingEventArgs<TModel>(containerId, versionId, principal);
            this.FireRetrieving(preArgs);
            if (preArgs.Cancel)
            {
                this.m_tracer.TraceEvent(EventLevel.Warning, "Pre-Event handler indicates abort retrieve {0}", containerId);
                return preArgs.Result;
            }

            // Query object
            using (var connection = m_configuration.Provider.GetReadonlyConnection())
                try
                {
                    connection.Open();
                    this.m_tracer.TraceEvent(EventLevel.Verbose, "GET {0}", containerId);

                    TModel retVal = null;
                    if (loadFast)
                        connection.LoadState = LoadState.PartialLoad;
                    else
                        connection.LoadState = LoadState.FullLoad;

                    // Get most recent version
                    if (versionId.GetValueOrDefault() == Guid.Empty)
                        retVal = this.Get(connection, containerId);
                    else
                        retVal = this.QueryInternal(connection, o => o.Key == containerId && o.VersionKey == versionId && o.ObsoletionTime == null || o.ObsoletionTime != null, Guid.Empty, 0, 1, out tr, null).FirstOrDefault();

                    var postData = new DataRetrievedEventArgs<TModel>(retVal, principal);
                    this.FireRetrieved(postData);

                    // Add to cache
                    foreach (var d in connection.CacheOnCommit)
                        ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Add(d);
                    return retVal;

                }
                catch (NotSupportedException e)
                {
                    throw new DataPersistenceException("Cannot perform LINQ query", e);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error, "Error : {0}", e);
                    throw new DataPersistenceException($"Cannot retrieve object {containerId}", e);
                }
                finally
                {
#if DEBUG
                    sw.Stop();
                    this.m_tracer.TraceEvent(EventLevel.Verbose, "Retrieve took {0} ms", sw.ElapsedMilliseconds);
#endif
                }

        }

        /// <summary>
        /// Update versioned association items
        /// </summary>
        internal virtual void UpdateVersionedAssociatedItems<TAssociation, TDomainAssociation>(IEnumerable<TAssociation> storage, TModel source, DataContext context)
            where TAssociation : VersionedAssociation<TModel>, new()
            where TDomainAssociation : class, IDbVersionedAssociation, IDbIdentified, new()
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TAssociation>>() as AdoBasePersistenceService<TAssociation>;
            if (persistenceService == null)
            {
                this.m_tracer.TraceEvent(EventLevel.Informational, "Missing persister for type {0}", typeof(TAssociation).Name);
                return;
            }

            Dictionary<Guid, Int32> sourceVersionMaps = new Dictionary<Guid, Int32>();

            // Ensure the source key is set
            foreach (var itm in storage)
                if (itm.SourceEntityKey.GetValueOrDefault() == Guid.Empty ||
                    itm.SourceEntityKey == null)
                    itm.SourceEntityKey = source.Key;
                else if (itm.SourceEntityKey != source.Key && !sourceVersionMaps.ContainsKey(itm.SourceEntityKey ?? Guid.Empty)) // The source comes from somewhere else
                {

                    SqlStatement versionQuery = null;
                    // Get the current tuple 
                    IDbVersionedData currentVersion = null;

                    // We need to figure out what the current version of the source item is ... 
                    // Since this is a versioned association an a versioned association only exists between Concept, Act, or Entity
                    if (itm is VersionedAssociation<Concept>)
                    {
                        versionQuery = context.CreateSqlStatement<DbConceptVersion>().SelectFrom().Where(o => o.VersionKey == itm.SourceEntityKey && !o.ObsoletionTime.HasValue).OrderBy<DbConceptVersion>(o => o.VersionSequenceId, Core.Model.Map.SortOrderType.OrderByDescending);
                        currentVersion = context.FirstOrDefault<DbConceptVersion>(versionQuery);
                    }
                    else if (itm is VersionedAssociation<Act>)
                    {
                        versionQuery = context.CreateSqlStatement<DbActVersion>().SelectFrom().Where(o => o.Key == itm.SourceEntityKey && !o.ObsoletionTime.HasValue).OrderBy<DbActVersion>(o => o.VersionSequenceId, Core.Model.Map.SortOrderType.OrderByDescending);
                        currentVersion = context.FirstOrDefault<DbActVersion>(versionQuery);
                    }
                    else if (itm is VersionedAssociation<Entity>)
                    {
                        versionQuery = context.CreateSqlStatement<DbEntityVersion>().SelectFrom().Where(o => o.Key == itm.SourceEntityKey && !o.ObsoletionTime.HasValue).OrderBy<DbEntityVersion>(o => o.VersionSequenceId, Core.Model.Map.SortOrderType.OrderByDescending);
                        currentVersion = context.FirstOrDefault<DbEntityVersion>(versionQuery);
                    }

                    if (currentVersion != null)
                        sourceVersionMaps.Add(itm.SourceEntityKey.Value, currentVersion.VersionSequenceId.Value);
                }

            // Get existing
            // TODO: What happens which this is reverse?
            var existing = context.Query<TDomainAssociation>(o => o.SourceKey == source.Key).ToList();

            // Remove old
            var obsoleteRecords = existing.Where(o => !storage.Any(ecn => ecn.Key == o.Key));
            foreach (var del in obsoleteRecords)
            {
                int obsVersion = 0;
                if (!sourceVersionMaps.TryGetValue(del.SourceKey, out obsVersion))
                    obsVersion = source.VersionSequence.GetValueOrDefault();

#if DEBUG
                this.m_tracer.TraceInfo("----- OBSOLETING {0} {1} ---- ", del.GetType().Name, del.Key);
#endif
                del.ObsoleteVersionSequenceId = obsVersion;
                context.Update<TDomainAssociation>(del);
            }

            // Update those that need it
            var updateRecords = storage.Select(o => new { store = o, existing = existing.FirstOrDefault(ecn => ecn.Key == o.Key && o.Key != Guid.Empty && o != ecn) }).Where(o => o.existing != null);
            foreach (var upd in updateRecords)
            {
                // Update by key, these lines make no sense we just update the existing versioned association
                //upd.store.EffectiveVersionSequenceId = upd.existing.EffectiveVersionSequenceId;
                //upd.store.ObsoleteVersionSequenceId = upd.existing.EffectiveVersionSequenceId;
                persistenceService.UpdateInternal(context, upd.store as TAssociation);
            }

            // Insert those that do not exist
            var insertRecords = storage.Where(o => !existing.Any(ecn => ecn.Key == o.Key));
            foreach (var ins in insertRecords)
            {
                Int32 eftVersion = 0;
                if (!sourceVersionMaps.TryGetValue(ins.SourceEntityKey.Value, out eftVersion))
                    eftVersion = source.VersionSequence.GetValueOrDefault();
                ins.EffectiveVersionSequenceId = eftVersion;

                persistenceService.InsertInternal(context, ins);
            }
        }
    }
}
