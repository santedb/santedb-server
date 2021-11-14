using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Security
{
    /// <summary>
    /// A persistence service that handles security applications
    /// </summary>
    public class SecurityApplicationPersistenceService : NonVersionedDataPersistenceService<SecurityApplication, DbSecurityApplication>
    {
        /// <summary>
        /// Security application persistence DI constructor
        /// </summary>
        public SecurityApplicationPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Before persisting the object
        /// </summary>
        protected override SecurityApplication BeforePersisting(DataContext context, SecurityApplication data)
        {
            if (!String.IsNullOrEmpty(data.ApplicationSecret))
            {
                this.m_tracer.TraceWarning("Caller has set ApplicationSecret on the SecurityApplication instance - this will be ignored");
                data.ApplicationSecret = null;
            }
            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// After being persisted
        /// </summary>
        protected override SecurityApplication AfterPersisted(DataContext context, SecurityApplication data)
        {
            data.ApplicationSecret = null;
            return base.AfterPersisted(context, data);
        }
    }
}