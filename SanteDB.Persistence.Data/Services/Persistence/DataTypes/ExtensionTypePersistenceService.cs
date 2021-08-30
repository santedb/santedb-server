using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Model.Extensibility;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// Extension type persistence
    /// </summary>
    public class ExtensionTypePersistenceService : NonVersionedDataPersistenceService<ExtensionType, DbExtensionType>
    {
        /// <summary>
        /// Creates a DI injected extension type
        /// </summary>
        public ExtensionTypePersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }


    }
}
