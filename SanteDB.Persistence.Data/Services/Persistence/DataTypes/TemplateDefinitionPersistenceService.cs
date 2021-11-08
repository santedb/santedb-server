using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Model.Extensibility;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// Template definition persistence services
    /// </summary>
    public class TemplateDefinitionPersistenceService : NonVersionedDataPersistenceService<TemplateDefinition, DbTemplateDefinition>
    {
        /// <summary>
        /// Persist the template definition to the database
        /// </summary>
        public TemplateDefinitionPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }
    }
}