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
    /// Entity address component persistence
    /// </summary>
    public class EntityAddressComponentPersistenceService : IdentifiedDataPersistenceService<EntityAddressComponent, DbEntityAddressComponent>
    {
        /// <summary>
        /// Entity address component DI injection
        /// </summary>
        public EntityAddressComponentPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare references
        /// </summary>
        protected override EntityAddressComponent BeforePersisting(DataContext context, EntityAddressComponent data)
        {
            data.ComponentTypeKey = this.EnsureExists(context, data.ComponentType)?.Key ?? data.ComponentTypeKey;
            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Convert to information model
        /// </summary>
        protected override EntityAddressComponent DoConvertToInformationModel(DataContext context, DbEntityAddressComponent dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            if (this.m_configuration.LoadStrategy == Configuration.LoadStrategyType.FullLoad)
            {
                retVal.ComponentType = this.GetRelatedPersistenceService<Concept>().Get(context, dbModel.ComponentTypeKey.GetValueOrDefault());
                retVal.SetLoaded(nameof(EntityAddressComponent.ComponentType));
            }

            return retVal;
        }
    }
}