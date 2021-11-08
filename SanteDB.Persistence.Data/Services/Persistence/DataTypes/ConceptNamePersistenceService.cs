using SanteDB.Core.i18n;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Concepts;
using System;
using System.Collections.Generic;
using System.Text;
using SanteDB.Core.Model;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// Concept name persistence services
    /// </summary>
    public class ConceptNamePersistenceService : ConceptReferencePersistenceBase<ConceptName, DbConceptName>
    {
        /// <summary>
        /// Creates a DI instance of the concept name
        /// </summary>
        public ConceptNamePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }
    }
}