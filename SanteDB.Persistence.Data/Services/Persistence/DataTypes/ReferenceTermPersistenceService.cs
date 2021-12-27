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
        public ReferenceTermPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Delete references
        /// </summary>
        protected override void DoDeleteReferencesInternal(DataContext context, Guid key)
        {
            context.Delete<DbReferenceTermName>(o => o.SourceKey == key);
            context.Delete<DbConceptReferenceTerm>(o => o.TargetKey == key);

            base.DoDeleteReferencesInternal(context, key);
        }

        /// <summary>
        /// Prepare references for persistence
        /// </summary>
        protected override ReferenceTerm BeforePersisting(DataContext context, ReferenceTerm data)
        {
            data.CodeSystemKey = this.EnsureExists(context, data.CodeSystem)?.Key ?? data.CodeSystemKey;
            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Convert the refeence term to reference model
        /// </summary>
        protected override ReferenceTerm DoConvertToInformationModel(DataContext context, DbReferenceTerm dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch (DataPersistenceQueryContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.CodeSystem = base.GetRelatedPersistenceService<CodeSystem>().Get(context, dbModel.CodeSystemKey);
                    retVal.SetLoaded(nameof(ReferenceTerm.CodeSystem));
                    goto case LoadMode.SyncLoad;
                case LoadMode.SyncLoad:
                    retVal.DisplayNames = base.GetRelatedPersistenceService<ReferenceTermName>().Query(context, o => o.SourceEntityKey == dbModel.Key).ToList();
                    retVal.SetLoaded(nameof(ReferenceTerm.DisplayNames));
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

            if (data.DisplayNames != null)
            {
                retVal.DisplayNames = base.UpdateModelAssociations(context, retVal, data.DisplayNames).ToList();
                retVal.SetLoaded(o => o.DisplayNames);

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
                retVal.SetLoaded(o => o.DisplayNames);

            }

            return retVal;
        }
    }
}