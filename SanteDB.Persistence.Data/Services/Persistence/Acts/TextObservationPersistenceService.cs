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
    /// Persistence service which can store and retrieve text observations
    /// </summary>
    public class TextObservationPersistenceService : ObservationDerivedPersistenceService<TextObservation, DbTextObservation>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public TextObservationPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }
        
        /// <inheritdoc/>
        protected override TextObservation DoConvertToInformationModelEx(DataContext context, DbActVersion dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            var obsData = referenceObjects.OfType<DbTextObservation>().FirstOrDefault();
            if(obsData == null)
            {
                this.m_tracer.TraceWarning("Using slow loading for text observation data");
                obsData = context.FirstOrDefault<DbTextObservation>(o => o.ParentKey == dbModel.VersionKey);
            }
            retVal.Value = obsData?.Value;
            return retVal;
        }
    }
}
