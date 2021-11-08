using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Model.Concepts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// A persistence service which stores and manages reference term display names
    /// </summary>
    public class ReferenceTermNamePersistenceService : BaseEntityDataPersistenceService<ReferenceTermName, DbReferenceTermName>
    {
        /// <summary>
        /// Creates a DI instance of this class
        /// </summary>
        public ReferenceTermNamePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }
    }
}