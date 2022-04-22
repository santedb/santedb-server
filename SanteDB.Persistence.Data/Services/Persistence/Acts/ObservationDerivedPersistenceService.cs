using SanteDB.Core.Model.Acts;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Acts;
using SanteDB.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// Represents a persistence service which stores and retrieves observation based table
    /// </summary>
    public abstract class ObservationDerivedPersistenceService<TModel, TDbModel> : ActDerivedPersistenceService<TModel, TDbModel, DbObservation>
        where TDbModel : DbObsSubTable, new()
        where TModel : Observation, new()
    {
        /// <summary>
        /// DI Constructor
        /// </summary>
        protected ObservationDerivedPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override TModel BeforePersisting(DataContext context, TModel data)
        {
            data.InterpretationConceptKey = this.EnsureExists(context, data.InterpretationConcept)?.Key ?? data.InterpretationConceptKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override TModel DoConvertToInformationModelEx(DataContext context, DbActVersion dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            var obsData = referenceObjects.OfType<DbObservation>().FirstOrDefault();
            if(obsData == null)
            {
                this.m_tracer.TraceWarning("Using slow lookup of observation data from database");
                obsData = context.FirstOrDefault<DbObservation>(o => o.ParentKey == dbModel.VersionKey);
            }

            if((DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy) == LoadMode.FullLoad)
            {
                retVal.InterpretationConcept = retVal.InterpretationConcept.GetRelatedPersistenceService().Get(context, retVal.InterpretationConceptKey.GetValueOrDefault());
                retVal.SetLoaded(o => o.InterpretationConcept);
            }

            retVal.InterpretationConceptKey = obsData.InterpretationConceptKey;
            retVal.ValueType = obsData.ValueType;

            return retVal;
        }
    }
}
