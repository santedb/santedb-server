using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.DataType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// Persistence service for entity identifiers
    /// </summary>
    public class EntityIdentifierPersistenceService : EntityAssociationPersistenceService<EntityIdentifier, DbEntityIdentifier>
    {
        /// <summary>
        /// Dependency injection of configuration
        /// </summary>
        public EntityIdentifierPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare references
        /// </summary>
        protected override EntityIdentifier BeforePersisting(DataContext context, EntityIdentifier data)
        {
            data.AuthorityKey = this.EnsureExists(context, data.Authority)?.Key ?? data.AuthorityKey;
            data.IdentifierTypeKey = this.EnsureExists(context, data.IdentifierType)?.Key ?? data.IdentifierTypeKey;
            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Special ORM query provider that uses a composite result
        /// </summary>
        public override IOrmResultSet ExecuteQueryOrm(DataContext context, Expression<Func<EntityIdentifier, bool>> query)
        {
            return this.DoQueryInternalAs<CompositeResult<DbEntityIdentifier, DbAssigningAuthority>>(context, query,
                (o) =>
                {
                    var columns = TableMapping.Get(typeof(DbEntityIdentifier)).Columns.Union(
                        TableMapping.Get(typeof(DbAssigningAuthority)).Columns, new ColumnMapping.ColumnComparer());
                    var retVal = context.CreateSqlStatement().SelectFrom(typeof(DbEntityIdentifier), columns.ToArray())
                        .InnerJoin<DbEntityIdentifier, DbAssigningAuthority>(q => q.AuthorityKey, q => q.Key);
                    return retVal;
                });
        }

        /// <summary>
        /// Convert from db model to information model
        /// </summary>
        protected override EntityIdentifier DoConvertToInformationModel(DataContext context, DbEntityIdentifier dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.IdentifierType = retVal.IdentifierType.GetRelatedPersistenceService().Get(context, dbModel.TypeKey.GetValueOrDefault());
                    retVal.SetLoaded(nameof(EntityIdentifier.IdentifierType));
                    goto case LoadMode.SyncLoad;
                case LoadMode.SyncLoad:
                    retVal.Authority = retVal.Authority.GetRelatedMappingProvider().ToModelInstance(context, referenceObjects.OfType<DbAssigningAuthority>().FirstOrDefault()) ?? 
                        retVal.Authority.GetRelatedPersistenceService().Get(context, dbModel.AuthorityKey);
                    retVal.SetLoaded(nameof(EntityIdentifier.Authority));
                    break;

                case LoadMode.QuickLoad:
                    retVal.Authority = retVal.Authority.GetRelatedMappingProvider().ToModelInstance(context, referenceObjects.OfType<DbAssigningAuthority>().FirstOrDefault());
                    if (retVal.Authority != null)
                    {
                        retVal.SetLoaded(o => o.Authority);
                    }
                    break;
            }
            return retVal;
        }
    }
}