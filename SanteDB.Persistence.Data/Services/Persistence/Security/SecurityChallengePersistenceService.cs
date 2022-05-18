using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Security
{
    /// <summary>
    /// Persistence service that stores and retrieves security challenges
    /// </summary>
    public class SecurityChallengePersistenceService : NonVersionedDataPersistenceService<SecurityChallenge, DbSecurityChallenge>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public SecurityChallengePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }


    }
}
