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
    /// ConceptSet persistence services for ADO
    /// </summary>
    public class ConceptSetPersistenceService : NonVersionedDataPersistenceService<ConceptSet, DbConceptSet>
    {
        /// <summary>
        /// Creates a new instance of the concept set
        /// </summary>
        public ConceptSetPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Perform the conversion of this concept set to a relationship model
        /// </summary>
        protected override ConceptSet DoConvertToInformationModel(DataContext context, DbConceptSet dbModel, params IDbIdentified[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            retVal.ConceptKeys = context.Query<DbConceptSetConceptAssociation>(o => o.SourceKey == dbModel.Key).Select(o => o.ConceptKey).ToList();
            return retVal;
        }
    }
}