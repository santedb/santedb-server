/*
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
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
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
    public abstract class VersionedDataPersistenceService<TModel, TDomain, TDomainKey> : BaseDataPersistenceService<TModel, TDomain, CompositeResult<TDomain, TDomainKey>>, IDataPersistenceServiceEx<TModel>
        where TDomain : class, IDbVersionedData, new()
        where TModel : VersionedEntityData<TModel>, new()
        where TDomainKey : IDbIdentified, new()
    {

        public VersionedDataPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// Return true if the specified object exists
        /// </summary>
        public override bool Exists(DataContext context, Guid key)
        {
            return context.Any<TDomainKey>(o => o.Key == key);
        }

        /// <summary>
        /// Insert the data
        /// </summary>
        public override TModel InsertInternal(DataContext context, TModel data)
        {
            // first we map the TDataKey entity
            var nonVersionedPortion = this.m_settingsProvider.GetMapper().MapModelInstance<TModel, TDomainKey>(data);

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
                .Where(o => o.Key == data.Key)
                .OrderBy<TDomain>(o => o.VersionSequenceId, Core.Model.Map.SortOrderType.OrderByDescending);

            var existingObject = context.FirstOrDefault<TDomain>(currentVersionQuery); // Get the last version (current)
            var nonVersionedObect = context.FirstOrDefault<TDomainKey>(o => o.Key == data.Key);

            if (existingObject == null)
                throw new KeyNotFoundException(data.Key.ToString());
            else if (((existingObject as IDbReadonly)?.IsReadonly == true ||
                (nonVersionedObect as IDbReadonly)?.IsReadonly == true) &&
                !AuthenticationContext.SystemApplicationSid.Equals(context.ContextId.ToString(), StringComparison.OrdinalIgnoreCase))
                throw new AdoFormalConstraintException(AdoFormalConstraintType.UpdatedReadonlyObject);
            else if (existingObject.ObsoletionTime.HasValue)
                this.m_tracer.TraceWarning("Current object {0} had no active versions - Will un-delete it", data);

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

            // Ensure that the new version does not have the obsoletion time specified at all
            newEntityVersion.ObsoletedByKey = null;
            newEntityVersion.ObsoletedByKeySpecified = true;
            newEntityVersion.ObsoletionTime = null;
            newEntityVersion.ObsoletionTimeSpecified = true;
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

            // Query has been registered?
            if (queryId != Guid.Empty && this.m_queryPersistence?.IsRegistered(queryId) == true)
                return this.GetStoredQueryResults(queryId, offset, count, out totalResults);


            // Queries to be performed
            IOrmResultSet retVal = null;
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
                    
                    SqlStatement domainQuery = null;
                    var expr = this.m_settingsProvider.GetMapper().MapModelExpression<TModel, TDomain, bool>(query, false);

                    // Fast query?
                    if (orderBy?.Length > 0)
                    {
                        if (expr != null)
                            domainQuery = context.CreateSqlStatement<TDomain>().SelectFrom(typeof(TDomain), typeof(TDomainKey))
                                .InnerJoin<TDomain, TDomainKey>(o => o.Key, o => o.Key)
                                .Where<TDomain>(expr).Build();
                        else
                            domainQuery = this.m_settingsProvider.GetQueryBuilder().CreateQuery(query).Build();

                        // Create or extend queries
                        if (retVal == null)
                            retVal = this.DomainQueryInternal<CompositeResult<TDomain, TDomainKey>>(context, domainQuery);
                        else
                            retVal = retVal.Union(this.DomainQueryInternal<CompositeResult<TDomain, TDomainKey>>(context, domainQuery));
                    }
                    else
                    {
                        var linkCol = TableMapping.Get(typeof(TDomain)).GetColumn(typeof(TDomain).GetProperty(nameof(DbIdentified.Key)));
                        var versionSeqCol = TableMapping.Get(typeof(TDomain)).GetColumn(typeof(TDomain).GetProperty(nameof(DbVersionedData.VersionSequenceId)));
                        if (expr != null)
                            domainQuery = context.CreateSqlStatement<TDomain>().SelectFrom(linkCol, versionSeqCol)
                                .InnerJoin<TDomain, TDomainKey>(o => o.Key, o => o.Key)
                                .Where<TDomain>(expr).Build();
                        else
                            domainQuery = this.m_settingsProvider.GetQueryBuilder().CreateQuery(query, linkCol, versionSeqCol).Build();

                        // Create or extend queries
                        if (retVal == null)
                            retVal = this.DomainQueryInternal<Guid>(context, domainQuery);
                        else
                            retVal = retVal.Union(this.DomainQueryInternal<Guid>(context, domainQuery));
                    }

                    
                }

                // HACK: More than one query which indicates union was used, we need to wrap in a select statement to be adherent to SQL standard on Firebird and PSQL
                if (queries.Count() > 1)
                {
                    var query = this.AppendOrderBy(context.CreateSqlStatement("SELECT * FROM (").Append(retVal.ToSqlStatement()).Append(") AS domain_query "), orderBy);
                    retVal = this.DomainQueryInternal<Guid>(context, query);
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

                    if(context.Data.TryGetValue("principal", out object pRaw))
                    {
                        var currentPrincipal = pRaw as IPrincipal;
                        countResults = countResults && currentPrincipal != AuthenticationContext.SystemPrincipal;
                        overrideFuzzyTotalSetting = currentPrincipal == AuthenticationContext.SystemPrincipal;
                    }

                    if (queryId != Guid.Empty && ApplicationServiceContext.Current.GetService<IQueryPersistenceService>() != null)
                    {
                        var keys = retVal.Keys<Guid>().Take(count.Value * 20).OfType<Guid>().ToArray();
                        totalResults = keys.Length;
                        this.m_queryPersistence?.RegisterQuerySet(queryId, keys, queries, totalResults);
                        if (totalResults == count.Value * 20) // result set is larger than 10,000 load in background
                        {
                            ApplicationServiceContext.Current.GetService<IThreadPoolService>().QueueUserWorkItem(o =>
                            {
                                var dynParm = o as dynamic;
                                var subContext = dynParm.Context as DataContext;
                                var statement = dynParm.Statement as SqlStatement;
                                var type = dynParm.Type as Type;
                                var qid = (Guid)dynParm.QueryId;

                                // Get the rest of the keys
                                Guid[] sk = null;
                                if(type == typeof(OrmResultSet<Guid>))
                                    sk = subContext.Query<Guid>(statement).ToArray();
                                else
                                    sk = subContext.Query<CompositeResult<TDomain, TDomainKey>>(statement).Keys<Guid>(false).ToArray();
                                this.m_queryPersistence?.RegisterQuerySet(queryId, sk, statement, sk.Length);
                            }, new { Context = context.OpenClonedContext(), Statement = retVal.Statement.Build(), QueryId = queryId, Type = retVal.GetType() });
                        }
                        return keys.Skip(offset).Take(count.Value).OfType<Object>();
                    }
                    else if (count.HasValue && countResults && !overrideFuzzyTotalSetting && !this.m_settingsProvider.GetConfiguration().UseFuzzyTotals)
                        totalResults = retVal.Count();
                    else
                        totalResults = 0;

                    // Fuzzy totals - This will only fetch COUNT + 1 as the total results
                    if (count.HasValue)
                    {
                        if ((overrideFuzzyTotalSetting || this.m_settingsProvider.GetConfiguration().UseFuzzyTotals) && totalResults == 0)
                        {
                            var fuzzResults = retVal.Skip(offset).Take(count.Value + 1).OfType<Object>().ToList();
                            totalResults = fuzzResults.Count() + offset;
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
            finally
            {
#if DEBUG
                sw.Stop();
              //  this.m_tracer.TraceVerbose("Query {0} in {1} ms", context.GetQueryLiteral(retVal.ToSqlStatement()), sw.ElapsedMilliseconds);
#endif
            }

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
                    .Where<TDomain>(o => o.Key == key)
                    .OrderBy<TDomain>(o => o.VersionSequenceId, Core.Model.Map.SortOrderType.OrderByDescending);

                // Is the most recent version obsolete? If so, un-obsolete it
                var recentVersion = context.FirstOrDefault<CompositeResult<TDomain, TDomainKey>>(domainQuery);
                if (recentVersion?.Object1.ObsoletionTime != null && !context.IsReadonly)
                {
                    this.m_tracer.TraceWarning("Object {0} # {1} has no active versions - Possible BUG - Restoring previous version", recentVersion.GetType().FullName, recentVersion.Object2?.Key);
                    recentVersion.Object1.ObsoletionTime = null;
                    recentVersion.Object1.ObsoletionTimeSpecified = true;
                    recentVersion.Object1.ObsoletedByKey = null;
                    recentVersion.Object1.ObsoletedByKeySpecified = true;
                    recentVersion = context.Update(recentVersion);
                }

                return this.CacheConvert(recentVersion, context);
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
                if (cacheItem != null && (cacheItem.VersionKey.HasValue && versionId == cacheItem.VersionKey.Value || versionId.GetValueOrDefault() == Guid.Empty) &&
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
            using (var connection = this.m_settingsProvider.GetConfiguration().Provider.GetReadonlyConnection())
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

            // Remove any duplicated relationships
            if (storage is IList<TAssociation> listStore)
            {
                for(int i = listStore.Count - 1; i >= 0; i--) // Remove dups
                    if(listStore.Count(o=> o.Key == listStore[i].Key) > 1)
                        listStore.RemoveAt(i);
            }
        }


        /// <summary>
        /// Touch the specified versioned object (update time without creating new version
        /// </summary>
        public void Touch(Guid key, TransactionMode mode, IPrincipal principal)
        {
#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif


            // Query object
            using (var connection = this.m_settingsProvider.GetConfiguration().Provider.GetWriteConnection())
                try
                {
                    connection.Open();
                    this.m_tracer.TraceEvent(EventLevel.Verbose, "TOUCH {0}", key);

                    TModel retVal = null;
                    connection.LoadState = LoadState.FullLoad;
                    // Update the specified object
                    var currentData = connection.FirstOrDefault<TDomain>(o => !o.ObsoletionTime.HasValue && o.Key == key);
                    if (currentData != null)
                    {
                        var provenance = connection.EstablishProvenance(principal, null);
                        currentData.CreationTime = DateTimeOffset.Now;
                        currentData.CreatedByKey = provenance;
                        connection.Update(currentData);
                    }

                }
                catch (NotSupportedException e)
                {
                    throw new DataPersistenceException("Cannot perform LINQ query because underlying repository does not support it", e);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error, "Error : {0}", e);
                    throw new DataPersistenceException($"Error touching record {key}", e);
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
        /// Query keys for versioned objects 
        /// </summary>
        /// <remarks>This redirects the query from the primary key (on TDomain) into the primary key on the base object</remarks>
        protected override IEnumerable<Guid> QueryKeysInternal(DataContext context, Expression<Func<TModel, bool>> query, int offset, int? count, out int totalResults)
        {
            if (!query.ToString().Contains("ObsoletionTime") && !query.ToString().Contains("VersionKey"))
            {
                var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(BaseEntityData.ObsoletionTime))), Expression.Constant(null));
                query = Expression.Lambda<Func<TModel, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, obsoletionReference, query.Body), query.Parameters);
            }

            // Construct the SQL query
            var pk = TableMapping.Get(typeof(TDomainKey)).Columns.SingleOrDefault(o => o.IsPrimaryKey);
            var domainQuery = this.m_settingsProvider.GetQueryBuilder().CreateQuery(query, pk);

            var results = context.Query<Guid>(domainQuery);

            count = count ?? 100;
            if (this.m_settingsProvider.GetConfiguration().UseFuzzyTotals)
            {
                // Skip and take
                results = results.Skip(offset).Take(count.Value + 1);
                totalResults = offset + results.Count();
            }
            else
            {
                totalResults = results.Count();
                results = results.Skip(offset).Take(count.Value);
            }

            return results.ToList(); // exhaust the results and continue
        }

        /// <summary>
        /// Obsolete the specified keys
        /// </summary>
        protected sealed override void BulkObsoleteInternal(DataContext context, Guid[] keysToObsolete)
        {
            foreach (var k in keysToObsolete)
            {
                
                // Get the current version
                var currentVersion = context.SingleOrDefault<TDomain>(o => o.ObsoletionTime == null && o.Key == k);
                // Create a new version
                var newVersion = new TDomain();
                newVersion.CopyObjectData(currentVersion);

                // Create a new version which has a status of obsolete
                if (newVersion is IDbHasStatus status)
                {
                    status.StatusConceptKey = StatusKeys.Obsolete;
                    // Update the old version
                    currentVersion.ObsoletedByKey = context.ContextId;
                    currentVersion.ObsoletionTime = DateTimeOffset.Now;
                    context.Update(currentVersion);
                    // Provenance data
                    newVersion.VersionSequenceId = null;
                    newVersion.ReplacesVersionKey = currentVersion.VersionKey;
                    newVersion.CreatedByKey = context.ContextId;
                    newVersion.CreationTime = DateTimeOffset.Now;
                    newVersion.VersionKey = Guid.Empty;
                    context.Insert(newVersion);

                }
                else // Just remove the version
                {
                    // Update the old version
                    currentVersion.ObsoletedByKey = context.ContextId;
                    currentVersion.ObsoletionTime = DateTimeOffset.Now;
                    context.Update(currentVersion);
                }
            }
        }
    }
}
