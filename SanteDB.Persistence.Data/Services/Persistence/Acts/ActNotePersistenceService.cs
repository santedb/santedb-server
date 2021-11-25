using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Extensibility;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// Persistence service for act notes
    /// </summary>
    public class ActNotePersistenceService : ActAssociationPersistenceService<ActNote, DbActNote>
    {
        /// <summary>
        /// Note persistence service DI constructor
        /// </summary>
        public ActNotePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare referneces on the object
        /// </summary>
        protected override ActNote BeforePersisting(DataContext context, ActNote data)
        {
            data.AuthorKey = this.EnsureExists(context, data.Author)?.Key ?? data.AuthorKey;
            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Perform conversion to information model
        /// </summary>
        protected override ActNote DoConvertToInformationModel(DataContext context, DbActNote dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            if (this.m_configuration.LoadStrategy == Configuration.LoadStrategyType.FullLoad)
            {
                retVal.Author = base.GetRelatedPersistenceService<Entity>().Get(context, dbModel.AuthorKey);
                retVal.SetLoaded(nameof(ActNote.Author));
            }
            return retVal;
        }
    }
}