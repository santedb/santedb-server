using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Concepts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// Persistence service for reference terms.
    /// </summary>
    public class ReferenceTermPersistenceService : NonVersionedDataPersistenceService<ReferenceTerm, DbReferenceTerm>
    {
        /// <summary>
        /// Reference term persistence service
        /// </summary>
        public ReferenceTermPersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare references for persistence
        /// </summary>
        protected override ReferenceTerm PrepareReferences(DataContext context, ReferenceTerm data)
        {
            data.CodeSystemKey = data.CodeSystem?.EnsureExists(context)?.Key ?? data.CodeSystemKey;
            return base.PrepareReferences(context, data);
        }

        /// <summary>
        /// Convert the refeence term to reference model
        /// </summary>
        protected override ReferenceTerm DoConvertToInformationModel(DataContext context, DbReferenceTerm dbModel, params IDbIdentified[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch(this.m_configuration.LoadStrategy)
            {
                case Configuration.LoadStrategyType.FullLoad:
                    retVal.CodeSystem = base.GetRelatedPersistenceService<CodeSystem>().Get(context, dbModel.CodeSystemKey, null);
                    retVal.SetLoadIndicator(nameof(ReferenceTerm.CodeSystem));
                    goto case Configuration.LoadStrategyType.SyncLoad;
                case Configuration.LoadStrategyType.SyncLoad:
                    retVal.DisplayNames = base.GetRelatedPersistenceService<ReferenceTermName>().Query(context, o => o.SourceEntityKey == dbModel.Key).ToList();
                    retVal.SetLoadIndicator(nameof(ReferenceTerm.DisplayNames));
                    break;
            }

            return retVal;
        }

        /// <summary>
        /// Perform an insertion of the obect and dependent properties
        /// </summary>
        protected override ReferenceTerm DoInsertModel(DataContext context, ReferenceTerm data)
        {
            var retVal = base.DoInsertModel(context, data);

            if(data.DisplayNames != null)
            {
                retVal.DisplayNames = base.UpdateModelAssociations(context, retVal, data.DisplayNames).ToList();
            }

            return retVal;
        }

        /// <summary>
        /// Perform an insertion of the obect and dependent properties
        /// </summary>
        protected override ReferenceTerm DoUpdateModel(DataContext context, ReferenceTerm data)
        {
            var retVal = base.DoInsertModel(context, data);

            if (data.DisplayNames != null)
            {
                retVal.DisplayNames = base.UpdateModelAssociations(context, retVal, data.DisplayNames).ToList();
            }

            return retVal;
        }
    }
}
