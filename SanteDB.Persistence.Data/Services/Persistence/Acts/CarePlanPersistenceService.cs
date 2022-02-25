using SanteDB.Core.Model.Acts;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Acts;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// Persistence service which stores care plans
    /// </summary>
    /// <remarks>
    /// The care plan storage has no specific storage requirements for care plans
    /// </remarks>
    public class CarePlanPersistenceService : ActDerivedPersistenceService<CarePlan>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public CarePlanPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override void DoCopyVersionSubTableInternal(DataContext context, DbActVersion newVersion)
        {
        }

    }
}
