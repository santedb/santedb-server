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
using SanteDB.Core.Model;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data.Model;
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;
using System;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security.Principal;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{

    public class EntityDerivedPersistenceService<TModel, TData> : EntityDerivedPersistenceService<TModel, TData, CompositeResult<TData, DbEntityVersion, DbEntity>>
    where TModel : Core.Model.Entities.Entity, new()
    where TData : DbEntitySubTable, new()
    { }

    /// <summary>
    /// Entity derived persistence services
    /// </summary>
    public class EntityDerivedPersistenceService<TModel, TData, TQueryReturn> : SimpleVersionedEntityPersistenceService<TModel, TData, TQueryReturn, DbEntityVersion>
        where TModel : Core.Model.Entities.Entity, new()
        where TData : DbEntitySubTable, new()
        where TQueryReturn : CompositeResult
    {

        // Entity persister
        protected EntityPersistenceService m_entityPersister = new EntityPersistenceService();

        /// <summary>
        /// From model instance
        /// </summary>
        public override object FromModelInstance(TModel modelInstance, DataContext context)
        {
            var retVal = base.FromModelInstance(modelInstance, context);
            (retVal as DbEntitySubTable).ParentKey = modelInstance.VersionKey.Value;
            return retVal;
        }

        /// <summary>
        /// Entity model instance
        /// </summary>
        public override sealed TModel ToModelInstance(object dataInstance, DataContext context)
        {
            return (TModel)this.m_entityPersister.ToModelInstance(dataInstance, context);
        }

        /// <summary>
        /// Conversion based on type
        /// </summary>
        protected override TModel CacheConvert(object o, DataContext context)
        {
            return (TModel)this.m_entityPersister.DoCacheConvert(o, context);
        }

        /// <summary>
        /// Insert the specified TModel into the database
        /// </summary>
        public override TModel InsertInternal(DataContext context, TModel data)
        {
            if (typeof(TModel).BaseType == typeof(Core.Model.Entities.Entity))
            {
                var inserted = this.m_entityPersister.InsertCoreProperties(context, data);
                data.Key = inserted.Key;
                data.VersionKey = inserted.VersionKey;
            }
            return base.InsertInternal(context, data);

        }

        /// <summary>
        /// Update the specified TModel
        /// </summary>
        public override TModel UpdateInternal(DataContext context, TModel data)
        {
            if (typeof(TModel).BaseType == typeof(Core.Model.Entities.Entity))
                this.m_entityPersister.UpdateCoreProperties(context, data);
            return base.InsertInternal(context, data);
            //return base.Update(context, data);
        }


        /// <summary>
        /// Gets the specified object
        /// </summary>
        public override TModel Get(Guid containerId, Guid? versionId, bool loadFast, IPrincipal principal = null)
        {
            var tr = 0;
            
            // Fire retrieving 
            var preArgs = new DataRetrievingEventArgs<TModel>(containerId, versionId, principal);
            this.FireRetrieving(preArgs);
            if (preArgs.Cancel)
            {
                this.m_tracer.TraceEvent(EventLevel.Warning, "Pre-event args indicate cancel : {0}", containerId);
                return preArgs.Result;
            }

            if (containerId != Guid.Empty)
            {
                var cacheItem = ApplicationServiceContext.Current.GetService<IDataCachingService>()?.GetCacheItem<TModel>(containerId) as TModel;
                if (cacheItem != null && (cacheItem.VersionKey.HasValue && versionId == cacheItem.VersionKey.Value || versionId.GetValueOrDefault() == Guid.Empty) &&
                    (loadFast && cacheItem.LoadState >= LoadState.PartialLoad || !loadFast && cacheItem.LoadState == LoadState.FullLoad))
                    return cacheItem;
            }

            // Get most recent version
            TModel result = default(TModel);
            if (versionId.GetValueOrDefault() == Guid.Empty)
                result = base.Query(o => o.Key == containerId && o.ObsoletionTime == null, 0, 1, out tr, principal).FirstOrDefault();
            else
                result = base.Query(o => o.Key == containerId && o.VersionKey == versionId, 0, 1, out tr, principal).FirstOrDefault();

            var postArgs = new DataRetrievedEventArgs<TModel>(result, principal);
            this.FireRetrieved(postArgs);

            return result;
        }

        /// <summary>
        /// Obsolete the object
        /// </summary>
        public override TModel ObsoleteInternal(DataContext context, TModel data)
        {
            if (typeof(TModel).BaseType == typeof(Core.Model.Entities.Entity))
                this.m_entityPersister.ObsoleteInternal(context, data);
            return base.InsertInternal(context, data);
        }

    }
}