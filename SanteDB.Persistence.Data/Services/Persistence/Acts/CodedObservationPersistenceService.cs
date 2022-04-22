using SanteDB.Core.Model.Acts;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Acts;
using SanteDB.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// A <see cref="ActDerivedPersistenceService{TAct, TDbActSubTable}"/> which stores coded observations
    /// </summary>
    public class CodedObservationPersistenceService : ObservationDerivedPersistenceService<CodedObservation, DbCodedObservation>
    {
        /// <summary>
        /// DI Constructor
        /// </summary>
        public CodedObservationPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override CodedObservation BeforePersisting(DataContext context, CodedObservation data)
        {
            data.ValueKey = this.EnsureExists(context, data.Value)?.Key ?? data.ValueKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override CodedObservation DoConvertToInformationModelEx(DataContext context, DbActVersion dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            var obsData = referenceObjects.OfType<DbCodedObservation>().FirstOrDefault();
            if(obsData == null)
            {
                this.m_tracer.TraceWarning("Performing slow load of coded observation data from database");
                obsData = context.FirstOrDefault<DbCodedObservation>(o => o.ParentKey == dbModel.VersionKey);
            }

            if((DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy) == LoadMode.FullLoad)
            {
                retVal.Value = retVal.Value.GetRelatedPersistenceService().Get(context, obsData?.Value ?? Guid.Empty);
                retVal.SetLoaded(o => o.Value);
            }

            retVal.ValueKey = obsData?.Value;
            return retVal;
        }
    }
}
