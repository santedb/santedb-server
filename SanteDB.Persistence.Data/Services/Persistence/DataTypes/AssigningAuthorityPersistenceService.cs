using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.DataType;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// Assigning authority persistence
    /// </summary>
    public class AssigningAuthorityPersistenceService : BaseEntityDataPersistenceService<AssigningAuthority, DbAssigningAuthority>
    {
        /// <inheritdoc/>
        public AssigningAuthorityPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override AssigningAuthority BeforePersisting(DataContext context, AssigningAuthority data)
        {
            data.SourceEntityKey = this.EnsureExists(context, data.SourceEntity)?.Key ?? data.SourceEntityKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override AssigningAuthority DoConvertToInformationModel(DataContext context, DbAssigningAuthority dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            if((DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy) == LoadMode.FullLoad)
            {
                retVal.AssigningApplication = retVal.AssigningApplication.GetRelatedPersistenceService().Get(context, dbModel.AssigningApplicationKey);
                retVal.SetLoaded(o => o.AssigningApplication);
            }

            return retVal;
        }
    }
}
