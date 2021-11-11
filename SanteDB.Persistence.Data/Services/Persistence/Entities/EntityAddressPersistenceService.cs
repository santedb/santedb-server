using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// A persistence service which operates on <see cref="EntityAddress"/>
    /// </summary>
    public class EntityAddressPersistenceService : EntityAssociationPersistenceService<EntityAddress, DbEntityAddress>
    {
        /// <summary>
        /// Dependency injection ctor
        /// </summary>
        public EntityAddressPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare referenced objects
        /// </summary>
        protected override EntityAddress PrepareReferences(DataContext context, EntityAddress data)
        {
            data.AddressUseKey = this.EnsureExists(context, data.AddressUse)?.Key ?? data.AddressUseKey;
            return base.PrepareReferences(context, data);
        }

        /// <summary>
        /// Perform an insert with the nested components
        /// </summary>
        protected override EntityAddress DoInsertModel(DataContext context, EntityAddress data)
        {
            var retVal = base.DoInsertModel(context, data);

            if (data.Component != null)
            {
                retVal.Component = this.UpdateModelAssociations(context, retVal, data.Component).ToList();
            }

            return retVal;
        }

        /// <summary>
        /// Update model
        /// </summary>
        protected override EntityAddress DoUpdateModel(DataContext context, EntityAddress data)
        {
            var retVal = base.DoUpdateModel(context, data);

            if (data.Component != null)
            {
                retVal.Component = this.UpdateModelAssociations(context, retVal, data.Component).ToList();
            }

            return retVal;
        }

        /// <summary>
        /// Convert back to information model
        /// </summary>
        protected override EntityAddress DoConvertToInformationModel(DataContext context, DbEntityAddress dbModel, params IDbIdentified[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            switch (this.m_configuration.LoadStrategy)
            {
                case Configuration.LoadStrategyType.FullLoad:
                    retVal.AddressUse = this.GetRelatedPersistenceService<Concept>().Get(context, dbModel.UseConceptKey);
                    retVal.SetLoaded(nameof(EntityAddress.AddressUse));
                    goto case Configuration.LoadStrategyType.SyncLoad;
                case Configuration.LoadStrategyType.SyncLoad:
                    retVal.Component = this.GetRelatedPersistenceService<EntityAddressComponent>().Query(context, o => o.SourceEntityKey == dbModel.Key).OrderBy(o => o.OrderSequence).ToList();
                    retVal.SetLoaded(nameof(EntityAddress.Component));
                    break;
                case Configuration.LoadStrategyType.QuickLoad:
                    retVal.Component = null;
                    break;
            }
            return retVal;
        }
    }
}