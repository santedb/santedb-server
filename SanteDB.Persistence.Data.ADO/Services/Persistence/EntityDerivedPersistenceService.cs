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
using SanteDB.Core.Event;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data.Model;
using SanteDB.Persistence.Data.ADO.Data.Model.Acts;
using SanteDB.Persistence.Data.ADO.Data.Model.DataType;
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;
using SanteDB.Persistence.Data.ADO.Data.Model.Extensibility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{

    public class EntityDerivedPersistenceService<TModel, TData> : EntityDerivedPersistenceService<TModel, TData, CompositeResult<TData, DbEntityVersion, DbEntity>>
    where TModel : Core.Model.Entities.Entity, new()
    where TData : DbEntitySubTable, new()
    {

        public EntityDerivedPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }
    }

    /// <summary>
    /// Entity derived persistence services
    /// </summary>
    public class EntityDerivedPersistenceService<TModel, TData, TQueryReturn> : SimpleVersionedEntityPersistenceService<TModel, TData, TQueryReturn, DbEntityVersion>
        where TModel : Core.Model.Entities.Entity, new()
        where TData : DbEntitySubTable, new()
        where TQueryReturn : CompositeResult
    {

        public EntityDerivedPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
            this.m_entityPersister = new EntityPersistenceService(settingsProvider);
        }

        // Entity persister
        protected EntityPersistenceService m_entityPersister;

        /// <summary>
        /// If the linked act exists
        /// </summary>
        public override bool Exists(DataContext context, Guid key)
        {
            return this.m_entityPersister.Exists(context, key);
        }

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
            return base.InsertInternal(context, data); // create a link to this version of the entity
        }

        /// <summary>
        /// Bulk obsolete
        /// </summary>
        protected override void BulkObsoleteInternal(DataContext context, Guid[] keysToObsolete)
        {
            foreach (var k in keysToObsolete)
            {
                // Get the current version
                var currentVersion = context.SingleOrDefault<DbEntityVersion>(o => o.ObsoletionTime == null && o.Key == k);
                // Create a new version
                var newVersion = new DbEntityVersion();
                newVersion.CopyObjectData(currentVersion);

                // Create a new version which has a status of obsolete
                newVersion.StatusConceptKey = StatusKeys.Obsolete;
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
                newVersion = context.Insert(newVersion);

                // Finally, insert a new version of sub data
                var cversion = context.SingleOrDefault<TData>(o => o.ParentKey == currentVersion.VersionKey);
                var newSubVersion = new TData();
                newSubVersion.CopyObjectData(cversion);
                newSubVersion.ParentKey = newVersion.VersionKey;
                context.Insert(newSubVersion);

            }
        }

        /// <summary>
        /// Query for all keys
        /// </summary>
        protected override IEnumerable<Guid> QueryKeysInternal(DataContext context, Expression<Func<TModel, bool>> query, int offset, int? count, out int totalResults)
        {
            if (!query.ToString().Contains("ObsoletionTime") && !query.ToString().Contains("VersionKey"))
            {
                var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(BaseEntityData.ObsoletionTime))), Expression.Constant(null));
                query = Expression.Lambda<Func<TModel, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, obsoletionReference, query.Body), query.Parameters);
            }

            // Construct the SQL query
            var pk = TableMapping.Get(typeof(DbEntity)).Columns.SingleOrDefault(o => o.IsPrimaryKey);
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
        /// Purge the specified records (redirects to the entity persister)
        /// </summary>
        public override void Purge(TransactionMode transactionMode, IPrincipal principal, params Guid[] keysToPurge)
        {
            this.m_entityPersister.Purge(transactionMode, principal, keysToPurge);
        }

        /// <summary>
        /// Bulk purge
        /// </summary>
        protected override void BulkPurgeInternal(DataContext connection, Expression<Func<TModel, bool>> expression)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Bulk purge
        /// </summary>
        protected override void BulkPurgeInternal(DataContext connection, Guid[] keysToPurge)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Copy specified keys
        /// </summary>
        public override void Copy(Guid[] keysToCopy, DataContext fromContext, DataContext toContext)
        {
            this.m_entityPersister.Copy(keysToCopy, fromContext, toContext);
        }

    }
}