using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Model.Concepts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{

    /// <summary>
    /// Concept class persistence services
    /// </summary>
    public class ConceptClassPersistenceService : NonVersionedDataPersistenceService<ConceptClass, DbConceptClass>
    {
        /// <summary>
        /// Creates a dependency injected instance of the concept class perssitence service
        /// </summary>
        public ConceptClassPersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }
    }
}
