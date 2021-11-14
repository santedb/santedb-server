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
    /// Persistence service that works with SecurityUser instances
    /// </summary>
    public class SecurityDevicePersistenceService : NonVersionedDataPersistenceService<SecurityDevice, DbSecurityDevice>
    {
        /// <summary>
        /// DI constructor for security device service
        /// </summary>
        public SecurityDevicePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Before persisting the object - remove sensitive fields
        /// </summary>
        protected override SecurityDevice BeforePersisting(DataContext context, SecurityDevice data)
        {
            if (!String.IsNullOrEmpty(data.DeviceSecret))
            {
                this.m_tracer.TraceWarning("Caller has set the DeviceSecret property on SecurityDevice - use the IDeviceIdentityProvider.ChangeSecret() for this - the property will be ignored");
                data.DeviceSecret = null;
            }
            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// After the device has been inserted - remove sensitive fields
        /// </summary>
        protected override SecurityDevice AfterPersisted(DataContext context, SecurityDevice data)
        {
            data.DeviceSecret = null;
            return base.AfterPersisted(context, data);
        }
    }
}