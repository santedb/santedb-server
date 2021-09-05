using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.DataType;
using System;
using System.Collections.Generic;
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
        public EntityIdentifierPersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare references
        /// </summary>
        protected override EntityIdentifier PrepareReferences(DataContext context, EntityIdentifier data)
        {
            data.AuthorityKey = this.EnsureExists(context, data.Authority)?.Key ?? data.AuthorityKey;
            data.IdentifierTypeKey = this.EnsureExists(context, data.IdentifierType)?.Key ?? data.IdentifierTypeKey;
            return base.PrepareReferences(context, data);
        }

        /// <summary>
        /// Convert from db model to information model
        /// </summary>
        protected override EntityIdentifier DoConvertToInformationModel(DataContext context, DbEntityIdentifier dbModel, params IDbIdentified[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            switch (this.m_configuration.LoadStrategy)
            {
                case Configuration.LoadStrategyType.SyncLoad:
                    retVal.Authority = this.GetRelatedPersistenceService<AssigningAuthority>().Get(context, dbModel.AuthorityKey, null);
                    goto case Configuration.LoadStrategyType.FullLoad;
                case Configuration.LoadStrategyType.FullLoad:
                    retVal.IdentifierType = this.GetRelatedPersistenceService<IdentifierType>().Get(context, dbModel.TypeKey.GetValueOrDefault(), null);
                    break;
            }
            return retVal;
        }
    }
}
