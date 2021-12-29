﻿using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Security
{
    /// <summary>
    /// Persistence service that works with SecurityPolicy objects
    /// </summary>
    public class SecurityPolicyPersistenceService : BaseEntityDataPersistenceService<SecurityPolicy, DbSecurityPolicy>
    {
        /// <summary>
        /// DI injected constructor
        /// </summary>
        public SecurityPolicyPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

    }
}