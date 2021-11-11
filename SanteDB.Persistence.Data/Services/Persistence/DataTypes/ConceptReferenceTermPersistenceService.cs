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
    /// Concept to Reference term persistence service
    /// </summary>
    public class ConceptReferenceTermPersistenceService : ConceptReferencePersistenceBase<ConceptReferenceTerm, DbConceptReferenceTerm>
    {
        /// <summary>
        /// Concept reference term persistence
        /// </summary>
        public ConceptReferenceTermPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare references for this object
        /// </summary>
        protected override ConceptReferenceTerm PrepareReferences(DataContext context, ConceptReferenceTerm data)
        {
            data.RelationshipTypeKey = this.EnsureExists(context, data.RelationshipType)?.Key ?? data.RelationshipTypeKey;
            data.ReferenceTermKey = this.EnsureExists(context, data.ReferenceTerm)?.Key ?? data.ReferenceTermKey;
            return data;
        }

        /// <summary>
        /// Convert to information model
        /// </summary>
        protected override ConceptReferenceTerm DoConvertToInformationModel(DataContext context, DbConceptReferenceTerm dbModel, params IDbIdentified[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch (this.m_configuration.LoadStrategy)
            {
                case Configuration.LoadStrategyType.FullLoad:
                    retVal.RelationshipType = base.GetRelatedPersistenceService<ConceptRelationshipType>().Get(context, dbModel.RelationshipTypeKey);
                    retVal.SetLoaded(nameof(ConceptReferenceTerm.RelationshipType));
                    break;
            }
            return retVal;
        }
    }
}