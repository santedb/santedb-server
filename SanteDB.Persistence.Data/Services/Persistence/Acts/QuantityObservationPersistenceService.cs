using SanteDB.Core.Model.Acts;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Acts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SanteDB.Core.Model;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// An observation persistence service which can manage observations which are quantities (value + unit)
    /// </summary>
    public class QuantityObservationPersistenceService : ObservationDerivedPersistenceService<QuantityObservation, DbQuantityObservation>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public QuantityObservationPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override QuantityObservation BeforePersisting(DataContext context, QuantityObservation data)
        {
            data.UnitOfMeasureKey = this.EnsureExists(context, data.UnitOfMeasure)?.Key ?? data.UnitOfMeasureKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override QuantityObservation DoConvertToInformationModelEx(DataContext context, DbActVersion dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            var obsData = referenceObjects.OfType<DbQuantityObservation>().FirstOrDefault();
            if(obsData == null)
            {
                this.m_tracer.TraceWarning("Using slow loading of observation data");
                obsData = context.FirstOrDefault<DbQuantityObservation>(o => o.ParentKey == dbModel.VersionKey);
            }

            if ((DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy) == LoadMode.FullLoad) 
            {
                retVal.UnitOfMeasure = retVal.UnitOfMeasure.GetRelatedPersistenceService().Get(context, obsData.UnitOfMeasureKey);
                retVal.SetLoaded(o => o.UnitOfMeasure);
            }
            retVal.Value = obsData.Value;
            return retVal;
        }
    }
}
