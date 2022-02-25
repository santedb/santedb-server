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
    public class EntityNamePersistenceService : EntityAssociationPersistenceService<EntityName, DbEntityName>
    {
        /// <summary>
        /// Dependency injection ctor
        /// </summary>
        public EntityNamePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare referenced objects
        /// </summary>
        protected override EntityName BeforePersisting(DataContext context, EntityName data)
        {
            data.NameUseKey = this.EnsureExists(context, data.NameUse)?.Key ?? data.NameUseKey;
            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Perform an insert with the nested components
        /// </summary>
        protected override EntityName DoInsertModel(DataContext context, EntityName data)
        {
            var retVal = base.DoInsertModel(context, data);

            if (data.Component != null)
            {
                retVal.Component = this.UpdateModelAssociations(context, retVal, data.Component).ToList();
                retVal.SetLoaded(o => o.Component);
            }

            return retVal;
        }

        /// <summary>
        /// Update model
        /// </summary>
        protected override EntityName DoUpdateModel(DataContext context, EntityName data)
        {
            var retVal = base.DoUpdateModel(context, data);

            if (data.Component != null)
            {
                retVal.Component = this.UpdateModelAssociations(context, retVal, data.Component).ToList();
                retVal.SetLoaded(o => o.Component);
            }

            return retVal;
        }

        /// <summary>
        /// Convert back to information model
        /// </summary>
        protected override EntityName DoConvertToInformationModel(DataContext context, DbEntityName dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.NameUse = retVal.NameUse.GetRelatedPersistenceService().Get(context, dbModel.UseConceptKey);
                    retVal.SetLoaded(nameof(EntityName.NameUse));
                    goto case LoadMode.SyncLoad;
                case LoadMode.SyncLoad:
                    retVal.Component = retVal.Component.GetRelatedPersistenceService().Query(context, o => o.SourceEntityKey == dbModel.Key).OrderBy(o => o.OrderSequence).ToList();
                    retVal.SetLoaded(nameof(EntityName.Component));
                    break;
            }
            return retVal;
        }
    }
}