using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// Place vs/ service persistence manager
    /// </summary>
    public class PlaceServicePersistenceService : EntityAssociationPersistenceService<PlaceService, DbPlaceService>
    {
        /// <inheritdoc />
        public PlaceServicePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc />
        protected override PlaceService BeforePersisting(DataContext context, PlaceService data)
        {
            data.ServiceConceptKey = this.EnsureExists(context, data.ServiceConcept)?.Key ?? data.ServiceConceptKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc />
        protected override PlaceService DoConvertToInformationModel(DataContext context, DbPlaceService dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            switch (DataPersistenceQueryContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.ServiceConcept = retVal.ServiceConcept.GetRelatedPersistenceService().Get(context, dbModel.ServiceConceptKey);
                    retVal.SetLoaded(o => o.ServiceConcept);
                    break;
            }

            return retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbPlaceService, PlaceService>(dbModel));
        }
    }
}
