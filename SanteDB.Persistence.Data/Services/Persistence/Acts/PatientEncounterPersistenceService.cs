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
    /// Patient encounter based persistence service
    /// </summary>
    public class PatientEncounterPersistenceService : ActDerivedPersistenceService<PatientEncounter, DbPatientEncounter>
    {
        /// <summary>
        /// DI Constructor
        /// </summary>
        public PatientEncounterPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override PatientEncounter DoInsertModel(DataContext context, PatientEncounter data)
        {
            var retVal = base.DoInsertModel(context, data);

            // Update the special arrangements
            if (data.SpecialArrangements != null)
            {
                retVal.SpecialArrangements = base.UpdateModelVersionedAssociations(context, retVal, data.SpecialArrangements).ToList();
            }

            return retVal;
        }

        /// <inheritdoc/>
        protected override PatientEncounter DoUpdateModel(DataContext context, PatientEncounter data)
        {
            var retVal = base.DoUpdateModel(context, data);

            // Update special arrangements
            if(data.SpecialArrangements != null)
            {
                retVal.SpecialArrangements = base.UpdateModelVersionedAssociations(context, retVal, data.SpecialArrangements).ToList();
            }

            return retVal;
        }

        /// <inheritdoc/>
        protected override void DoDeleteReferencesInternal(DataContext context, Guid key)
        {
            context.DeleteAll<DbPatientEncounterArrangement>(o => o.SourceKey == key);
            base.DoDeleteReferencesInternal(context, key);
        }

        /// <inheritdoc/>
        protected override PatientEncounter BeforePersisting(DataContext context, PatientEncounter data)
        {
            data.DischargeDispositionKey = this.EnsureExists(context, data.DischargeDisposition)?.Key ?? data.DischargeDispositionKey;
            data.AdmissionSourceTypeKey = this.EnsureExists(context, data.AdmissionSourceType)?.Key ?? data.AdmissionSourceTypeKey;
            if(data.SpecialArrangements != null)
            {
                foreach(var itm in data.SpecialArrangements)
                {
                    itm.ArrangementTypeKey = this.EnsureExists(context, itm.ArrangementType)?.Key ?? itm.ArrangementTypeKey;
                }
            }
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override PatientEncounter DoConvertToInformationModelEx(DataContext context, DbActVersion dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            var dbEncounter = referenceObjects.OfType<DbPatientEncounter>().FirstOrDefault();
            if(dbEncounter == null)
            {
                this.m_tracer.TraceWarning("Using slow loading of encounter data (hint: use the appropriate persistence API)");
                dbEncounter = context.FirstOrDefault<DbPatientEncounter>(o => o.ParentKey == dbModel.VersionKey);
            }

            switch(DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.DischargeDisposition = retVal.DischargeDisposition.GetRelatedPersistenceService().Get(context, dbEncounter.DischargeDispositionKey);
                    retVal.SetLoaded(o => o.DischargeDisposition);
                    retVal.AdmissionSourceType = retVal.AdmissionSourceType.GetRelatedPersistenceService().Get(context, dbEncounter.AdmissionSourceTypeKey);
                    retVal.SetLoaded(o => o.AdmissionSourceType);
                    goto case LoadMode.SyncLoad;
                case LoadMode.SyncLoad:
                    retVal.SpecialArrangements = retVal.SpecialArrangements.GetRelatedPersistenceService().Query(context, o => o.SourceEntityKey == dbModel.Key).ToList();
                   
                    break;
            }

            retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbPatientEncounter, PatientEncounter>(dbEncounter), declaredOnly: true);
            return retVal;
        }
    }
}
