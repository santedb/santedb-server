/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * User: fyfej
 * Date: 2022-9-7
 */
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