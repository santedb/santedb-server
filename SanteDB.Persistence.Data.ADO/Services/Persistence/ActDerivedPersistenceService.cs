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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data.Model;
using SanteDB.Persistence.Data.ADO.Data.Model.Acts;
namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Represents a persistence service which is derived from an act
    /// </summary>
    public abstract class ActDerivedPersistenceService<TModel, TData> : ActDerivedPersistenceService<TModel, TData, CompositeResult<TData, DbActVersion, DbAct>>
        where TModel : Core.Model.Acts.Act, new()
        where TData : DbActSubTable, new()
    {

        public ActDerivedPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

    }

    /// <summary>
    /// Represents a persistence service which is derived from an act
    /// </summary>
    public abstract class ActDerivedPersistenceService<TModel, TData, TQueryReturn> : SimpleVersionedEntityPersistenceService<TModel, TData, TQueryReturn, DbActVersion>
        where TModel : Core.Model.Acts.Act, new()
        where TData : DbActSubTable, new()
        where TQueryReturn : CompositeResult
    {

        public ActDerivedPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
            this.m_actPersister = new ActPersistenceService(settingsProvider);
        }

        // act persister
        protected ActPersistenceService m_actPersister;

        /// <summary>
        /// If the linked act exists
        /// </summary>
        public override bool Exists(DataContext context, Guid key)
        {
            return this.m_actPersister.Exists(context, key);
        }

        /// <summary>
        /// From model instance
        /// </summary>
        public override object FromModelInstance(TModel modelInstance, DataContext context)
        {
            var retVal = base.FromModelInstance(modelInstance, context);
            (retVal as DbActSubTable).ParentKey = modelInstance.VersionKey.Value;
            return retVal;
        }

        /// <summary>
        /// Entity model instance
        /// </summary>
        public override sealed TModel ToModelInstance(object dataInstance, DataContext context)
        {
            return (TModel)this.m_actPersister.ToModelInstance(dataInstance, context);
        }

        /// <summary>
        /// Insert the specified TModel into the database
        /// </summary>
        public override TModel InsertInternal(DataContext context, TModel data)
        {
            if (typeof(TModel).BaseType == typeof(Act))
            {
                var inserted = this.m_actPersister.InsertCoreProperties(context, data);
                data.Key = inserted.Key;
            }
            return base.InsertInternal(context, data);
        }

        /// <summary>
        /// Update the specified TModel
        /// </summary>
        public override TModel UpdateInternal(DataContext context, TModel data)
        {
            if (typeof(TModel).BaseType == typeof(Act))
                this.m_actPersister.UpdateCoreProperties(context, data);
            return base.InsertInternal(context, data);
        }

        /// <summary>
        /// Obsolete the object
        /// </summary>
        public override TModel ObsoleteInternal(DataContext context, TModel data)
        {
            var retVal = this.m_actPersister.ObsoleteInternal(context, data);
            return base.InsertInternal(context, data);
        }

        /// <summary>
        /// Purge the specified records (redirects to the act persister)
        /// </summary>
        public override void Purge(TransactionMode transactionMode, IPrincipal principal, params Guid[] keysToPurge)
        {
            this.m_actPersister.Purge(transactionMode, principal, keysToPurge);
        }

        /// <summary>
        /// Bulk obsolete 
        /// </summary>
        protected override void BulkObsoleteInternal(DataContext context, Guid[] keysToObsolete)
        {
            foreach (var k in keysToObsolete)
            {
                // Get the current version
                var currentVersion = context.SingleOrDefault<DbActVersion>(o => o.ObsoletionTime == null && o.Key == k);
                // Create a new version
                var newVersion = new DbActVersion();
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
        /// Copy keys
        /// </summary>
        public override void Copy(Guid[] keysToCopy, DataContext fromContext, DataContext toContext)
        {
            this.m_actPersister.Copy(keysToCopy, fromContext, toContext);
        }

        /// <summary>
        /// Query keys 
        /// </summary>
        protected override IEnumerable<Guid> QueryKeysInternal(DataContext context, Expression<Func<TModel, bool>> query, int offset, int? count, out int totalResults)
        {
            if (!query.ToString().Contains("ObsoletionTime") && !query.ToString().Contains("VersionKey"))
            {
                var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(BaseEntityData.ObsoletionTime))), Expression.Constant(null));
                query = Expression.Lambda<Func<TModel, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, obsoletionReference, query.Body), query.Parameters);
            }

            // Construct the SQL query
            var pk = TableMapping.Get(typeof(DbAct)).Columns.SingleOrDefault(o => o.IsPrimaryKey);
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
    }
}
