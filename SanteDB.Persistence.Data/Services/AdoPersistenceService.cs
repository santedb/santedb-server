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
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.OrmLite.Migration;
using SanteDB.Persistence.Data.Configuration;
using SanteDB.Persistence.Data.Services.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services
{
    /// <summary>
    /// A daemon service which registers the other persistence services
    /// </summary>
    public class AdoPersistenceService : ISqlDataPersistenceService, IServiceFactory
    {
        // Gets the configuration
        private AdoPersistenceConfigurationSection m_configuration;

        // Trace source for the service
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(AdoPersistenceService));

        // Mapper for this service
        private ModelMapper m_mapper;

        // Service manager
        private readonly IServiceManager m_serviceManager;

        // Service list
        private IList<IAdoPersistenceProvider> m_services = new List<IAdoPersistenceProvider>();

        /// <summary>
        /// ADO Persistence service
        /// </summary>
        public AdoPersistenceService(IConfigurationManager configManager, IServiceManager serviceManager)
        {
            this.m_configuration = configManager.GetSection<AdoPersistenceConfigurationSection>();
            this.m_mapper = new ModelMapper(typeof(AdoPersistenceService).Assembly.GetManifestResourceStream(DataConstants.MapResourceName), "AdoModelMap");
            this.m_serviceManager = serviceManager;
            QueryBuilder.AddQueryHacks(serviceManager.CreateAll<IQueryBuilderHack>(this.m_mapper));

            // Upgrade the schema
            this.m_configuration.Provider.UpgradeSchema("SanteDB.Persistence.Data");

            // Iterate and register ADO data persistence services
            foreach (var pservice in serviceManager.CreateInjectedOfAll<IAdoPersistenceProvider>())
            {
                pservice.Provider = this.m_configuration.Provider;
                serviceManager.AddServiceProvider(pservice);
                this.m_services.Add(pservice);
            }
            serviceManager.AddServiceProvider(typeof(TagPersistenceService));
            serviceManager.AddServiceProvider(typeof(AdoRelationshipValidationProvider));
            // TODO: Initialize further classes here
        }

        /// <summary>
        /// Gets the invariant name of this service
        /// </summary>
        public string InvariantName => this.m_configuration.Provider.Invariant;

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "ADO Persistence Service";

        /// <summary>
        /// Execute a non-query SQL script
        /// </summary>
        public void ExecuteNonQuery(string sql)
        {
            if (String.IsNullOrEmpty(sql))
            {
                throw new ArgumentNullException(nameof(sql));
            }

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    using (var tx = context.BeginTransaction())
                    {
                        context.ExecuteNonQuery(context.CreateSqlStatement(sql));
                        tx.Commit();
                    }
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error executing SQL statement {0} - {1}", sql, e.Message);
                    throw new DataPersistenceException("Error executing raw SQL", e);
                }
            }
        }

        /// <summary>
        /// Try to create the specified service
        /// </summary>
        public bool TryCreateService<TService>(out TService serviceInstance)
        {
            if(this.TryCreateService(typeof(TService), out var strongInstance)) {
                serviceInstance = (TService)strongInstance;
                return true;
            }
            serviceInstance = default(TService);
            return false;
        }

        /// <summary>
        /// Try to create the specified service
        /// </summary>
        public bool TryCreateService(Type serviceType, out object serviceInstance)
        {
            serviceInstance = this.m_services.FirstOrDefault(o => serviceType.IsAssignableFrom(o.GetType()));
            return serviceInstance != null;
        }
    }
}