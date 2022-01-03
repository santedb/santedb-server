using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Acts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// Act Protocol persistence services
    /// </summary>
    public class ActProtocolPersistenceService : IdentifiedDataPersistenceService<ActProtocol, DbActProtocol>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public ActProtocolPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override ActProtocol BeforePersisting(DataContext context, ActProtocol data)
        {
            data.ProtocolKey = this.EnsureExists(context, data.Protocol)?.Key ?? data.ProtocolKey; 
            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Convert the protocol to data element
        /// </summary>
        protected override ActProtocol DoConvertToInformationModel(DataContext context, DbActProtocol dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            switch(DataPersistenceQueryContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.Protocol = this.GetRelatedPersistenceService<Protocol>().Get(context, dbModel.ProtocolKey);
                    retVal.SetLoaded(o => o.Protocol);
                    break;
            }
            return retVal;
        }
    }
}
