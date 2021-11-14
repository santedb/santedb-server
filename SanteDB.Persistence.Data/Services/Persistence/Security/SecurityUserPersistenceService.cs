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
    /// Persistence service that works with SecurityApplication objects
    /// </summary>
    public class SecurityUserPersistenceService : NonVersionedDataPersistenceService<SecurityUser, DbSecurityUser>
    {
        /// <summary>
        /// DI injected constructor
        /// </summary>
        public SecurityUserPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        protected override SecurityUser BeforePersisting(DataContext context, SecurityUser data)
        {
            if (!String.IsNullOrEmpty(data.Password))
            {
                this.m_tracer.TraceWarning("Caller has set the Password property on SecurityUser instance. Use the IIdentityProvider.ChangePassword() method - this property will be ignored here");
                data.Password = null;
            }
            if (!String.IsNullOrEmpty(data.SecurityHash))
            {
                this.m_tracer.TraceWarning("Caller has set the SecurityHash property on SecurityUser instance - this property is for internal use and the setting of this property will be ignored here");
                data.SecurityHash = null;
            }

            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Called after persistence is completed
        /// </summary>
        protected override SecurityUser AfterPersisted(DataContext context, SecurityUser data)
        {
            data.Password = null;
            return base.AfterPersisted(context, data);
        }
    }
}