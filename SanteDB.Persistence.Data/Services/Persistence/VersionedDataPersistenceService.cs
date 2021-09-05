using SanteDB.Core;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence
{
    /// <summary>
    /// Persistence service which handles versioned objects
    /// </summary>
    public abstract class VersionedDataPersistenceService<TModel, TDbModel, TDbKeyModel> : BaseEntityDataPersistenceService<TModel, TDbModel>
        where TModel : VersionedEntityData<TModel>, new()
        where TDbModel : DbVersionedData, new()
        where TDbKeyModel : DbIdentified, new()
    {
        /// <summary>
        /// Generate the specified constructor
        /// </summary>
        public VersionedDataPersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Perform a GET internal
        /// </summary>
        protected override TDbModel DoGetInternal(DataContext context, Guid key, Guid? versionKey, bool allowCache = false)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            TDbModel retVal = default(TDbModel);

#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            try
            {
#endif

                if (!versionKey.HasValue) // fetching the current version
                {
                    var cacheKey = this.GetAdHocCacheKey(key);
                    if (allowCache && (this.m_configuration?.CachingPolicy?.Targets & Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects) == Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects)
                    {
                        retVal = this.m_adhocCache?.Get<TDbModel>(cacheKey);
                    }

                    // Cache miss
                    if (retVal == null)
                    {
                        retVal = context.Query<TDbModel>(o => o.Key == key).OrderByDescending(o => o.VersionSequenceId).FirstOrDefault();

                        if ((this.m_configuration?.CachingPolicy?.Targets & Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects) == Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects)
                        {
                            this.m_adhocCache.Add<TDbModel>(cacheKey, retVal, this.m_configuration.CachingPolicy?.DataObjectExpiry);
                        }
                    }
                }
                else
                {
                    // Fetch the object
                    retVal = context.FirstOrDefault<TDbModel>(o => o.Key == key && o.VersionKey == versionKey);
                }

#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose("Get {0}/v.{1} took {2} ms", key, versionKey, sw.ElapsedMilliseconds);
            }
#endif
            return retVal;
        }

        /// <summary>
        /// Perform an insert on the specified object
        /// </summary>
        protected override TDbModel DoInsertInternal(DataContext context, TDbModel dbModel)
        {

            if (dbModel == null)
            {
                throw new ArgumentNullException(nameof(dbModel), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }

#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            try
            {
#endif
                // First we want to insert the keyed object via TDbKeyTable
                if (dbModel.Key == Guid.Empty)
                {
                    dbModel.Key = Guid.NewGuid();
                }
                var dbKeyTable = context.Insert(new TDbKeyModel() { Key = dbModel.Key });

                // Next we want to insert the version key
                if (dbModel.VersionKey == Guid.Empty)
                {
                    dbModel.VersionKey = Guid.NewGuid();
                }
                dbModel.CreationTime = DateTimeOffset.Now;
                dbModel.CreatedByKey = context.ContextId;

                // Insert the version link data
                return context.Insert(dbModel);
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose("Insert {0} took {1}ms", dbModel, sw.ElapsedMilliseconds);
            }
#endif
        }

        /// <summary>
        /// Perform a versioned update
        /// </summary>
        protected override TDbModel DoUpdateInternal(DataContext context, TDbModel model)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (model == null)
            {
                throw new ArgumentNullException(nameof(model), ErrorMessages.ERR_ARGUMENT_NULL);
            }

#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            try
            {
#endif

                // We don't create new versions, instead we update the current data
                var existing = context.Query<TDbModel>(o => o.Key == model.Key).OrderByDescending(o => o.VersionSequenceId).Skip(0).Take(2).ToArray();

                if (!existing.Any())
                {
                    throw new KeyNotFoundException(ErrorMessages.ERR_NOT_FOUND.Format(model));
                }

                // Are we creating a new verison? or no?
                if (!this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.FullVersioning))
                {
                    if (existing.Count() > 1) // We only keep recent and last
                    {
                        context.Delete<TDbModel>(o => o.Key == model.Key && o.VersionSequenceId <= existing.Last().VersionSequenceId);
                    }
                }

                // We want to obsolete the non current version(s)
                foreach (var itm in context.Query<TDbModel>(o => o.Key == model.Key && !o.ObsoletionTime.HasValue))
                {
                    itm.ObsoletionTime = DateTimeOffset.Now;
                    itm.ObsoletedByKey = context.ContextId;
                    itm.ObsoletedByKeySpecified = itm.ObsoletionTimeSpecified = true;
                    context.Update(itm);
                }

                // next - we create a new version of dbmodel
                var newVersion = existing.First().CopyObjectData(model, true);
                newVersion.ReplacesVersionKey = newVersion.VersionKey;
                newVersion.CreationTime = DateTimeOffset.Now;
                newVersion.CreatedByKey = context.ContextId;
                newVersion.ObsoletedByKey = null;
                newVersion.ObsoletionTime = null;
                newVersion.VersionSequenceId = null;
                
                newVersion.ObsoletedByKeySpecified = model.ObsoletionTimeSpecified = true;
                newVersion.VersionKey = Guid.NewGuid();
                return context.Insert(newVersion);
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose("Update of {0} took {1} ms", model, sw.ElapsedMilliseconds);
            }
#endif
        }

        /// <summary>
        /// Perform an obsoletion of the object in the datamodel
        /// </summary>
        /// <param name="context">The context in which the obsoletion is occurring</param>
        /// <param name="key">The key of the object which is to be obsoleted</param>
        protected override TDbModel DoObsoleteInternal(DataContext context, Guid key)
        {

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (key == Guid.Empty)
            {
                throw new ArgumentException(ErrorMessages.ERR_MISSING_ARGUMENT, nameof(key));
            }

            // Perform the obsoletion
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            try
            {
#endif

                // How are we obsoleting this? - is it full versioning?
                if (this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.FullVersioning))
                {
                    // Get the current version
                    var existing = context.Query<TDbModel>(o => o.Key == key).OrderByDescending(o => o.VersionSequenceId).FirstOrDefault();
                    if (existing == null)
                    {
                        throw new KeyNotFoundException(ErrorMessages.ERR_NOT_FOUND.Format(key));
                    }

                    // Set status to obsolete and create a new version (redirect to update)
                    if (existing is IDbHasStatus status)
                    {
                        status.StatusConceptKey = StatusKeys.Obsolete;
                        return this.DoUpdateInternal(context, existing);
                    }
                    else
                    {
                        return base.DoObsoleteInternal(context, key);
                    }
                }
                else
                {
                    return base.DoObsoleteInternal(context, key);
                }
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose("Obsoletion of {0} took {1} ms", key, sw.ElapsedMilliseconds);
            }
