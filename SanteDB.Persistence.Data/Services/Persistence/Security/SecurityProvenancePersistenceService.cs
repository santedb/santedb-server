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
using SanteDB.Core.i18n;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
        public SecurityProvenancePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Update provenance not supported
        /// </summary>
        protected override DbSecurityProvenance DoUpdateInternal(DataContext context, DbSecurityProvenance model)
        {
            throw new NotSupportedException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_PERMITTED));
        }

        /// <summary>
        /// Obsoletion of provenance not supported
        /// </summary>
        protected override DbSecurityProvenance DoDeleteInternal(DataContext context, Guid key, DeleteMode deletionMode)
        {
            throw new NotSupportedException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_PERMITTED));
        }

        /// <summary>
        /// Obsoletion of provenance not supported
        /// </summary>
        protected override IEnumerable<DbSecurityProvenance> DoDeleteAllInternal(DataContext context, Expression<Func<SecurityProvenance, bool>> expression, DeleteMode deleteMode)
        {
            // The user may be trying to purge old provenance objects
            if (deleteMode == DeleteMode.PermanentDelete) // this statement will fail due to RI in the database anyways - so just send it
            {
                return base.DoDeleteAllInternal(context, expression, deleteMode);
            }
            else
            {
                throw new NotSupportedException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_PERMITTED));
            }
        }
    }
}