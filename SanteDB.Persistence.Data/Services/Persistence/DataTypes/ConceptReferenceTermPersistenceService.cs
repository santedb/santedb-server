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
        public ConceptReferenceTermPersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare references for this object
        /// </summary>
        protected override ConceptReferenceTerm PrepareReferences(DataContext context, ConceptReferenceTerm data)
        {
            data.RelationshipTypeKey = data.RelationshipType?.EnsureExists(context)?.Key ?? data.RelationshipTypeKey;
            data.ReferenceTermKey = data.RelationshipType?.EnsureExists(context)?.Key ?? data.ReferenceTermKey;
            return data;
        }

        /// <summary>
        /// Convert to information model
        /// </summary>
        protected override ConceptReferenceTerm DoConvertToInformationModel(DataContext context, DbConceptReferenceTerm dbModel, params IDbIdentified[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch(this.m_configuration.LoadStrategy)
            {
                case Configuration.LoadStrategyType.FullLoad:
                    retVal.RelationshipType = base.GetRelatedPersistenceService<ConceptRelationshipType>().Get(context, dbModel.RelationshipTypeKey, null);
                    break;

            }
            return retVal;
        }

    }
}
