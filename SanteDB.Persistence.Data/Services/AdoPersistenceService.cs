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
    public class AdoPersistenceService : ISqlDataPersistenceService
    {
        // Gets the configuration
        private AdoPersistenceConfigurationSection m_configuration;

        // Trace source for the service
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(AdoPersistenceService));

        // Mapper for this service
        private ModelMapper m_mapper;

        /// <summary>
        /// ADO Persistence service
        /// </summary>
        public AdoPersistenceService(IConfigurationManager configManager, IServiceManager serviceManager)
        {
            this.m_configuration = configManager.GetSection<AdoPersistenceConfigurationSection>();
            this.m_mapper = new ModelMapper(typeof(AdoPersistenceService).Assembly.GetManifestResourceStream(DataConstants.MapResourceName), "AdoModelMap");
            QueryBuilder.AddQueryHacks(serviceManager.CreateAll<IQueryBuilderHack>(this.m_mapper));

            // Upgrade the schema
            this.m_configuration.Provider.UpgradeSchema("SanteDB.Persistence.Data");

            // Iterate and register ADO data persistence services
            foreach (var pservice in serviceManager.CreateInjectedOfAll<IAdoPersistenceProvider>())
            {
                pservice.Provider = this.m_configuration.Provider;
                serviceManager.AddServiceProvider(pservice);
            }

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
    }
}