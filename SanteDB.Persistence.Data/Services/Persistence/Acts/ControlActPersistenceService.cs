using SanteDB.Core.Model.Acts;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Model.Acts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// Persistence service which stores controlling acts 
    /// </summary>
    public class ControlActPersistenceService : ActDerivedPersistenceService<ControlAct, DbControlAct>
    {
        /// <summary>
        /// DI Constructor
        /// </summary>
        public ControlActPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }
    }
}
