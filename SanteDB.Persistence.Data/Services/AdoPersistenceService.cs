using SanteDB.Core.Diagnostics;
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
        private Tracer m_tracer = Tracer.GetTracer(typeof(AdoPersistenceService));

        // The query builder for this service
        private QueryBuilder m_queryBuilder;

        // Mapper for this service
        private ModelMapper m_mapper;

        /// <summary>
        /// ADO Persistence service
        /// </summary>
        public AdoPersistenceService(IConfigurationManager configManager, IServiceManager serviceManager)
        {
            this.m_configuration = configManager.GetSection<AdoPersistenceConfigurationSection>();
            this.m_mapper = new ModelMapper(typeof(AdoPersistenceService).Assembly.GetManifestResourceStream(DataConstants.MapResourceName), "AdoModelMap");
            this.m_queryBuilder = new QueryBuilder(this.m_mapper, this.m_configuration.Provider, serviceManager.CreateAll<IQueryBuilderHack>(this.m_mapper).ToArray());

            // Upgrade the schema
            this.m_configuration.Provider.UpgradeSchema("SanteDB.Persistence.Data");

            // Iterate and register ADO data persistence services
            //foreach(var type in serviceManager.GetAllTypes().Where(t=>typeof(IAdoPersistenceService).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface))
            //{
            //    serviceManager.AddServiceProvider(serviceManager.CreateInjected(type));
            //}

            // Now we want to register secondary types - These are types for which there is a registered database model map but not 
            // a concrete implementation class

        }

        /// <summary>
        /// Gets the invariant name of this service
        /// </summary>
        public string InvariantName => this.m_configuration.Provider.Invariant;

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "ADO Persistence Service";


        public void ExecuteNonQuery(string sql)
        {
            throw new NotImplementedException();
        }
    
    }
}
