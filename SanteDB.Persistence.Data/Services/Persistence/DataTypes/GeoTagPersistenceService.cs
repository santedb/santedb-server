using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Model.DataType;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// GEO Tag Persistence
    /// </summary>
    public class GeoTagPersistenceService : IdentifiedDataPersistenceService<GeoTag, DbGeoTag>
    {
        /// <summary>
        /// Creats a new DI injected instance of the persister
        /// </summary>
        public GeoTagPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }
    }
}