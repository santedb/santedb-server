using SanteDB.Core.BusinessRules;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Extensions;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.DataType;
using SanteDB.Persistence.Data.Model.Entities;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// Persistence service that is responsible for storing and retrieving entities
    /// </summary>
    public class EntityPersistenceService : VersionedDataPersistenceService<Entity, DbEntityVersion, DbEntity>
    {
        /// <summary>
        /// Creates a dependency injected
        /// </summary>
        public EntityPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare references
        /// </summary>
        protected override Entity PrepareReferences(DataContext context, Entity data)
        {
            data.ClassConceptKey = this.EnsureExists(context, data.ClassConcept)?.Key ?? data.ClassConceptKey;
            data.CreationActKey = this.EnsureExists(context, data.CreationAct)?.Key ?? data.CreationActKey;
            data.DeterminerConceptKey = this.EnsureExists(context, data.DeterminerConcept)?.Key ?? data.DeterminerConceptKey;
            data.StatusConceptKey = this.EnsureExists(context, data.StatusConcept)?.Key ?? data.StatusConceptKey;
            data.TemplateKey = this.EnsureExists(context, data.Template)?.Key ?? data.TemplateKey;
            data.TypeConceptKey = this.EnsureExists(context, data.TypeConcept)?.Key ?? data.TypeConceptKey;

            // Prepare any detected issues
            var issues = this.VerifyEntity(context, data).ToArray();
            if (issues.Any(i => i.Priority == DetectedIssuePriorityType.Error))
            {
                throw new DetectedIssueException(issues);
            }
            else if (issues.Any()) // There are issues
            {
                var extension = data.Extensions.FirstOrDefault(o => o.ExtensionTypeKey == ExtensionTypeKeys.DataQualityExtension);
                if (extension == null)
                {
                    data.Extensions.Add(new EntityExtension(ExtensionTypeKeys.DataQualityExtension, typeof(DictionaryExtensionHandler), issues));
                }
                else
                {
                    var existingValues = extension.GetValue<List<DetectedIssue>>();
                    existingValues.AddRange(issues);
                    extension.ExtensionValue = existingValues;
                }
            }

            return base.PrepareReferences(context, data);
        }

        /// <summary>
        /// Convert the data model back to information model
        /// </summary>
        protected override Entity DoConvertToInformationModel(DataContext context, DbEntityVersion dbModel, params IDbIdentified[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch (this.m_configuration.LoadStrategy)
            {
                case Configuration.LoadStrategyType.FullLoad:
                    retVal.ClassConcept = this.GetRelatedPersistenceService<Concept>().Get(context, dbModel.ClassConceptKey, null);
                    retVal.CreationAct = this.GetRelatedPersistenceService<Act>().Get(context, dbModel.CreationActKey.GetValueOrDefault(), null);
                    retVal.DeterminerConcept = this.GetRelatedPersistenceService<Concept>().Get(context, dbModel.DeterminerConceptKey, null);
                    retVal.StatusConcept = this.GetRelatedPersistenceService<Concept>().Get(context, dbModel.StatusConceptKey, null);
                    retVal.TypeConcept = this.GetRelatedPersistenceService<Concept>().Get(context, dbModel.TypeConceptKey.GetValueOrDefault(), null);
                    goto case Configuration.LoadStrategyType.SyncLoad;
                case Configuration.LoadStrategyType.SyncLoad:
                    retVal.Addresses = this.GetRelatedPersistenceService<EntityAddress>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.Extensions = this.GetRelatedPersistenceService<EntityExtension>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.Identifiers = this.GetRelatedPersistenceService<EntityIdentifier>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.Names = this.GetRelatedPersistenceService<EntityName>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.Notes = this.GetRelatedPersistenceService<EntityNote>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.Relationships = this.GetRelatedPersistenceService<EntityRelationship>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.Tags = this.GetRelatedPersistenceService<EntityTag>().Query(context, o => o.SourceEntityKey == dbModel.Key).ToList();
                    retVal.Telecoms = this.GetRelatedPersistenceService<EntityTelecomAddress>().Query(context, o => o.SourceEntityKey == dbModel.Key).ToList();
                    goto case Configuration.LoadStrategyType.QuickLoad;
                case Configuration.LoadStrategyType.QuickLoad:
                    var query = context.CreateSqlStatement<DbEntitySecurityPolicy>().SelectFrom(typeof(DbEntitySecurityPolicy), typeof(DbSecurityPolicy))
                        .InnerJoin<DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
                        .Where(o => o.SourceKey == dbModel.Key);
                    retVal.Policies = context.Query<CompositeResult<DbEntitySecurityPolicy, DbSecurityPolicy>>(query)
                        .ToList()
                        .Select(o => new SecurityPolicyInstance(new SecurityPolicy(o.Object2.Name, o.Object2.Oid, o.Object2.IsPublic, o.Object2.CanOverride), PolicyGrantType.Grant))
                        .ToList();
                    break;
            }

            return retVal;
        }

        // TODO: Implement Insert with relations
        // TODO: Implement Update with relations
        // TODO: Implement Obsolete with relations
    }
}