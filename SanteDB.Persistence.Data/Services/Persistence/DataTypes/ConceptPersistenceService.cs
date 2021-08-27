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
        /// Prepare references 
        /// </summary>
        protected override Concept PrepareReferences(DataContext context, Concept data)
        {
            data.ClassKey = data.Class?.EnsureExists(context)?.Key ?? data.ClassKey;
            data.StatusConceptKey = data.StatusConcept?.EnsureExists(context)?.Key ?? data.StatusConceptKey;
            return data;
        }

        /// <summary>
        /// Perform an insert of the model properties
        /// </summary>
        protected override Concept DoInsertModel(DataContext context, Concept data)
        {
            // Do Insertion of themodel
            var retVal = base.DoInsertModel(context, data);

            // Insert names
            if (data.ConceptNames != null)
            {
                retVal.ConceptNames = base.UpdateModelVersionedAssociations<ConceptName>(context, retVal, data.ConceptNames).ToList();
            }

            // Concept sets
            if (data.ConceptSetsXml != null)
            {
                retVal.ConceptSetsXml = base.UpdateInternalAssociations<DbConceptSetConceptAssociation>(context, retVal.Key.Value,
                    data.ConceptSetsXml.Select(o => new DbConceptSetConceptAssociation()
                    {
                        ConceptKey = retVal.Key.Value,
                        SourceKey = o
                    })).Select(o => o.SourceKey).ToList();
            }

            // Reference terms
            if (data.ReferenceTerms != null)
            {
                retVal.ReferenceTerms = base.UpdateModelVersionedAssociations<ConceptReferenceTerm>(context, retVal, data.ReferenceTerms).ToList();
            }

            // Relationships
            if (data.Relationship != null)
            {
                retVal.Relationship = base.UpdateModelVersionedAssociations<ConceptRelationship>(context, retVal, data.Relationship).ToList();
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

            // Relationships
            if (data.Relationship != null)
            {
                retVal.Relationship = base.UpdateModelVersionedAssociations<ConceptRelationship>(context, retVal, data.Relationship).ToList();
            }

            return retVal;
        }


        /// <summary>
        /// Convert the <paramref name="dbModel"/> to <typeparamref name="TModel"/>
        /// </summary>
        protected override Concept DoConvertToInformationModel(DataContext context, DbConceptVersion dbModel, params IDbIdentified[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch (this.m_configuration.LoadStrategy)
            {
                case Configuration.LoadStrategyType.FullLoad:
                    retVal.Class = base.GetRelatedPersistenceService<ConceptClass>().Get(context, dbModel.ClassKey, null);
                    goto case Configuration.LoadStrategyType.SyncLoad; // special case - FullLoad implies SyncLoad so we want a fallthrough - the only way to do this in C# is with this messy GOTO stuff
                case Configuration.LoadStrategyType.SyncLoad:
                    retVal.ConceptNames = base.GetRelatedPersistenceService<ConceptName>().Query(context, o => o.SourceEntityKey == dbModel.Key).ToList();
                    retVal.Relationship = base.GetRelatedPersistenceService<ConceptRelationship>().Query(context, o => o.SourceEntityKey == dbModel.Key).ToList();
                    retVal.ReferenceTerms = this.GetRelatedPersistenceService<ConceptReferenceTerm>().Query(context, o => o.SourceEntityKey == dbModel.Key).ToList();
                    goto case Configuration.LoadStrategyType.QuickLoad;
                case Configuration.LoadStrategyType.QuickLoad:
                    retVal.ConceptSetsXml = context.Query<DbConceptSetConceptAssociation>(o => o.SourceKey == dbModel.Key).Select(o => o.SourceKey).ToList();
                    break;
            }

            return retVal;
        }
    }
}
