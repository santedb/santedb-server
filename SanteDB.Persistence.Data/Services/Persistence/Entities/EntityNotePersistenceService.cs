﻿using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Extensibility;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// Persistence service for entity notes
    /// </summary>
    public class EntityNotePersistenceService : EntityAssociationPersistenceService<EntityNote, DbEntityNote>
    {
        /// <summary>
        /// Note persistence service DI constructor
        /// </summary>
        public EntityNotePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare referneces on the object
        /// </summary>
        protected override EntityNote BeforePersisting(DataContext context, EntityNote data)
        {
            data.AuthorKey = this.EnsureExists(context, data.Author)?.Key ?? data.AuthorKey;
            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Perform conversion to information model
        /// </summary>
        protected override EntityNote DoConvertToInformationModel(DataContext context, DbEntityNote dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.Author = retVal.Author.GetRelatedPersistenceService().Get(context, dbModel.AuthorKey);
                    retVal.SetLoaded(nameof(EntityNote.Author));
                    break;
            }
            return retVal;
        }
    }
}