using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Concepts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// Concept relationship persistence service
    /// </summary>
    public class ConceptRelationshipPersistenceService : ConceptReferencePersistenceBase<ConceptRelationship, DbConceptRelationship>
    {
        /// <summary>
        /// Concept relationship persistence service
        /// </summary>
        public ConceptRelationshipPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Concept relationship persistence service
        /// </summary>
        protected override ConceptRelationship BeforePersisting(DataContext context, ConceptRelationship data)
        {
            data.RelationshipTypeKey = this.EnsureExists(context, data.RelationshipType)?.Key ?? data.RelationshipTypeKey;
            data.TargetConceptKey = this.EnsureExists(context, data.TargetConcept)?.Key ?? data.TargetConceptKey;
            return data;
        }

        /// <summary>
        /// Information model conversion
        /// </summary>
        protected override ConceptRelationship DoConvertToInformationModel(DataContext context, DbConceptRelationship dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            switch (DataPersistenceQueryContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.RelationshipType = retVal.RelationshipType.GetRelatedPersistenceService().Get(context, dbModel.RelationshipTypeKey);
                    retVal.SetLoaded(nameof(ConceptRelationship.RelationshipType));
                    retVal.TargetConcept = retVal.TargetConcept.GetRelatedPersistenceService().Get(context, dbModel.TargetKey);
                    retVal.SetLoaded(nameof(ConceptRelationship.TargetConcept));
                    break;
            }
            return retVal;
        }
    }
}