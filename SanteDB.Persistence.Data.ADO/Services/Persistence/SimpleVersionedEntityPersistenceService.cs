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
using SanteDB.Core.Event;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security.Principal;

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
                var domainQuery = this.m_persistenceService.GetQueryBuilder().CreateQuery<TModel>(o => o.Key == key && o.ObsoletionTime == null).Build();
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
        public override TModel Get(Guid containerId, Guid? versionId, bool loadFast, IPrincipal overrideAuthContext = null)
        {
            var tr = 0;
            if (containerId == Guid.Empty)
                return default(TModel);

            DataRetrievingEventArgs<TModel> preArgs = new DataRetrievingEventArgs<TModel>(containerId, versionId, overrideAuthContext);
            this.FireRetrieving(preArgs);
            if (preArgs.Cancel)
            {
                this.m_tracer.TraceEvent(EventLevel.Warning, "Pre-Event handler indicates abort retrieve {0}", containerId);
                return preArgs.Result;
            }


            var cacheItem = ApplicationServiceContext.Current.GetService<IDataCachingService>()?.GetCacheItem<TModel>(containerId) as TModel;
            if (cacheItem != null && (cacheItem.VersionKey.HasValue && versionId == cacheItem.VersionKey.Value || versionId == Guid.Empty) &&
                (loadFast && cacheItem.LoadState >= LoadState.PartialLoad || !loadFast && cacheItem.LoadState == LoadState.FullLoad))
                return cacheItem;


#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif


            // Query object
            using (var connection = m_configuration.Provider.GetReadonlyConnection())
                try
                {
                    connection.Open();
                    this.m_tracer.TraceEvent(EventLevel.Verbose, "GET {0}", containerId);

                    TModel retVal = null;
                    connection.LoadState = LoadState.FullLoad;
                    // Get most recent version
                    if (versionId.GetValueOrDefault() == Guid.Empty)
                        retVal = this.Get(connection, containerId);
                    else
                        retVal = this.CacheConvert(this.QueryInternal(connection, o => o.Key == containerId && o.VersionKey == versionId, Guid.Empty, 0, 1, out tr, null).FirstOrDefault(), connection);

                    var postData = new DataRetrievedEventArgs<TModel>(retVal, overrideAuthContext);
                    this.FireRetrieved(postData);

                    return retVal;

                }
                catch (NotSupportedException e)
                {
                    throw new DataPersistenceException("Cannot perform LINQ query", e);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error,  "Error : {0}", e);
                    throw;
                }
                finally
                {
#if DEBUG
                    sw.Stop();
                    this.m_tracer.TraceEvent(EventLevel.Verbose, "Retrieve took {0} ms", sw.ElapsedMilliseconds);
#endif
                }
        }

    }
}