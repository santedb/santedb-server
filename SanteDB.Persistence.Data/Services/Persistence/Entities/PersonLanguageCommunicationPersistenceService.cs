using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Model.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// Persistence service for language of communication
    /// </summary>
    public class PersonLanguageCommunicationPersistenceService : EntityAssociationPersistenceService<PersonLanguageCommunication, DbPersonLanguageCommunication>
    {
        /// <inheritdoc/>
        public PersonLanguageCommunicationPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }
    }
}
