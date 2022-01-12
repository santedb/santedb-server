﻿/*
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
using SanteDB.Core.Jobs;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.ADO.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Represents a basic ADO FreeText search service which really just filters on database tables
    /// </summary>
    public class AdoFreetextSearchService : IFreetextSearchService
    {
        private readonly ISqlDataPersistenceService m_sqlDataPersistence;
        private readonly IThreadPoolService m_threadPool;

        /// <summary>
        /// Gets the name of the service
        /// </summary>
        public string ServiceName => "ADO.NET Freetext Search Service";

        /// <summary>
        /// ADO freetext constructor
        /// </summary>
        public AdoFreetextSearchService(IJobManagerService jobManager, IServiceManager serviceManager, ISqlDataPersistenceService sqlDataPersistence, IThreadPoolService threadPoolService)
        {
            this.m_sqlDataPersistence = sqlDataPersistence;
            this.m_threadPool = threadPoolService;

            var job = serviceManager.CreateInjected<AdoRebuildFreetextIndexJob>();
            jobManager.AddJob(job, JobStartType.TimerOnly);
            if (jobManager.GetJobSchedules(job)?.Any() != true)
            {
                jobManager.SetJobSchedule(job, new DayOfWeek[] { DayOfWeek.Saturday }, new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0));
            }

            // Subscribe to common types and events
            var appServiceProvider = ApplicationServiceContext.Current;
            appServiceProvider.GetService<IDataPersistenceService<Bundle>>().Inserted += (o, e) => e.Data.Item.ForEach(i => this.ReIndex(i));
            appServiceProvider.GetService<IDataPersistenceService<Patient>>().Inserted += (o, e) => this.ReIndex(e.Data);
            appServiceProvider.GetService<IDataPersistenceService<Provider>>().Inserted += (o, e) => this.ReIndex(e.Data);
            appServiceProvider.GetService<IDataPersistenceService<Material>>().Inserted += (o, e) => this.ReIndex(e.Data);
            appServiceProvider.GetService<IDataPersistenceService<ManufacturedMaterial>>().Inserted += (o, e) => this.ReIndex(e.Data);
            appServiceProvider.GetService<IDataPersistenceService<Entity>>().Inserted += (o, e) => this.ReIndex(e.Data);
            appServiceProvider.GetService<IDataPersistenceService<Place>>().Inserted += (o, e) => this.ReIndex(e.Data);
            appServiceProvider.GetService<IDataPersistenceService<Organization>>().Inserted += (o, e) => this.ReIndex(e.Data);
            appServiceProvider.GetService<IDataPersistenceService<Person>>().Inserted += (o, e) => this.ReIndex(e.Data);
            appServiceProvider.GetService<IDataPersistenceService<Patient>>().Updated += (o, e) => this.ReIndex(e.Data);
            appServiceProvider.GetService<IDataPersistenceService<Provider>>().Updated += (o, e) => this.ReIndex(e.Data);
            appServiceProvider.GetService<IDataPersistenceService<Material>>().Updated += (o, e) => this.ReIndex(e.Data);
            appServiceProvider.GetService<IDataPersistenceService<ManufacturedMaterial>>().Updated += (o, e) => this.ReIndex(e.Data);
            appServiceProvider.GetService<IDataPersistenceService<Entity>>().Updated += (o, e) => this.ReIndex(e.Data);
            appServiceProvider.GetService<IDataPersistenceService<Place>>().Updated += (o, e) => this.ReIndex(e.Data);
            appServiceProvider.GetService<IDataPersistenceService<Organization>>().Updated += (o, e) => this.ReIndex(e.Data);
            appServiceProvider.GetService<IDataPersistenceService<Person>>().Updated += (o, e) => this.ReIndex(e.Data);
        }
        /// <summary>
        /// Search for the specified object in the list of terms
        /// </summary>
        public IEnumerable<TEntity> Search<TEntity>(string[] term, Guid queryId, int offset, int? count, out int totalResults, ModelSort<TEntity>[] orderBy) where TEntity : IdentifiedData, new()
        {

            // Does the provider support freetext search clauses?
            var idps = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TEntity>>();
            if (idps == null)
                throw new InvalidOperationException("Cannot find a UNION query repository service");

            var searchTerm = String.Join(" and ", term);
            return idps.Query(o => o.FreetextSearch(searchTerm), offset, count, out totalResults, AuthenticationContext.Current.Principal, orderBy);
        }

        /// <summary>
        /// Reindex the entity 
        /// </summary>
        public void ReIndex<TEntity>(TEntity entity) where TEntity : IdentifiedData
        {
            // TODO: Detect type and reindex based on type
            this.m_threadPool.QueueUserWorkItem(p =>
            {
                using (AuthenticationContext.EnterSystemContext())
                {
                    this.m_sqlDataPersistence.ExecuteNonQuery($"SELECT reindex_fti_ent('{p}')");
                }
            }, entity.Key);
        }
    }
}
