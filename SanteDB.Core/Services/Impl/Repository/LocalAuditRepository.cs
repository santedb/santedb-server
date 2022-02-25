/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */

using SanteDB.Core;
using SanteDB.Core.Model.Audit;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Server.Core.Security.Attribute;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SanteDB.Core.Diagnostics;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Represents an audit repository which stores and queries audit data.
    /// </summary>
    [ServiceProvider("Default Audit Repository")]
    public class LocalAuditRepository : IRepositoryService<AuditEventData>
    {
        // Localization Service
        private readonly ILocalizationService m_localizationService;

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(LocalAuditRepository));

        // Persistence service
        private readonly IDataPersistenceService<AuditEventData> m_persistenceService;

        /// <summary>
        /// Construct instance of LocalAuditRepository
        /// </summary>
        /// <param name="localizationService"></param>
        public LocalAuditRepository(ILocalizationService localizationService, IDataPersistenceService<AuditEventData> persistenceService)
        {
            this.m_localizationService = localizationService;
            this.m_persistenceService = persistenceService;
        }

        /// <summary>
        /// Gets the service name
        /// </summary>
        public String ServiceName => "Default Audit Repository";

        /// <summary>
        /// Find the specified data
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AccessAuditLog)]
        public IQueryResultSet<AuditEventData> Find(Expression<Func<AuditEventData, bool>> query)
        {
            var service = ApplicationServiceContext.Current.GetService<IDataPersistenceService<AuditEventData>>();
            if (service == null)
            {
                this.m_tracer.TraceError("Cannot find the data persistence service for audits");
                throw new InvalidOperationException(this.m_localizationService.GetString("error.server.core.auditPersistenceService"));
            }
            return service.Query(query, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Find with query controls
        /// </summary>
        [Obsolete("Use Find(Expression<Func<AuditEventData, bool>>)", true)]
        public IEnumerable<AuditEventData> Find(Expression<Func<AuditEventData, bool>> query, int offset, int? count, out int totalResults, params ModelSort<AuditEventData>[] orderBy)
        {
            var results = this.Find(query);

            if (results is IOrderableQueryResultSet<AuditEventData> orderable)
            {
                foreach (var itm in orderBy)
                {
                    switch (itm.SortOrder)
                    {
                        case SanteDB.Core.Model.Map.SortOrderType.OrderBy:
                            results = orderable.OrderBy(itm.SortProperty);
                            break;

                        case SanteDB.Core.Model.Map.SortOrderType.OrderByDescending:
                            results = orderable.OrderByDescending(itm.SortProperty);
                            break;
                    }
                }
            }

            totalResults = results.Count();
            return results.Skip(offset).Take(count ?? 100);
        }

        /// <summary>
        /// Gets the specified object
        /// </summary>
        public AuditEventData Get(Guid key)
        {
            return this.Get(key, Guid.Empty);
        }

        /// <summary>
        /// Gets the specified correlation key
        /// </summary>
        public AuditEventData Get(object correlationKey)
        {
            return this.Get((Guid)correlationKey, Guid.Empty);
        }

        /// <summary>
        /// Get the specified audit by key
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AccessAuditLog)]
        public AuditEventData Get(Guid key, Guid versionKey)
        {
            return this.m_persistenceService.Get(key, versionKey, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Insert the specified data
        /// </summary>
        public AuditEventData Insert(AuditEventData audit)
        {
            return this.m_persistenceService.Insert(audit, TransactionMode.Commit, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Obsolete the specified data
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AccessAuditLog)]
        public AuditEventData Delete(Guid key)
        {
            return this.m_persistenceService.Delete(key, TransactionMode.Commit, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Save (create or update) the specified object
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AccessAuditLog)]
        public AuditEventData Save(AuditEventData data)
        {
            var existing = this.m_persistenceService.Get(data.Key.Value, null, AuthenticationContext.Current.Principal);
            if (existing == null)
            {
                return this.m_persistenceService.Update(data, TransactionMode.Commit, AuthenticationContext.Current.Principal);
            }
            else
            {
                return this.m_persistenceService.Insert(data, TransactionMode.Commit, AuthenticationContext.Current.Principal);
            }
        }
    }
}