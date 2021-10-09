using SanteDB.Core.BusinessRules;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Extensions;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
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
        /// <param name="configurationManager"></param>
        /// <param name="adhocCacheService"></param>
        /// <param name="dataCachingService"></param>
        /// <param name="queryPersistence"></param>
        public EntityPersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Verify identities on the specified entity
        /// </summary>
        private IEnumerable<DetectedIssue> VerifyEntity(DataContext context, Entity entityToVerify)
        {
            // Validate unique values for IDs
            var priority = this.m_configuration.Validation.ValidationLevel;

            foreach (var id in entityToVerify.Identifiers)
            {
                // Get ID
                DbAssigningAuthority dbAuth = null;

                if (id.AuthorityKey.HasValue) // Attempt lookup in adhoc cache then by db
                {
                    dbAuth = this.m_adhocCache?.Get<DbAssigningAuthority>($"{DataConstants.AdhocAuthorityKey}{id.AuthorityKey}");
                    if (dbAuth == null)
                        dbAuth = context.FirstOrDefault<DbAssigningAuthority>(o => o.Key == id.AuthorityKey);
                }
                else
                {
                    dbAuth = this.m_adhocCache?.Get<DbAssigningAuthority>($"{DataConstants.AdhocAuthorityKey}{id.Authority.DomainName}");
                    if (dbAuth == null)
                    {
                        dbAuth = context.FirstOrDefault<DbAssigningAuthority>(o => o.DomainName == id.Authority.DomainName);
                        if (dbAuth != null)
                            id.AuthorityKey = dbAuth.Key;
                    }
                }

                if (dbAuth == null)
                {
                    yield return new DetectedIssue(priority, "id.aa.notFound", $"Missing assigning authority with ID {String.Join(",", entityToVerify.Identifiers.Select(o => o.AuthorityKey))}", DetectedIssueKeys.SafetyConcernIssue);
                    continue;
                }
                else
                {
                    this.m_adhocCache?.Add($"{DataConstants.AdhocAuthorityKey}{id.AuthorityKey}", dbAuth, new TimeSpan(0, 5, 0));
                    this.m_adhocCache?.Add($"{DataConstants.AdhocAuthorityKey}{dbAuth.DomainName}", dbAuth, new TimeSpan(0, 5, 0));
                }

                // Get this identifier records which is not owned by my record
                var ownedByOthers = context.Query<DbEntityIdentifier>(
                    context.CreateSqlStatement()
                    .SelectFrom(typeof(DbEntityIdentifier))
                    .Where<DbEntityIdentifier>(o => o.Value == id.Value && o.AuthorityKey == id.AuthorityKey && o.ObsoleteVersionSequenceId == null && o.SourceKey != entityToVerify.Key)
                    .And("NOT EXISTS (SELECT 1 FROM ent_rel_tbl WHERE (src_ent_id = ? AND trg_ent_id = ent_id_tbl.ent_id OR trg_ent_id = ? AND src_ent_id = ent_id_tbl.ent_id) AND obslt_vrsn_seq_id IS NULL)", entityToVerify.Key, entityToVerify.Key)
                ).Any();
                var ownedByMe = context.Query<DbEntityIdentifier>(
                    context.CreateSqlStatement()
                    .SelectFrom(typeof(DbEntityIdentifier))
                    .Where<DbEntityIdentifier>(o => o.Value == id.Value && o.AuthorityKey == id.AuthorityKey && o.ObsoleteVersionSequenceId == null)
                    .And("(ent_id = ? OR EXISTS (SELECT 1 FROM ent_rel_tbl WHERE (src_ent_id = ?  AND trg_ent_id = ent_id_tbl.ent_id) OR (trg_ent_id = ? AND src_ent_id = ent_id_tbl.ent_id) AND obslt_vrsn_seq_id IS NULL))", entityToVerify.Key, entityToVerify.Key, entityToVerify.Key)
                ).Any();

                // Verify scope
                IEnumerable<DbAuthorityScope> scopes = this.m_adhocCache?.Get<DbAuthorityScope[]>($"ado.aa.scp.{dbAuth.Key}");
                if (scopes == null)
                {
                    scopes = context.Query<DbAuthorityScope>(o => o.SourceKey == dbAuth.Key);
                    this.m_adhocCache?.Add($"{DataConstants.AdhocAuthorityScopeKey}{dbAuth.Key}", scopes.ToArray());
                }

                if (scopes.Any() && !scopes.Any(s => s.ScopeConceptKey == entityToVerify.ClassConceptKey) // This type of identifier is not allowed to be assigned to this type of object
                    && !ownedByOthers
                    && !ownedByMe) // Unless it was already associated to another type of object related to me
                    yield return new DetectedIssue(DetectedIssuePriorityType.Error, "id.target", $"Identifier of type {dbAuth.DomainName} cannot be assigned to object of type {entityToVerify.ClassConceptKey}", DetectedIssueKeys.BusinessRuleViolationIssue);

                // If the identity domain is unique, and we've been asked to raid identifier uq issues
                if (dbAuth.IsUnique && this.m_configuration.Validation.IdentifierUniqueness &&
                    ownedByOthers)
                    yield return new DetectedIssue(priority, $"id.uniqueness", $"Identifier {id.Value} in domain {dbAuth.DomainName} violates unique constraint", DetectedIssueKeys.FormalConstraintIssue);
                if (dbAuth.AssigningApplicationKey.HasValue) // Must have permission
                {
                    if (context.GetProvenance().ApplicationKey != dbAuth.AssigningApplicationKey  // Established prov key
                        && entityToVerify.CreatedByKey != dbAuth.AssigningApplicationKey  // original prov key
                        && !ownedByMe
                        && !ownedByOthers) // and has not already been assigned to me or anyone else (it is a new , unknown identifier)
                        yield return new DetectedIssue(DetectedIssuePriorityType.Error, $"id.authority", $"Application does not have permission to assign {dbAuth.DomainName}", DetectedIssueKeys.SecurityIssue);
                }
                if (!String.IsNullOrEmpty(dbAuth.ValidationRegex) && this.m_configuration.Validation.IdentifierFormat) // must be valid
                {
                    var nonMatch = !new Regex(dbAuth.ValidationRegex).IsMatch(id.Value);
                    if (nonMatch)
                        yield return new DetectedIssue(priority, $"id.format", $"Identifier {id.Value} in domain {dbAuth.DomainName} failed format validation", DetectedIssueKeys.FormalConstraintIssue);
                }
            }
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