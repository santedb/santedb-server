using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Acts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// A <see cref="IDataPersistenceService{TModel}"/> which is responsible for the storage and maintenance of <see cref="Protocol"/> definitions
    /// </summary>
    public class ProtocolPersistenceService : BaseEntityDataPersistenceService<Protocol, DbProtocol>
    {
        /// <inheritdoc/>
        public ProtocolPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override Protocol BeforePersisting(DataContext context, Protocol data)
        {
            data.NarrativeKey = this.EnsureExists(context, data.Narrative)?.Key ?? data.NarrativeKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override Protocol DoConvertToInformationModel(DataContext context, DbProtocol dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    if (dbModel.NarrativeKey.HasValue)
                    {
                        retVal.Narrative = retVal.Narrative.GetRelatedPersistenceService<Narrative>().Get(context, dbModel.NarrativeKey.Value);
                        retVal.SetLoaded(o => o.Narrative);
                    }
                    break;
            }

            // copy data
            retVal.Definition = dbModel.Definition;
            retVal.HandlerClassName = dbModel.HandlerClassName;
            retVal.Name = dbModel.Name;
            retVal.Oid = dbModel.Oid;
            return retVal;
        }
    }
}
