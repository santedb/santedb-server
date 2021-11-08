using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Model.Extensibility;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// A tag persistence service for ActTags
    /// </summary>
    public class ActTagPersistenceService : BaseEntityDataPersistenceService<ActTag, DbActTag>
    {
        /// <summary>
        /// Create a DI injected instance of the act tag persistence service
        /// </summary>
        public ActTagPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCaching = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCaching, queryPersistence)
        {
        }
    }
}