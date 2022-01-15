using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Acts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// Persistence service between act and act relationship
    /// </summary>
    public class ActRelationshipPersistenceService : ActAssociationPersistenceService<ActRelationship, DbActRelationship>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public ActRelationshipPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }


        /// <summary>
        /// Prepare references
        /// </summary>
        protected override ActRelationship BeforePersisting(DataContext context, ActRelationship data)
        {
            data.ClassificationKey = this.EnsureExists(context, data.Classification)?.Key ?? data.ClassificationKey;
            data.RelationshipTypeKey = this.EnsureExists(context, data.RelationshipType)?.Key ?? data.RelationshipTypeKey;
            data.TargetActKey= this.EnsureExists(context, data.TargetAct)?.Key ?? data.TargetActKey;
            data.SourceEntityKey = this.EnsureExists(context, data.SourceEntity)?.Key ?? data.SourceEntityKey;
            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Convert to information model
        /// </summary>
        protected override ActRelationship DoConvertToInformationModel(DataContext context, DbActRelationship dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch (DataPersistenceQueryContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.TargetAct = retVal.TargetAct.GetRelatedPersistenceService().Get(context, dbModel.TargetKey);
                    retVal.SetLoaded(o=>o.TargetAct);
                    retVal.Classification = retVal.Classification.GetRelatedPersistenceService().Get(context, dbModel.ClassificationKey.GetValueOrDefault());
                    retVal.SetLoaded(o=>o.Classification);
                    retVal.RelationshipType = retVal.RelationshipType.GetRelatedPersistenceService().Get(context, dbModel.RelationshipTypeKey);
                    retVal.SetLoaded(o => o.RelationshipType);
                    break;
            }

            return retVal;
        }
    }
}
