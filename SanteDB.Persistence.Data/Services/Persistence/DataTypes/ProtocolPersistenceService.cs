using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Acts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// Protocol persistence services
    /// </summary>
    public class ProtocolPersistenceService : BaseEntityDataPersistenceService<Protocol, DbProtocol>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
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

            switch(DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.Narrative = retVal.Narrative.GetRelatedPersistenceService().Get(context, dbModel.NarrativeKey.GetValueOrDefault());
                    retVal.SetLoaded(o => o.Narrative);
                    break;
            }

            return retVal;
        }
    }
}
