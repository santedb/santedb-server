using SanteDB.Core.i18n;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Security
{
    /// <summary>
    /// Security provenance persistence service
    /// </summary>
    public class SecurityProvenancePersistenceService : IdentifiedDataPersistenceService<SecurityProvenance, DbSecurityProvenance>
    {
        /// <summary>
        /// Creates a new persistence service for security provenance
        /// </summary>
        public SecurityProvenancePersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Update provenance not supported
        /// </summary>
        protected override DbSecurityProvenance DoUpdateInternal(DataContext context, DbSecurityProvenance model)
        {
            throw new NotSupportedException(ErrorMessages.ERR_NOT_PERMITTED);
        }

        /// <summary>
        /// Obsoletion of provenance not supported
        /// </summary>
        protected override DbSecurityProvenance DoObsoleteInternal(DataContext context, Guid key)
        {
            throw new NotSupportedException(ErrorMessages.ERR_NOT_PERMITTED);
        }
    }
}
