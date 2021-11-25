using SanteDB.Core.BusinessRules;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Extensions;
using SanteDB.Core.i18n;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model;
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
    public class EntityDerivedPersistenceService<TEntity> : VersionedDataPersistenceService<TEntity, DbEntityVersion, DbEntity>
        where TEntity : Entity, IVersionedEntity, new()
    {
        /// <summary>
        /// Creates a dependency injected
        /// </summary>
        public EntityDerivedPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare references
        /// </summary>
        protected override TEntity BeforePersisting(DataContext context, TEntity data)
        {
            if (!data.StatusConceptKey.HasValue)
            {
                data.StatusConceptKey = StatusKeys.New;
            }

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

            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Convert the data model back to information model
        /// </summary>
        protected override TEntity DoConvertToInformationModel(DataContext context, DbEntityVersion dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch (this.m_configuration.LoadStrategy)
            {
                case Configuration.LoadStrategyType.FullLoad:
                    retVal.ClassConcept = this.GetRelatedPersistenceService<Concept>().Get(context, dbModel.ClassConceptKey);
                    retVal.SetLoaded(nameof(Entity.ClassConcept));
                    retVal.CreationAct = this.GetRelatedPersistenceService<Act>().Get(context, dbModel.CreationActKey.GetValueOrDefault());
                    retVal.SetLoaded(nameof(Entity.CreationAct));
                    retVal.DeterminerConcept = this.GetRelatedPersistenceService<Concept>().Get(context, dbModel.DeterminerConceptKey);
                    retVal.SetLoaded(nameof(Entity.DeterminerConcept));
                    retVal.StatusConcept = this.GetRelatedPersistenceService<Concept>().Get(context, dbModel.StatusConceptKey);
                    retVal.SetLoaded(nameof(Entity.StatusConcept));
                    retVal.TypeConcept = this.GetRelatedPersistenceService<Concept>().Get(context, dbModel.TypeConceptKey.GetValueOrDefault());
                    retVal.SetLoaded(nameof(Entity.TypeConcept));
                    goto case Configuration.LoadStrategyType.SyncLoad;
                case Configuration.LoadStrategyType.SyncLoad:
                    retVal.Addresses = this.GetRelatedPersistenceService<EntityAddress>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(nameof(Entity.Addresses));
                    retVal.Extensions = this.GetRelatedPersistenceService<EntityExtension>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(nameof(Entity.Extensions));
                    retVal.Identifiers = this.GetRelatedPersistenceService<EntityIdentifier>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(nameof(Entity.Identifiers));
                    retVal.Names = this.GetRelatedPersistenceService<EntityName>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(nameof(Entity.Names));
                    retVal.Notes = this.GetRelatedPersistenceService<EntityNote>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(nameof(Entity.Notes));
                    retVal.Relationships = this.GetRelatedPersistenceService<EntityRelationship>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(nameof(Entity.Relationships));
                    retVal.Tags = this.GetRelatedPersistenceService<EntityTag>().Query(context, o => o.SourceEntityKey == dbModel.Key).ToList();
                    retVal.SetLoaded(nameof(Entity.Tags));
                    retVal.Telecoms = this.GetRelatedPersistenceService<EntityTelecomAddress>().Query(context, o => o.SourceEntityKey == dbModel.Key).ToList();
                    retVal.SetLoaded(nameof(Entity.Telecoms));
                    goto case Configuration.LoadStrategyType.QuickLoad;
                case Configuration.LoadStrategyType.QuickLoad:
                    var query = context.CreateSqlStatement<DbEntitySecurityPolicy>().SelectFrom(typeof(DbEntitySecurityPolicy), typeof(DbSecurityPolicy))
                        .InnerJoin<DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
                        .Where(o => o.SourceKey == dbModel.Key);
                    retVal.Policies = context.Query<CompositeResult<DbEntitySecurityPolicy, DbSecurityPolicy>>(query)
                        .ToList()
                        .Select(o => new SecurityPolicyInstance(new SecurityPolicy(o.Object2.Name, o.Object2.Oid, o.Object2.IsPublic, o.Object2.CanOverride), PolicyGrantType.Grant))
                        .ToList();
                    retVal.SetLoaded(nameof(Entity.Policies));
                    break;
            }

            return retVal;
        }

        /// <summary>
        /// Insert the model object (in this case an entity)
        /// </summary>
        /// <param name="context">The data context on which the data is to be inserted</param>
        /// <param name="data">The data which is to be inserted</param>
        /// <returns>The inserted entity</returns>
        protected override TEntity DoInsertModel(DataContext context, TEntity data)
        {
            var retVal = base.DoInsertModel(context, data);

            if (data.Addresses != null)
            {
                retVal.Addresses = this.UpdateModelVersionedAssociations(context, retVal, data.Addresses).ToList();
            }

            if (data.Extensions != null)
            {
                retVal.Extensions = this.UpdateModelVersionedAssociations(context, retVal, data.Extensions).ToList();
            }

            if (data.Identifiers != null)
            {
                retVal.Identifiers = this.UpdateModelVersionedAssociations(context, retVal, data.Identifiers).ToList();
            }

            if (data.Names != null)
            {
                retVal.Names = this.UpdateModelVersionedAssociations(context, retVal, data.Names).ToList();
            }

            if (data.Notes != null)
            {
                retVal.Notes = this.UpdateModelVersionedAssociations(context, retVal, data.Notes).ToList();
            }

            if (data.Policies != null)
            {
                retVal.Policies = this.UpdateInternalAssociations(context, retVal.Key.Value, data.Policies.Select(o => new DbEntitySecurityPolicy()
                {
                    PolicyKey = o.PolicyKey.Value
                }), o => o.SourceKey == retVal.Key && !o.ObsoleteVersionSequenceId.HasValue).Select(o => o.ToSecurityPolicyInstance(context)).ToList();
            }

            if (data.Relationships != null)
            {
                retVal.Relationships = this.UpdateModelVersionedAssociations(context, retVal, data.Relationships).ToList();
            }

            if (data.Tags != null)
            {
                retVal.Tags = this.UpdateModelAssociations(context, retVal, data.Tags).ToList();
            }

            if (data.Telecoms != null)
            {
                retVal.Telecoms = this.UpdateModelVersionedAssociations(context, retVal, data.Telecoms).ToList();
            }

            return retVal;
        }

        /// <summary>
        /// Perform an update on the model
        /// </summary>
        protected override TEntity DoUpdateModel(DataContext context, TEntity data)
        {
            var retVal = base.DoUpdateModel(context, data);

            if (data.Addresses != null)
            {
                retVal.Addresses = this.UpdateModelVersionedAssociations(context, retVal, data.Addresses).ToList();
            }

            if (data.Extensions != null)
            {
                retVal.Extensions = this.UpdateModelVersionedAssociations(context, retVal, data.Extensions).ToList();
            }

            if (data.Identifiers != null)
            {
                retVal.Identifiers = this.UpdateModelVersionedAssociations(context, retVal, data.Identifiers).ToList();
            }

            if (data.Names != null)
            {
                retVal.Names = this.UpdateModelVersionedAssociations(context, retVal, data.Names).ToList();
            }

            if (data.Notes != null)
            {
                retVal.Notes = this.UpdateModelVersionedAssociations(context, retVal, data.Notes).ToList();
            }

            if (data.Policies != null)
            {
                retVal.Policies = this.UpdateInternalAssociations(context, retVal.Key.Value, data.Policies.Select(o => new DbEntitySecurityPolicy()
                {
                    PolicyKey = o.PolicyKey.Value
                }), o => o.SourceKey == retVal.Key && !o.ObsoleteVersionSequenceId.HasValue).Select(o => o.ToSecurityPolicyInstance(context)).ToList();
            }

            if (data.Relationships != null)
            {
                retVal.Relationships = this.UpdateModelVersionedAssociations(context, retVal, data.Relationships).ToList();
            }

            if (data.Tags != null)
            {
                retVal.Tags = this.UpdateModelAssociations(context, retVal, data.Tags).ToList();
            }

            if (data.Telecoms != null)
            {
                retVal.Telecoms = this.UpdateModelVersionedAssociations(context, retVal, data.Telecoms).ToList();
            }

            return retVal;
        }
    }
}