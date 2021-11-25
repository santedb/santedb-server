using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// A persistence service which handles entity relationships
    /// </summary>
    public class EntityRelationshipPersistenceService : EntityAssociationPersistenceService<EntityRelationship, DbEntityRelationship>
    {
        /// <summary>
        /// Entity relationship persistence service
        /// </summary>
        public EntityRelationshipPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare references
        /// </summary>
        protected override EntityRelationship BeforePersisting(DataContext context, EntityRelationship data)
        {
            data.ClassificationKey = this.EnsureExists(context, data.Classification)?.Key ?? data.ClassificationKey;
            data.RelationshipRoleKey = this.EnsureExists(context, data.RelationshipRole)?.Key ?? data.RelationshipRoleKey;
            data.RelationshipTypeKey = this.EnsureExists(context, data.RelationshipType)?.Key ?? data.RelationshipTypeKey;
            data.TargetEntityKey = this.EnsureExists(context, data.TargetEntity)?.Key ?? data.TargetEntityKey;
            data.HolderKey = this.EnsureExists(context, data.Holder)?.Key ?? data.HolderKey;
            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Convert to information model
        /// </summary>
        protected override EntityRelationship DoConvertToInformationModel(DataContext context, DbEntityRelationship dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch (this.m_configuration.LoadStrategy)
            {
                case Configuration.LoadStrategyType.FullLoad:
                    retVal.TargetEntity = this.GetRelatedPersistenceService<Entity>().Get(context, dbModel.TargetKey);
                    retVal.SetLoaded(nameof(EntityRelationship.TargetEntity));
                    retVal.Classification = this.GetRelatedPersistenceService<Concept>().Get(context, dbModel.ClassificationKey.GetValueOrDefault());
                    retVal.SetLoaded(nameof(EntityRelationship.Classification));
                    retVal.RelationshipRole = this.GetRelatedPersistenceService<Concept>().Get(context, dbModel.RelationshipRoleKey.GetValueOrDefault());
                    break;
            }

            return retVal;
        }
    }
}