#endif
        }

        /// <summary>
        /// Perform an internal query - adding appropriate status code and obsoletion time filters
        /// </summary>
        protected override OrmResultSet<TDbModel> DoQueryInternal(DataContext context, Expression<Func<TModel, bool>> query, bool allowCache = false)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (query == null)
            {
                throw new ArgumentNullException(nameof(query), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            // First - we determine if the query has an explicit status concept set
            if (typeof(IHasState).IsAssignableFrom(typeof(TModel)) && !query.ToString().Contains(nameof(IHasState.StatusConceptKey)))
            {
                var statusKeyProperty = Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(IHasState.StatusConceptKey)));
                statusKeyProperty = Expression.MakeMemberAccess(statusKeyProperty, statusKeyProperty.Type.GetProperty("Value"));
                query = Expression.Lambda<Func<TModel, bool>>(Expression.And(query.Body, Expression.MakeBinary(ExpressionType.NotEqual, statusKeyProperty, Expression.Constant(StatusKeys.Obsolete))), query.Parameters);
            }

            // pass control to sub group
            return base.DoQueryInternal(context, query, allowCache);
        }

        /// <summary>
        /// Update associated entities 
        /// </summary>
        /// <remarks>
        /// Updates the associated items of <typeparamref name="TModelAssociation"/> such that
        /// <paramref name="data"/>'s associations are updated to match the list 
        /// provided in <paramref name="associations"/>
        /// </remarks>
        protected virtual IEnumerable<TModelAssociation> UpdateModelVersionedAssociations<TModelAssociation>(DataContext context, TModel data, IEnumerable<TModelAssociation> associations)
            where TModelAssociation : IdentifiedData, IVersionedAssociation, new()
        {
            // Ensure either the relationship points to (key) (either source or target)
            associations = associations.Select(a =>
            {
                if (a is ITargetedAssociation target && target.Key != data.Key && a.SourceEntityKey != data.Key ||
                    a.SourceEntityKey.GetValueOrDefault() == Guid.Empty) // The target is a target association
                {
                    a.SourceEntityKey = data.Key;
                }
                return a;
            }).ToArray();

            // We now want to fetch the perssitence serivce of this 
            var persistenceService = base.GetRelatedPersistenceService<TModelAssociation>();
            if (persistenceService == null)
            {
                throw new DataPersistenceException(ErrorMessages.ERR_ARGUMENT_INCOMPATIBLE_TYPE.Format(typeof(IAdoPersistenceProvider<TModelAssociation>), typeof(IDataPersistenceService<TModelAssociation>)));
            }

            // Next we want to perform a relationship query to establish what is being loaded and what is being persisted
            var existing = persistenceService.Query(context, o => o.SourceEntityKey == data.Key && !o.ObsoleteVersionSequenceId.HasValue).ToArray();

            // Which are new and which are not?
            var removedRelationships = existing.Where(o => !associations.Any(a => a.Key == o.Key)).Select(a =>
            {
                if (this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.AssociationVersioning))
                {
                    a.ObsoleteVersionSequenceId = data.VersionSequence;
                    a = persistenceService.Update(context, a);
                }
                else
                {
                   a =  persistenceService.Obsolete(context, a.Key.Value);
                }
                a.BatchOperation = Core.Model.DataTypes.BatchOperationType.Obsolete;
                return a;
            });
            var addedRelationships = associations.Where(o => !o.Key.HasValue || !existing.Any(a => a.Key == o.Key)).Select(a =>
            {
                a.EffectiveVersionSequenceId = data.VersionSequence;
                a = persistenceService.Insert(context, a);
                a.BatchOperation = Core.Model.DataTypes.BatchOperationType.Insert;
                return a;
            });
            var updatedRelationships = associations.Where(o => o.Key.HasValue && existing.Any(a => a.Key == o.Key && !a.SemanticEquals(o))).Select(a =>
            {
                
                // We are versioning so obsolete existing and then create new
                if (this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.AssociationVersioning))
                {
                    a.ObsoleteVersionSequenceId = data.VersionSequence;
                    persistenceService.Update(context, a);
                    a.Key = null;
                    a.ObsoleteVersionSequenceId = null;
                    a.EffectiveVersionSequenceId = data.VersionSequence;
                    a = persistenceService.Insert(context, a);
                }
                else // We just update the existing
                {
                    a = persistenceService.Update(context, a);
                }
                a.BatchOperation = Core.Model.DataTypes.BatchOperationType.Update;
                return a;
            });

            return existing.Where(e => !removedRelationships.Any(r => r.Key == e.Key)).Union(addedRelationships).ToArray();

        }

        /// <summary>
        /// Update the internal
        /// </summary>
        protected virtual IEnumerable<TAssociativeTable> UpdateInternalVersoinedAssociations<TAssociativeTable>(DataContext context, Guid sourceKey, int versionSequence, IEnumerable<TAssociativeTable> associations)
            where TAssociativeTable : IDbVersionedAssociation, new()
        {
            // Ensure the source by locking the IEnumerable
            associations = associations.Select(a =>
            {
                if (a.SourceKey == Guid.Empty)
                {
                    a.SourceKey = sourceKey;
                }
                return a;
            }).ToArray();

            // Existing associations in the database
            var existing = context.Query<TAssociativeTable>(o => o.SourceKey == sourceKey && !o.ObsoleteVersionSequenceId.HasValue).ToArray();

            // Which ones are new?
            var removeRelationships = existing.Where(e => !associations.Any(a => a.Equals(e)));
            var addRelationships = associations.Where(a => !existing.Any(e => e.Equals(a)));

            // First, remove the old
            foreach (var itm in removeRelationships)
            {
                this.m_tracer.TraceVerbose("Will remove {0} of {1}", typeof(TAssociativeTable).Name, itm);

                if (this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.AssociationVersioning))
                {
                    itm.ObsoleteVersionSequenceId = versionSequence;
                    context.Update(itm);
                }
                else
                {
                    context.Delete(itm);
                }
            }

            // Next, add the new
            foreach (var itm in addRelationships)
            {
                this.m_tracer.TraceVerbose("Will add {0} of {1}", typeof(TAssociativeTable).Name, itm);
                itm.EffectiveVersionSequenceId = versionSequence;
                context.Insert(itm);
            }

            return existing.Where(o => !removeRelationships.Any(r => r.Equals(o))).Union(addRelationships);
        }
    }
}
