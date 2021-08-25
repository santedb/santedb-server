using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Concepts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// Persistence service for concepts
    /// </summary>
    public class ConceptPersistenceService : VersionedDataPersistenceService<Concept, DbConceptVersion, DbConcept>
    {

        /// <summary>
        /// Creates a DI instance of hte conept persistence service
        /// </summary>
        public ConceptPersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Perform an insert of the model properties
        /// </summary>
        protected override Concept DoInsertModel(DataContext context, Concept data)
        {
            var retVal = base.DoInsertModel(context, data);

            // Insert names
            if(data.ConceptNames != null)
            {
                retVal.ConceptNames = base.UpdateModelVersionedAssociations<ConceptName>(context, retVal, data.ConceptNames).ToList();
            }

            // Concept sets
            if(data.ConceptSetsXml != null)
            {
                retVal.ConceptSetsXml = base.UpdateInternalAssociations<DbConceptSetConceptAssociation>(context, retVal.Key.Value,
                    data.ConceptSetsXml.Select(o => new DbConceptSetConceptAssociation()
                    {
                        ConceptKey = retVal.Key.Value,
                        SourceKey = o
                    })).Select(o=>o.SourceKey).ToList();
            }

            // Reference terms
            if(data.ReferenceTerms != null)
            {
                retVal.ReferenceTerms = base.UpdateModelVersionedAssociations<ConceptReferenceTerm>(context, retVal, data.ReferenceTerms).ToList();
            }

            return retVal;
        }

        /// <summary>
        /// Perform an update of the model and properties
        /// </summary>
        protected override Concept DoUpdateModel(DataContext context, Concept data)
        {
            var retVal = base.DoUpdateModel(context, data);
            
            // Update names
            if (data.ConceptNames != null)
            {
                retVal.ConceptNames = base.UpdateModelVersionedAssociations<ConceptName>(context, retVal, data.ConceptNames).ToList();
            }

            // Update concept sets
            if (data.ConceptSetsXml != null)
            {
                retVal.ConceptSetsXml = base.UpdateInternalAssociations<DbConceptSetConceptAssociation>(context, retVal.Key.Value,
                    data.ConceptSetsXml.Select(o => new DbConceptSetConceptAssociation()
                    {
                        ConceptKey = retVal.Key.Value,
                        SourceKey = o
                    })).Select(o => o.SourceKey).ToList();
            }

            // Update reference terms
            if (data.ReferenceTerms != null)
            {
                retVal.ReferenceTerms = base.UpdateModelVersionedAssociations<ConceptReferenceTerm>(context, retVal, data.ReferenceTerms).ToList();
            }

            return retVal;
        }
    }
}
