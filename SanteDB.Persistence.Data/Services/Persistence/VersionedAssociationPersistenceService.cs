using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence
{
    /// <summary>
    /// Abstract class for versioned associations
    /// </summary>
    public abstract class VersionedAssociationPersistenceService<TModel, TDbModel>
        : IdentifiedDataPersistenceService<TModel, TDbModel>
        where TModel : IdentifiedData, IVersionedAssociation, new()
        where TDbModel : DbIdentified, IDbVersionedAssociation, new()
    {
        /// <summary>
        /// Creates a DI instance of hte persistence layer
        /// </summary>
        public VersionedAssociationPersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Get the current version sequence for the source key
        /// </summary>
        protected abstract int GetCurrentVersionSequenceForSource(DataContext context, Guid sourceKey);

        /// <summary>
        /// Perform an obsoletion of the association
        /// </summary>
        protected override TDbModel DoObsoleteInternal(DataContext context, Guid key)
        {

            if(context == null)
            {
                throw new ArgumentNullException(ErrorMessages.ERR_ARGUMENT_NULL, nameof(context));
            }
            else if(key == default(Guid))
            {
                throw new ArgumentException(ErrorMessages.ERR_ARGUMENT_RANGE, nameof(key));
            }

            // Versioning in place? if so obsolete is update
            if (this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.AssociationVersioning))
            {
                var existing = context.FirstOrDefault<TDbModel>(o => o.Key == key);

                // Get the source table
                existing.ObsoleteVersionSequenceId = this.GetCurrentVersionSequenceForSource(context, existing.SourceKey);
                return this.DoUpdateInternal(context, existing);
            }
            else
            {
                return base.DoObsoleteInternal(context, key);
            }
        }

        /// <summary>
        /// Perform an insertion of the object
        /// </summary>
        protected override TDbModel DoInsertInternal(DataContext context, TDbModel dbModel)
        {
            if(context == null)
            {
                throw new ArgumentNullException(ErrorMessages.ERR_ARGUMENT_NULL, nameof(context));
            }
            else if (dbModel == null)
            {
                throw new ArgumentNullException(ErrorMessages.ERR_ARGUMENT_NULL, nameof(dbModel));
            }

            // Effective seq set?
            if(dbModel.EffectiveVersionSequenceId == default(int))
            {
                dbModel.EffectiveVersionSequenceId = this.GetCurrentVersionSequenceForSource(context, dbModel.SourceKey);
            }

            return base.DoInsertInternal(context, dbModel);
        }

        /// <summary>
        /// Perform a query (appends the filter for obsolete sequence)
        /// </summary>
        protected override OrmResultSet<TDbModel> DoQueryInternal(DataContext context, Expression<Func<TModel, bool>> query, bool allowCache = false)
        {

            if(context == null)
            {
                throw new ArgumentNullException(ErrorMessages.ERR_ARGUMENT_NULL, nameof(context));
            }
            else if (query == null)
            {
                throw new ArgumentNullException(ErrorMessages.ERR_ARGUMENT_NULL, nameof(query));
            }

            // TODO: Write a utility function that looks for this
            if(!query.ToString().Contains(nameof(IVersionedAssociation.ObsoleteVersionSequenceId)))
            {
                var obsoletionVersionSequenceClause = Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(IVersionedAssociation.ObsoleteVersionSequenceId)));
                query = Expression.Lambda<Func<TModel, bool>>(Expression.And(query.Body, Expression.MakeBinary(ExpressionType.Equal, obsoletionVersionSequenceClause, Expression.Constant(null))), query.Parameters);
            }
            return base.DoQueryInternal(context, query, allowCache);
        }
    }
}
