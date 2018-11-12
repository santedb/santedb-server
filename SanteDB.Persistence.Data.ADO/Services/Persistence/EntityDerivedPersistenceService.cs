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
 * User: fyfej
 * Date: 2017-9-1
 */
using SanteDB.Core.Model.Entities;
using SanteDB.Persistence.Data.ADO.Data.Model;
using System.Security.Principal;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Data;
using System;
using SanteDB.Core.Services;
using System.Linq;
using SanteDB.Persistence.Data.ADO.Data.Model.Acts;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;
using SanteDB.OrmLite;
using SanteDB.Core.Model;
using MARC.HI.EHRS.SVC.Core.Event;

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
        public override TModel Get<TIdentifier>(MARC.HI.EHRS.SVC.Core.Data.Identifier<TIdentifier> containerId, IPrincipal principal, bool loadFast)
        {
            var tr = 0;
            var uuid = containerId as Identifier<Guid>;

            // Fire retrieving 
            var preArgs = new PreRetrievalEventArgs<TModel>(containerId, principal);
            this.FireRetrieving(preArgs);
            if (preArgs.Cancel)
            {
                this.m_tracer.TraceEvent(System.Diagnostics.TraceEventType.Warning, 0, "Pre-event args indicate cancel : {0}", containerId);
                return preArgs.OverrideResult;
            }

            if (uuid.Id != Guid.Empty)
            {
                var cacheItem = ApplicationContext.Current.GetService<IDataCachingService>()?.GetCacheItem<TModel>(uuid.Id) as TModel;
                if (cacheItem != null && (cacheItem.VersionKey.HasValue && uuid.VersionId == cacheItem.VersionKey.Value || uuid.VersionId == Guid.Empty) &&
                    (loadFast && cacheItem.LoadState >= LoadState.PartialLoad || !loadFast && cacheItem.LoadState == LoadState.FullLoad))
                    return cacheItem;
            }

            // Get most recent version
            TModel result = default(TModel);
            if (uuid.VersionId == Guid.Empty)
                result = base.Query(o => o.Key == uuid.Id && o.ObsoletionTime == null, 0, 1, principal, out tr).FirstOrDefault();
            else
                result = base.Query(o => o.Key == uuid.Id && o.VersionKey == uuid.VersionId, 0, 1, principal, out tr).FirstOrDefault();

            var postArgs = new PostRetrievalEventArgs<TModel>(result, principal);
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