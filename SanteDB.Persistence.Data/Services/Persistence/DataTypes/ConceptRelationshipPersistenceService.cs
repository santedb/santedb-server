﻿using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Concepts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// Concept relationship persistence service
    /// </summary>
    public class ConceptRelationshipPersistenceService : ConceptReferencePersistenceBase<ConceptRelationship, DbConceptRelationship>
    {

        /// <summary>
        /// Concept relationship persistence service
        /// </summary>
        public ConceptRelationshipPersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Concept relationship persistence service
        /// </summary>
        protected override ConceptRelationship PrepareReferences(DataContext context, ConceptRelationship data)
        {
            data.RelationshipTypeKey = this.EnsureExists(context, data.RelationshipType)?.Key ?? data.RelationshipTypeKey;
            data.TargetConceptKey = this.EnsureExists(context, data.TargetConcept)?.Key ?? data.TargetConceptKey;
            return data;
        }

        /// <summary>
        /// Information model conversion
        /// </summary>
        protected override ConceptRelationship DoConvertToInformationModel(DataContext context, DbConceptRelationship dbModel, params IDbIdentified[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            switch(this.m_configuration.LoadStrategy)
            {
                case Configuration.LoadStrategyType.FullLoad:
                    retVal.RelationshipType = base.GetRelatedPersistenceService<ConceptRelationshipType>().Get(context, dbModel.RelationshipTypeKey, null);
                    retVal.SetLoadIndicator(nameof(ConceptRelationship.RelationshipType));
                    retVal.TargetConcept = base.GetRelatedPersistenceService<Concept>().Get(context, dbModel.TargetKey, null);
                    retVal.SetLoadIndicator(nameof(ConceptRelationship.TargetConcept));
                    break;
            }
            return retVal;

        }
    }
}