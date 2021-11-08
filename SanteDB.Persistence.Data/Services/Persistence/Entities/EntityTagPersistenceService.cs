using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Model.Extensibility;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// Entity tag persistence
    /// </summary>
    public class EntityTagPersistenceService : BaseEntityDataPersistenceService<EntityTag, DbEntityTag>
    {
        /// <summary>
        /// Create DI injected tag persistence service
        /// </summary>
        public EntityTagPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }
    }
}