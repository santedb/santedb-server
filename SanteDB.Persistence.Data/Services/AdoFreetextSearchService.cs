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
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Event;
using SanteDB.Core.Jobs;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite.Providers;
using SanteDB.Persistence.Data.Jobs;
using SanteDB.Persistence.Data.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Services
{
    /// <summary>
    /// An implementation of the <see cref="IFreetextSearchService"/> which uses the underlying built-in database functionality
    /// </summary>
    /// <remarks>
    /// <para>This implementation of the free-text search service relies on a <see cref="IDbProvider"/> implementation 
    /// providing a <see cref="IDbFilterFunction"/> with the identifier <c>freetext</c>. The call is equivalent to using the 
    /// HDSI query expression: <c>id=:(freetext|$term)</c> and uses SanteDB's extended filter function (see an example
    /// of <see href="https://help.santesuite.org/developers/server-plugins/custom-algorithms#implementing-custom-idbfilterfunction">implementing a custom IDbFilterFunction</see></para>
    /// </remarks>
    public class AdoFreetextSearchService : IFreetextSearchService
    {
        private readonly AdoPersistenceConfigurationSection m_configuration;
        private readonly IThreadPoolService m_threadPool;
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(AdoFreetextSearchService));

        /// <summary>
        /// Gets the name of the service
        /// </summary>
        public string ServiceName => "ADO.NET Freetext Search Service";

        /// <summary>
        /// ADO freetext constructor
        /// </summary>
        public AdoFreetextSearchService(IJobManagerService jobManager, IServiceManager serviceManager, IConfigurationManager configurationManager, IThreadPoolService threadPoolService)
        {
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
            this.m_threadPool = threadPoolService;

            if (this.m_configuration.Provider.GetFilterFunction("freetext") == null) return; // Freetext not supported

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
        public IQueryResultSet<TEntity> SearchEntity<TEntity>(string[] term) where TEntity : Entity, new()
        {

            // Does the provider support freetext search clauses?
            var idps = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TEntity>>();
            if (idps == null)
                throw new InvalidOperationException("Cannot find a UNION query repository service");

            var searchTerm = String.Join(" and ", term.SelectMany(t=>t.Split(' ')));
            return idps.Query(o => o.FreetextSearch(searchTerm), AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Reindex the entity 
        /// </summary>
        public void ReIndex<TEntity>(TEntity entity) where TEntity : IdentifiedData
        {
            if (this.m_configuration.Provider.GetFilterFunction("freetext") != null)
            {
                // TODO: Detect type and reindex based on type
                this.m_threadPool.QueueUserWorkItem(p =>
                {
                    using (var ctx = this.m_configuration.Provider.GetWriteConnection())
                    {
                        try
                        {
                            ctx.Open();
                            ctx.ExecuteProcedure<object>("reindex_fti_ent", p);
                        }
                        catch (Exception e)
                        {
                            this.m_tracer.TraceWarning("Could not refresh fulltext index - {0}", e.Message);
                        }
                    }
                }, entity.Key);
            }
        }

    }
}
