﻿using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.DataType;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// Persistence service for act identifiers
    /// </summary>
    public class ActIdentifierPersistenceService : ActAssociationPersistenceService<ActIdentifier, DbActIdentifier>
    {
        /// <summary>
        /// Dependency injection of configuration
        /// </summary>
        public ActIdentifierPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare references
        /// </summary>
        protected override ActIdentifier BeforePersisting(DataContext context, ActIdentifier data)
        {
            data.AuthorityKey = this.EnsureExists(context, data.Authority)?.Key ?? data.AuthorityKey;
            data.IdentifierTypeKey = this.EnsureExists(context, data.IdentifierType)?.Key ?? data.IdentifierTypeKey;
            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Convert from db model to information model
        /// </summary>
        protected override ActIdentifier DoConvertToInformationModel(DataContext context, DbActIdentifier dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            switch (this.m_configuration.LoadStrategy)
            {
                case Configuration.LoadStrategyType.SyncLoad:
                    retVal.Authority = this.GetRelatedPersistenceService<AssigningAuthority>().Get(context, dbModel.AuthorityKey);
                    retVal.SetLoaded(nameof(ActIdentifier.Authority));
                    goto case Configuration.LoadStrategyType.FullLoad;
                case Configuration.LoadStrategyType.FullLoad:
                    retVal.IdentifierType = this.GetRelatedPersistenceService<IdentifierType>().Get(context, dbModel.TypeKey.GetValueOrDefault());
                    retVal.SetLoaded(nameof(ActIdentifier.IdentifierType));
                    break;
            }
            return retVal;
        }
    }
}