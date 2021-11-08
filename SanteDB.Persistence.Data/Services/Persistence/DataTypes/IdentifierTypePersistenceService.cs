using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.DataType;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// Identifier type persistence service
    /// </summary>
    public class IdentifierTypePersistenceService : BaseEntityDataPersistenceService<IdentifierType, DbIdentifierType>
    {
        /// <summary>
        /// Creates a DI instance of the identifier type
        /// </summary>
        public IdentifierTypePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare all references
        /// </summary>
        protected override IdentifierType PrepareReferences(DataContext context, IdentifierType data)
        {
            data.ScopeConceptKey = this.EnsureExists(context, data.ScopeConcept)?.Key ?? data.ScopeConceptKey;
            data.TypeConceptKey = this.EnsureExists(context, data.TypeConcept)?.Key ?? data.TypeConceptKey;
            return base.PrepareReferences(context, data);
        }

        /// <summary>
        /// Convert the database model to an information model object
        /// </summary>
        protected override IdentifierType DoConvertToInformationModel(DataContext context, DbIdentifierType dbModel, params IDbIdentified[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            if (this.m_configuration.LoadStrategy == Configuration.LoadStrategyType.FullLoad)
            {
                retVal.TypeConcept = base.GetRelatedPersistenceService<Concept>().Get(context, dbModel.TypeConceptKey, null);
                retVal.ScopeConcept = base.GetRelatedPersistenceService<Concept>().Get(context, dbModel.ScopeConceptKey.GetValueOrDefault(), null);
            }
            return retVal;
        }
    }
}