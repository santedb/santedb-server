/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: justin
 * Date: 2018-6-22
 */
using System.Security.Principal;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Persistence.Data.ADO.Data.Model;
using MARC.HI.EHRS.SVC.Core.Data;
using System;
using MARC.HI.EHRS.SVC.Core;
using SanteDB.Core.Services;
using System.Linq;
using SanteDB.Persistence.Data.ADO.Data;
using MARC.HI.EHRS.SVC.Core.Event;
using System.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data.Model.Acts;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Represents a simple versioned entity persistence service
    /// </summary>
    public abstract class SimpleVersionedEntityPersistenceService<TModel, TData, TQueryReturn, TRootEntity> : CorePersistenceService<TModel, TData, TQueryReturn>
        where TModel : BaseEntityData, IVersionedEntity, new()
        where TData : DbSubTable, new()
        where TRootEntity : class, IDbVersionedData, new()
        where TQueryReturn : CompositeResult
    {

        /// <summary>
        /// Get the specified object
        /// </summary>
        internal override TModel Get(DataContext context, Guid key)
        {

            // We need to join, but to what?
            // True to get the cache item
            var cacheService = new AdoPersistenceCache(context);
            var cacheItem = cacheService?.GetCacheItem<TModel>(key) as TModel;
            if (cacheItem != null && context.Transaction == null)
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
                var domainQuery = AdoPersistenceService.GetQueryBuilder().CreateQuery<TModel>(o => o.Key == key && o.ObsoletionTime == null).Build();
                domainQuery.OrderBy<TRootEntity>(o => o.VersionSequenceId, Core.Model.Map.SortOrderType.OrderByDescending);
                cacheItem = this.ToModelInstance(context.FirstOrDefault<TQueryReturn>(domainQuery), context);
                if (cacheService != null)
                    cacheService.Add(cacheItem);
                return cacheItem;
            }
        }

        /// <summary>
        /// Gets the specified object taking version of the entity into consideration
        /// </summary>
        public override TModel Get<TIdentifier>(MARC.HI.EHRS.SVC.Core.Data.Identifier<TIdentifier> containerId, IPrincipal principal, bool loadFast)
        {
            var tr = 0;
            var uuid = containerId as Identifier<Guid>;

            if (uuid.Id == Guid.Empty)
                return default(TModel);

            PreRetrievalEventArgs<TModel> preArgs = new PreRetrievalEventArgs<TModel>(containerId);
            this.FireRetrieving(preArgs);
            if (preArgs.Cancel)
            {
                this.m_tracer.TraceEvent(TraceEventType.Warning, 0, "Pre-Event handler indicates abort retrieve {0}", containerId.Id);
                return preArgs.OverrideResult;
            }

            if (uuid.Id != Guid.Empty)
            {
                var cacheItem = ApplicationContext.Current.GetService<IDataCachingService>()?.GetCacheItem<TModel>(uuid.Id) as TModel;
                if (cacheItem != null && (cacheItem.VersionKey.HasValue && uuid.VersionId == cacheItem.VersionKey.Value || uuid.VersionId == Guid.Empty) &&
                    (loadFast && cacheItem.LoadState >= LoadState.PartialLoad || !loadFast && cacheItem.LoadState == LoadState.FullLoad))
                    return cacheItem;
            }

#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif

            
            // Query object
            using (var connection = m_configuration.Provider.GetReadonlyConnection())
                try
                {
                    connection.Open();
                    this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, "GET {0}", containerId);

                    TModel retVal = null;
                    connection.LoadState = LoadState.FullLoad;
                    // Get most recent version
                    if (uuid.VersionId == Guid.Empty)
                        retVal = this.Get(connection, uuid.Id);
                    else
                        retVal = this.CacheConvert(this.QueryInternal(connection, o => o.Key == uuid.Id && o.VersionKey == uuid.VersionId, Guid.Empty, 0, 1, out tr).FirstOrDefault(), connection);

                    var postData = new PostRetrievalEventArgs<TModel>(retVal);
                    this.FireRetrieved(postData);

                    return retVal;

                }
                catch (NotSupportedException e)
                {
                    throw new DataPersistenceException("Cannot perform LINQ query", e);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(TraceEventType.Error, 0, "Error : {0}", e);
                    throw;
                }
                finally
                {
#if DEBUG
                    sw.Stop();
                    this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, "Retrieve took {0} ms", sw.ElapsedMilliseconds);
#endif
                }
        }

    }
}