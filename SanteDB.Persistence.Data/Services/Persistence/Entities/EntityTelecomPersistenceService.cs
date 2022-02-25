using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// A persistence service that operates on telecoms
    /// </summary>
    public class EntityTelecomPersistenceService : EntityAssociationPersistenceService<EntityTelecomAddress, DbTelecomAddress>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public EntityTelecomPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare references for the insertion/update
        /// </summary>
        protected override EntityTelecomAddress BeforePersisting(DataContext context, EntityTelecomAddress data)
        {
            data.AddressUseKey = this.EnsureExists(context, data.AddressUse)?.Key ?? data.AddressUseKey;
            data.TypeConceptKey = this.EnsureExists(context, data.TypeConcept)?.Key ?? data.TypeConceptKey;

            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Convert the telecom address
        /// </summary>
        protected override EntityTelecomAddress DoConvertToInformationModel(DataContext context, DbTelecomAddress dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.AddressUse = retVal.AddressUse.GetRelatedPersistenceService().Get(context, dbModel.TelecomUseKey);
                    retVal.SetLoaded(nameof(EntityTelecomAddress.AddressUse));
                    retVal.TypeConcept = retVal.TypeConcept.GetRelatedPersistenceService().Get(context, dbModel.TypeConceptKey.GetValueOrDefault());
                    retVal.SetLoaded(nameof(EntityTelecomAddress.TypeConcept));
                    break;
            }

            return retVal;
        }
    }
}