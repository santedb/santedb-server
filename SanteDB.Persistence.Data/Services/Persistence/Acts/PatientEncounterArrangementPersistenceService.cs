using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Acts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// Patient encounter arrangement persistence service
    /// </summary>
    public class PatientEncounterArrangementPersistenceService : ActAssociationPersistenceService<PatientEncounterArrangement, DbPatientEncounterArrangement>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public PatientEncounterArrangementPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override PatientEncounterArrangement BeforePersisting(DataContext context, PatientEncounterArrangement data)
        {
            data.ArrangementTypeKey = this.EnsureExists(context, data.ArrangementType)?.Key ?? data.ArrangementTypeKey;
            return base.BeforePersisting(context, data);
        }
    
        /// <inheritdoc/>
        protected override PatientEncounterArrangement DoConvertToInformationModel(DataContext context, DbPatientEncounterArrangement dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            if((DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy) == LoadMode.FullLoad)
            {
                retVal.ArrangementType = retVal.ArrangementType.GetRelatedPersistenceService().Get(context, dbModel.ArrangementTypeKey);
                retVal.SetLoaded(o => o.ArrangementType);
            }
            retVal.StartTime = dbModel.StartTime;
            retVal.StopTime = dbModel.StopTime;
            return retVal;
        }
    }
}
