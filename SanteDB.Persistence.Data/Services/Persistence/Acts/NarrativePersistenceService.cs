using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Acts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// Persistence service that handles narratives
    /// </summary>
    public class NarrativePersistenceService : ActDerivedPersistenceService<Narrative, DbNarrative>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public NarrativePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override Narrative DoConvertToInformationModelEx(DataContext context, DbActVersion dbModel, params object[] referenceObjects)
        {
            var modelData = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            var narrativeData = referenceObjects.OfType<DbNarrative>().FirstOrDefault();
            if(narrativeData == null)
            {
                this.m_tracer.TraceWarning("Using slow method of loading DbNarrative data from DbActVersion - Consider using the Narrative persistence service instead");
                narrativeData = context.FirstOrDefault<DbNarrative>(o => o.ParentKey == dbModel.VersionKey);
            }

            modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbNarrative, Narrative>(narrativeData), declaredOnly: true);
            return modelData;
        }

    }
}
