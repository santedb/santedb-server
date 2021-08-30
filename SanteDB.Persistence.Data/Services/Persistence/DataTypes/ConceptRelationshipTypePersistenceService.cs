using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Model.Concepts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// Concept relationship type persistnece
    /// </summary>
    public class ConceptRelationshipTypePersistenceService : NonVersionedDataPersistenceService<ConceptRelationshipType, DbConceptRelationshipType>
    {
        /// <summary>
        /// Concept relationship type persistence CTOR with DI services
        /// </summary>
        public ConceptRelationshipTypePersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }
    }
}
