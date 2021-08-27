using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
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
        public ConceptRelationshipPersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Concept relationship persistence service
        /// </summary>
        protected override ConceptRelationship PrepareReferences(DataContext context, ConceptRelationship data)
        {
            data.RelationshipTypeKey = data.RelationshipType?.EnsureExists(context)?.Key ?? data.RelationshipTypeKey;
            data.TargetConceptKey = data.TargetConcept?.EnsureExists(context)?.Key ?? data.TargetConceptKey;
            return data;
        }
    }
}
