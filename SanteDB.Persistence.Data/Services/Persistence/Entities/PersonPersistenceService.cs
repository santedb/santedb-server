using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{

    /// <summary>
    /// Persistence service which is responsible for managing persons
    /// </summary>
    public class PersonPersistenceService : EntityDerivedPersistenceService<Person, DbPerson>
    {
        /// <inheritdoc/>
        public PersonPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc />
        protected override Person BeforePersisting(DataContext context, Person data)
        {
            data.OccupationKey = this.EnsureExists(context, data.Occupation)?.Key ?? data.OccupationKey;
            data.GenderConceptKey = this.EnsureExists(context, data.GenderConcept)?.Key ?? data.GenderConceptKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override Person DoInsertModel(DataContext context, Person data)
        {
            switch (data)
            {
                case Patient pat:
                    return pat.GetRelatedPersistenceService().Insert(context, pat);
                case Provider prov:
                    return prov.GetRelatedPersistenceService().Insert(context, prov);
                case UserEntity ue:
                    return ue.GetRelatedPersistenceService().Insert(context, ue);
                default:
                    var retVal = base.DoInsertModel(context, data);

                    if (data.LanguageCommunication != null)
                    {
                        retVal.LanguageCommunication = this.UpdateModelVersionedAssociations(context, retVal, data.LanguageCommunication).ToList();
                        retVal.SetLoaded(o => o.LanguageCommunication);
                    }

                    return retVal;
            }
        }

        /// <inheritdoc/>
        protected override Person DoUpdateModel(DataContext context, Person data)
        {
            switch (data)
            {
                case Patient pat:
                    return pat.GetRelatedPersistenceService().Update(context, pat);
                case Provider prov:
                    return prov.GetRelatedPersistenceService().Update(context, prov);
                case UserEntity ue:
                    return ue.GetRelatedPersistenceService().Update(context, ue);
                default:
                    var retVal = base.DoUpdateModel(context, data);

                    if (data.LanguageCommunication != null)
                    {
                        retVal.LanguageCommunication = this.UpdateModelVersionedAssociations(context, retVal, data.LanguageCommunication).ToList();
                        retVal.SetLoaded(o => o.LanguageCommunication);
                    }

                    return retVal;
            }
        }

        /// <inheritdoc/>
        protected override Person DoConvertToInformationModelEx(DataContext context, DbEntityVersion dbModel, params object[] referenceObjects)
        {
            var modelData = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            var personData = referenceObjects.OfType<DbPerson>().FirstOrDefault();
            if (personData == null)
            {
                this.m_tracer.TraceWarning("Using slow join to DbPerson from DbEntityVersion");
                personData = context.FirstOrDefault<DbPerson>(o => o.ParentKey == dbModel.VersionKey);
            }

            // Deep loading?
            switch (DataPersistenceQueryContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    modelData.GenderConcept = modelData.GenderConcept.GetRelatedPersistenceService().Get(context, personData.GenderConceptKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.GenderConcept);
                    modelData.Occupation = modelData.Occupation.GetRelatedPersistenceService().Get(context, personData.OccupationKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.Occupation);
                    goto case LoadMode.SyncLoad;
                case LoadMode.SyncLoad:
                    modelData.LanguageCommunication = modelData.LanguageCommunication.GetRelatedPersistenceService().Query(context, r => r.SourceEntityKey == dbModel.Key)?.ToList();
                    modelData.SetLoaded(o => o.LanguageCommunication);
                    break;
            }
            modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbPerson, Person>(personData), false, declaredOnly: true);
            return modelData;
        }
    }
}
