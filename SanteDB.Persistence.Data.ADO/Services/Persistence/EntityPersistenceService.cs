/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using SanteDB.Core;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Extensions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model;
using SanteDB.Persistence.Data.ADO.Data.Model.Acts;
using SanteDB.Persistence.Data.ADO.Data.Model.Concepts;
using SanteDB.Persistence.Data.ADO.Data.Model.DataType;
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;
using SanteDB.Persistence.Data.ADO.Data.Model.Extensibility;
using SanteDB.Persistence.Data.ADO.Data.Model.Roles;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Entity persistence service
    /// </summary>
    public class EntityPersistenceService : VersionedDataPersistenceService<Core.Model.Entities.Entity, DbEntityVersion, DbEntity>
    {

        public EntityPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// To model instance
        /// </summary>
        public virtual TEntityType ToModelInstance<TEntityType>(DbEntityVersion dbVersionInstance, DbEntity entInstance, DataContext context) where TEntityType : Core.Model.Entities.Entity, new()
        {
            var retVal = this.m_settingsProvider.GetMapper().MapDomainInstance<DbEntityVersion, TEntityType>(dbVersionInstance);

            if (retVal == null) return null;

            retVal.ClassConceptKey = entInstance.ClassConceptKey;
            retVal.DeterminerConceptKey = entInstance.DeterminerConceptKey;
            retVal.TemplateKey = entInstance.TemplateKey;

            // Attempt to load policies
            var policySql = context.CreateSqlStatement<DbEntitySecurityPolicy>()
                .SelectFrom(typeof(DbEntitySecurityPolicy), typeof(DbSecurityPolicy))
                .InnerJoin<DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
                .Where(o => o.SourceKey == retVal.Key  && (!o.ObsoleteVersionSequenceId.HasValue || o.ObsoleteVersionSequenceId > retVal.VersionSequence));
            retVal.Policies = context.Query<CompositeResult<DbEntitySecurityPolicy, DbSecurityPolicy>>(policySql).ToArray().Select(o => new SecurityPolicyInstance(new SecurityPolicy()
            {
                CanOverride = o.Object2.CanOverride,
                CreatedByKey = o.Object2.CreatedByKey,
                CreationTime = o.Object2.CreationTime,
                Handler = o.Object2.Handler,
                IsPublic = o.Object2.IsPublic,
                Name = o.Object2.Name,
                LoadState = LoadState.FullLoad,
                Key = o.Object2.Key,
                Oid = o.Object2.Oid
            }, PolicyGrantType.Grant)).ToList();

            // Inversion relationships
            //if (retVal.Relationships != null)
            //{
            //    retVal.Relationships.RemoveAll(o => o.InversionIndicator);
            //    retVal.Relationships.AddRange(context.EntityAssociations.Where(o => o.TargetEntityId == retVal.Key.Value).Distinct().Select(o => new EntityRelationship(o.AssociationTypeConceptId, o.TargetEntityId)
            //    {
            //        SourceEntityKey = o.SourceEntityId,
            //        Key = o.EntityAssociationId,
            //        InversionIndicator = true
            //    }));
            //}
            return retVal;
        }

        /// <summary>
        /// Convert to model instance
        /// </summary>
        public override Core.Model.Entities.Entity ToModelInstance(object dataInstance, DataContext context)
        {

#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif 
            if (dataInstance == null)
                return null;
            // Alright first, which type am I mapping to?
            var dbEntityVersion = (dataInstance as CompositeResult)?.Values.OfType<DbEntityVersion>().FirstOrDefault() ?? dataInstance as DbEntityVersion ?? context.FirstOrDefault<DbEntityVersion>(o => o.VersionKey == (dataInstance as DbEntitySubTable).ParentKey);
            var dbEntity = (dataInstance as CompositeResult)?.Values.OfType<DbEntity>().FirstOrDefault() ?? context.FirstOrDefault<DbEntity>(o => o.Key == dbEntityVersion.Key);
            Entity retVal = null;

            if(dbEntityVersion.StatusConceptKey == StatusKeys.Purged) // Purged data doesn't exist
                retVal = this.ToModelInstance<Entity>(dbEntityVersion, dbEntity, context);
            else switch (dbEntity.ClassConceptKey.ToString().ToLower())
            {
                case EntityClassKeyStrings.Device:
                    retVal = new DeviceEntityPersistenceService(this.m_settingsProvider).ToModelInstance(
                        (dataInstance as CompositeResult)?.Values.OfType<DbDeviceEntity>().FirstOrDefault() ?? context.FirstOrDefault<DbDeviceEntity>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        dbEntityVersion,
                        dbEntity,
                        context);
                    break;
                case EntityClassKeyStrings.NonLivingSubject:
                    retVal = new ApplicationEntityPersistenceService(this.m_settingsProvider).ToModelInstance(
                        (dataInstance as CompositeResult)?.Values.OfType<DbApplicationEntity>().FirstOrDefault() ?? context.FirstOrDefault<DbApplicationEntity>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        dbEntityVersion,
                        dbEntity,
                        context);
                    break;
                case EntityClassKeyStrings.Person:
                    var ue = (dataInstance as CompositeResult)?.Values.OfType<DbUserEntity>().FirstOrDefault() ?? context.FirstOrDefault<DbUserEntity>(o => o.ParentKey == dbEntityVersion.VersionKey);

                    if (ue != null)
                        retVal = new UserEntityPersistenceService(this.m_settingsProvider).ToModelInstance(
                            ue,
                            (dataInstance as CompositeResult)?.Values.OfType<DbPerson>().FirstOrDefault() ?? context.FirstOrDefault<DbPerson>(o => o.ParentKey == dbEntityVersion.VersionKey),
                            dbEntityVersion,
                            dbEntity,
                            context);
                    else
                        retVal = new PersonPersistenceService(this.m_settingsProvider).ToModelInstance(
                            (dataInstance as CompositeResult)?.Values.OfType<DbPerson>().FirstOrDefault() ?? context.FirstOrDefault<DbPerson>(o => o.ParentKey == dbEntityVersion.VersionKey),
                            dbEntityVersion,
                            dbEntity,
                            context);
                    break;
                case EntityClassKeyStrings.Patient:
                    retVal = new PatientPersistenceService(this.m_settingsProvider).ToModelInstance(
                        (dataInstance as CompositeResult)?.Values.OfType<DbPatient>().FirstOrDefault() ?? context.FirstOrDefault<DbPatient>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        (dataInstance as CompositeResult)?.Values.OfType<DbPerson>().FirstOrDefault() ?? context.FirstOrDefault<DbPerson>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        dbEntityVersion,
                        dbEntity,
                        context);
                    break;
                case EntityClassKeyStrings.Provider:
                    retVal = new ProviderPersistenceService(this.m_settingsProvider).ToModelInstance(
                        (dataInstance as CompositeResult)?.Values.OfType<DbProvider>().FirstOrDefault() ?? context.FirstOrDefault<DbProvider>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        (dataInstance as CompositeResult)?.Values.OfType<DbPerson>().FirstOrDefault() ?? context.FirstOrDefault<DbPerson>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        dbEntityVersion,
                        dbEntity,
                        context);
                    break;
                case EntityClassKeyStrings.Place:
                case EntityClassKeyStrings.CityOrTown:
                case EntityClassKeyStrings.Country:
                case EntityClassKeyStrings.CountyOrParish:
                case EntityClassKeyStrings.PrecinctOrBorough:
                case EntityClassKeyStrings.State:
                case EntityClassKeyStrings.ServiceDeliveryLocation:
                    retVal = new PlacePersistenceService(this.m_settingsProvider).ToModelInstance(
                        (dataInstance as CompositeResult)?.Values.OfType<DbPlace>().FirstOrDefault() ?? context.FirstOrDefault<DbPlace>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        dbEntityVersion,
                        dbEntity,
                        context);
                    break;
                case EntityClassKeyStrings.Organization:
                    retVal = new OrganizationPersistenceService(this.m_settingsProvider).ToModelInstance(
                        (dataInstance as CompositeResult)?.Values.OfType<DbOrganization>().FirstOrDefault() ?? context.FirstOrDefault<DbOrganization>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        dbEntityVersion,
                        dbEntity,
                        context);
                    break;
                case EntityClassKeyStrings.Material:
                    retVal = new MaterialPersistenceService(this.m_settingsProvider).ToModelInstance<Material>(
                        (dataInstance as CompositeResult)?.Values.OfType<DbMaterial>().FirstOrDefault() ?? context.FirstOrDefault<DbMaterial>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        dbEntityVersion,
                        dbEntity,
                        context);
                    break;
                case EntityClassKeyStrings.ManufacturedMaterial:
                    retVal = new ManufacturedMaterialPersistenceService(this.m_settingsProvider).ToModelInstance(
                        (dataInstance as CompositeResult)?.Values.OfType<DbManufacturedMaterial>().FirstOrDefault() ?? context.FirstOrDefault<DbManufacturedMaterial>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        (dataInstance as CompositeResult)?.Values.OfType<DbMaterial>().FirstOrDefault() ?? context.FirstOrDefault<DbMaterial>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        dbEntityVersion,
                        dbEntity,
                        context);
                    break;
                default:
                    retVal = this.ToModelInstance<Entity>(dbEntityVersion, dbEntity, context);
                    break;

            }

#if DEBUG
            sw.Stop();
            this.m_tracer.TraceEvent(EventLevel.Verbose, "Basic conversion took: {0}", sw.ElapsedMilliseconds);
#endif 

            retVal.LoadAssociations(context);
            return retVal;
        }

        /// <summary>
        /// Conversion based on type
        /// </summary>
        protected override Entity CacheConvert(object dataInstance, DataContext context)
        {
            return this.DoCacheConvert(dataInstance, context);
        }

        /// <summary>
        /// Perform the cache convert
        /// </summary>
        internal Entity DoCacheConvert(object dataInstance, DataContext context)
        {
            if (dataInstance == null)
                return null;
            // Alright first, which type am I mapping to?
            var dbEntityVersion = (dataInstance as CompositeResult)?.Values.OfType<DbEntityVersion>().FirstOrDefault() ?? dataInstance as DbEntityVersion ?? context.FirstOrDefault<DbEntityVersion>(o => o.VersionKey == (dataInstance as DbEntitySubTable).ParentKey);
            var dbEntity = (dataInstance as CompositeResult)?.Values.OfType<DbEntity>().FirstOrDefault() ?? context.FirstOrDefault<DbEntity>(o => o.Key == dbEntityVersion.Key);
            Entity retVal = null;
            var cache = new AdoPersistenceCache(context);

            if (!dbEntityVersion.ObsoletionTime.HasValue)
                switch (dbEntity.ClassConceptKey.ToString().ToUpper())
                {
                    case EntityClassKeyStrings.Device:
                        retVal = cache?.GetCacheItem<DeviceEntity>(dbEntity.Key);
                        break;
                    case EntityClassKeyStrings.NonLivingSubject:
                        retVal = cache?.GetCacheItem<ApplicationEntity>(dbEntity.Key);
                        break;
                    case EntityClassKeyStrings.Person:
                        var ue = (dataInstance as CompositeResult)?.Values.OfType<DbUserEntity>().FirstOrDefault() ?? context.FirstOrDefault<DbUserEntity>(o => o.ParentKey == dbEntityVersion.VersionKey);
                        if (ue != null)
                            retVal = cache?.GetCacheItem<UserEntity>(dbEntity.Key);

                        else
                            retVal = cache?.GetCacheItem<Person>(dbEntity.Key);
                        break;
                    case EntityClassKeyStrings.Patient:
                        retVal = cache?.GetCacheItem<Patient>(dbEntity.Key);
                        break;
                    case EntityClassKeyStrings.Provider:
                        retVal = cache?.GetCacheItem<Provider>(dbEntity.Key);

                        break;
                    case EntityClassKeyStrings.Place:
                    case EntityClassKeyStrings.CityOrTown:
                    case EntityClassKeyStrings.Country:
                    case EntityClassKeyStrings.CountyOrParish:
                    case EntityClassKeyStrings.State:
                    case EntityClassKeyStrings.PrecinctOrBorough:
                    case EntityClassKeyStrings.ServiceDeliveryLocation:
                        retVal = cache?.GetCacheItem<Place>(dbEntity.Key);

                        break;
                    case EntityClassKeyStrings.Organization:
                        retVal = cache?.GetCacheItem<Organization>(dbEntity.Key);

                        break;
                    case EntityClassKeyStrings.Material:
                        retVal = cache?.GetCacheItem<Material>(dbEntity.Key);

                        break;
                    case EntityClassKeyStrings.ManufacturedMaterial:
                        retVal = cache?.GetCacheItem<ManufacturedMaterial>(dbEntity.Key);

                        break;
                    default:
                        retVal = cache?.GetCacheItem<Entity>(dbEntity.Key);
                        break;
                }

            // Return cache value
            if (retVal != null)
                return retVal;
            else
                return base.CacheConvert(dataInstance, context);
        }

        /// <summary>
        /// Insert the specified entity into the data context
        /// </summary>
        public Core.Model.Entities.Entity InsertCoreProperties(DataContext context, Core.Model.Entities.Entity data)
        {

            // The creator asserted from the persistence call
            Guid? assertedCreator = data.CreatedByKey;

            // Ensure FK exists
            if (data.ClassConcept != null) data.ClassConcept = data.ClassConcept?.EnsureExists(context) as Concept;
            if (data.DeterminerConcept != null) data.DeterminerConcept = data.DeterminerConcept?.EnsureExists(context) as Concept;
            if (data.StatusConcept != null) data.StatusConcept = data.StatusConcept?.EnsureExists(context) as Concept;
            if (data.TypeConcept != null) data.TypeConcept = data.TypeConcept?.EnsureExists(context) as Concept;
            if (data.Template != null) data.Template = data.Template?.EnsureExists(context) as TemplateDefinition;
            if (data.CreationAct != null) data.CreationAct = data.CreationAct?.EnsureExists(context) as Act;
            data.TypeConceptKey = data.TypeConcept?.Key ?? data.TypeConceptKey;
            data.DeterminerConceptKey = data.DeterminerConcept?.Key ?? data.DeterminerConceptKey;
            data.ClassConceptKey = data.ClassConcept?.Key ?? data.ClassConceptKey;
            data.StatusConceptKey = data.StatusConcept?.Key ?? data.StatusConceptKey;
            data.StatusConceptKey = data.StatusConceptKey == Guid.Empty || data.StatusConceptKey == null ? StatusKeys.New : data.StatusConceptKey;
            data.CreationActKey = data.CreationAct?.Key ?? data.CreationActKey;

            var retVal = base.InsertInternal(context, data);

            // Identifiers
            if (data.Identifiers != null)
            {
                this.VerifyIdentities(context, data, assertedCreator);
                // Assert 
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.EntityIdentifier, DbEntityIdentifier>(
                    data.Identifiers.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);
            }

            // Relationships
            if (data.Relationships != null)
                base.UpdateVersionedAssociatedItems<Core.Model.Entities.EntityRelationship, DbEntityRelationship>(
                   data.Relationships.Distinct(new EntityRelationshipPersistenceService.Comparer()).Where(o => o != null && !o.InversionIndicator && !o.IsEmpty()).ToList(),
                    retVal,
                    context);

            // Telecoms
            if (data.Telecoms != null)
                base.UpdateVersionedAssociatedItems<Core.Model.Entities.EntityTelecomAddress, DbTelecomAddress>(
                   data.Telecoms.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);

            // Extensions
            if (data.Extensions != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.EntityExtension, DbEntityExtension>(
                   data.Extensions.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);

            // Names
            if (data.Names != null)
                base.UpdateVersionedAssociatedItems<Core.Model.Entities.EntityName, DbEntityName>(
                   data.Names.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);

            // Addresses
            if (data.Addresses != null)
                base.UpdateVersionedAssociatedItems<Core.Model.Entities.EntityAddress, DbEntityAddress>(
                   data.Addresses.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);

            // Notes
            if (data.Notes != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.EntityNote, DbEntityNote>(
                   data.Notes.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);

            // Tags
            if (data.Tags != null)
                base.UpdateAssociatedItems<Core.Model.DataTypes.EntityTag, DbEntityTag>(
                   data.Tags.Where(o => o != null && !o.IsEmpty() && !o.TagKey.StartsWith("$")),
                    retVal,
                    context);

            // Persist policies
            if (data.Policies != null && data.Policies.Any())
            {
                foreach (var p in data.Policies)
                {
                    var pol = p.Policy?.EnsureExists(context);
                    var polKey = p.Policy?.Key ?? p.PolicyKey;
                    if (polKey == null) // maybe we can retrieve it from the PIP?
                    {
                        var pipInfo = ApplicationServiceContext.Current.GetService<IPolicyInformationService>().GetPolicy(p.PolicyKey.ToString());
                        if (pipInfo != null)
                        {
                            p.Policy = new Core.Model.Security.SecurityPolicy()
                            {
                                Oid = pipInfo.Oid,
                                Name = pipInfo.Name,
                                CanOverride = pipInfo.CanOverride
                            };
                            pol = p.Policy.EnsureExists(context);
                            polKey = pol.Key;
                        }
                        else throw new InvalidOperationException("Cannot find policy information");
                    }

                    // Insert
                    context.Insert(new DbEntitySecurityPolicy()
                    {
                        Key = Guid.NewGuid(),
                        PolicyKey = polKey.Value,
                        SourceKey = retVal.Key.Value,
                        EffectiveVersionSequenceId = retVal.VersionSequence.Value
                    });
                }
            }

            return retVal;
        }

        /// <summary>
        /// Verify the identities are appropriately designed
        /// </summary>
        private void VerifyIdentities(DataContext context, Entity data, Guid? assertedCreator)
        {
            // Validate unique values for IDs
            DbSecurityProvenance provenance = null;
            var issues = new List<DetectedIssue>();
            var priority = this.m_settingsProvider.GetConfiguration().Validation.ValidationLevel;
            var adhocCache = ApplicationServiceContext.Current.GetService<IAdhocCacheService>();

            foreach (var id in data.Identifiers)
            {
                // Get ID
                DbAssigningAuthority dbAuth = null;

                if (id.AuthorityKey.HasValue) // Attempt lookup in adhoc cache then by db
                {
                    dbAuth = adhocCache?.Get<DbAssigningAuthority>($"aa.{id.AuthorityKey}");
                    if(dbAuth == null)
                        dbAuth = context.FirstOrDefault<DbAssigningAuthority>(o => o.Key == id.AuthorityKey);
                }
                else 
                {
                    dbAuth = adhocCache?.Get<DbAssigningAuthority>($"aa.{id.Authority.DomainName}");
                    if (dbAuth == null)
                    {
                        dbAuth = context.FirstOrDefault<DbAssigningAuthority>(o => o.DomainName == id.Authority.DomainName);
                        if (dbAuth != null)
                            id.AuthorityKey = dbAuth.Key;
                    }

                }

                if (dbAuth == null)
                {
                    issues.Add(new DetectedIssue(priority, "id.aa.notFound", $"Missing assigning authority with ID {String.Join(",", data.Identifiers.Select(o => o.AuthorityKey))}", DetectedIssueKeys.SafetyConcernIssue));
                    continue;
                }
                else
                {
                    adhocCache?.Add($"aa.{id.AuthorityKey}", dbAuth, new TimeSpan(0, 5, 0));
                    adhocCache?.Add($"aa.{dbAuth.DomainName}", dbAuth, new TimeSpan(0, 5, 0));
                }

                // Get this identifier records which is not owned by my record
                var ownedByOthers = context.Query<DbEntityIdentifier>(
                    context.CreateSqlStatement()
                    .SelectFrom(typeof(DbEntityIdentifier))
                    .Where<DbEntityIdentifier>(o => o.Value == id.Value && o.AuthorityKey == id.AuthorityKey && o.ObsoleteVersionSequenceId == null && o.SourceKey != data.Key)
                    .And("NOT EXISTS (SELECT 1 FROM ent_rel_tbl WHERE (src_ent_id = ? AND trg_ent_id = ent_id_tbl.ent_id OR trg_ent_id = ? AND src_ent_id = ent_id_tbl.ent_id) AND obslt_vrsn_seq_id IS NULL)", data.Key, data.Key)
                ).Any();
                var ownedByMe = context.Query<DbEntityIdentifier>(
                    context.CreateSqlStatement()
                    .SelectFrom(typeof(DbEntityIdentifier))
                    .Where<DbEntityIdentifier>(o => o.Value == id.Value && o.AuthorityKey == id.AuthorityKey && o.ObsoleteVersionSequenceId == null)
                    .And("(ent_id = ? OR EXISTS (SELECT 1 FROM ent_rel_tbl WHERE (src_ent_id = ?  AND trg_ent_id = ent_id_tbl.ent_id) OR (trg_ent_id = ? AND src_ent_id = ent_id_tbl.ent_id) AND obslt_vrsn_seq_id IS NULL))", data.Key, data.Key, data.Key)
                ).Any();

                // Verify scope
                IEnumerable<DbAuthorityScope> scopes = adhocCache?.Get<DbAuthorityScope[]>($"aa.scp.{dbAuth.Key}");
                if (scopes == null)
                {
                    scopes = context.Query<DbAuthorityScope>(o => o.AssigningAuthorityKey == dbAuth.Key);
                    adhocCache?.Add($"aa.scp.{dbAuth.Key}", scopes.ToArray());
                }

                if (scopes.Any() && !scopes.Any(s => s.ScopeConceptKey == data.ClassConceptKey) // This type of identifier is not allowed to be assigned to this type of object
                    && !ownedByOthers
                    && !ownedByMe) // Unless it was already associated to another type of object related to me
                    issues.Add(new DetectedIssue(DetectedIssuePriorityType.Error, "id.target", $"Identifier of type {dbAuth.DomainName} cannot be assigned to object of type {data.ClassConceptKey}", DetectedIssueKeys.BusinessRuleViolationIssue));

                // If the identity domain is unique, and we've been asked to raid identifier uq issues
                if (dbAuth.IsUnique && this.m_settingsProvider.GetConfiguration().Validation.IdentifierUniqueness &&
                    ownedByOthers)
                    issues.Add(new DetectedIssue(priority, $"id.uniqueness", $"Identifier {id.Value} in domain {dbAuth.DomainName} violates unique constraint", DetectedIssueKeys.FormalConstraintIssue));
                if (dbAuth.AssigningApplicationKey.HasValue) // Must have permission
                {
                    if (provenance == null) provenance = context.FirstOrDefault<DbSecurityProvenance>(o => o.Key == data.CreatedByKey);


                    if (provenance.ApplicationKey != dbAuth.AssigningApplicationKey  // Established prov key
                        && assertedCreator != dbAuth.AssigningApplicationKey  // original prov key
                        && !ownedByMe 
                        && !ownedByOthers) // and has not already been assigned to me or anyone else (it is a new , unknown identifier)
                        issues.Add(new DetectedIssue(DetectedIssuePriorityType.Error, $"id.authority", $"Application {provenance.ApplicationKey} does not have permission to assign {dbAuth.DomainName}", DetectedIssueKeys.SecurityIssue));
                }
                if (!String.IsNullOrEmpty(dbAuth.ValidationRegex) && this.m_settingsProvider.GetConfiguration().Validation.IdentifierFormat) // must be valid
                {
                    var nonMatch = !new Regex(dbAuth.ValidationRegex).IsMatch(id.Value);
                    if (nonMatch)
                        issues.Add(new DetectedIssue(priority, $"id.format", $"Identifier {id.Value} in domain {dbAuth.DomainName} failed format validation", DetectedIssueKeys.FormalConstraintIssue));
                }
            }

            // Validate issues
            if (issues.Any(o => o.Priority == DetectedIssuePriorityType.Error))
                throw new DetectedIssueException(issues);
            else if (issues.Count > 0)
            {
                var extension = data.Extensions.FirstOrDefault(o => o.ExtensionTypeKey == ExtensionTypeKeys.DataQualityExtension);
                if (extension == null)
                    data.Extensions.Add(new EntityExtension(ExtensionTypeKeys.DataQualityExtension, typeof(DictionaryExtensionHandler), issues));
                else
                {
                    var existingValues = extension.GetValue<List<DetectedIssue>>();
                    existingValues.AddRange(issues);
                    extension.ExtensionValue = existingValues;
                }
            }

        }

        /// <summary>
        /// Update the specified entity
        /// </summary>
        public Core.Model.Entities.Entity UpdateCoreProperties(DataContext context, Core.Model.Entities.Entity data)
        {

            // Asserted creator of this version
            Guid? assertedCreator = data.CreatedByKey;

            // Esnure exists
            if (data.ClassConcept != null) data.ClassConcept = data.ClassConcept?.EnsureExists(context) as Concept;
            if (data.DeterminerConcept != null) data.DeterminerConcept = data.DeterminerConcept?.EnsureExists(context) as Concept;
            if (data.StatusConcept != null) data.StatusConcept = data.StatusConcept?.EnsureExists(context) as Concept;
            if (data.Template != null) data.Template = data.Template?.EnsureExists(context) as TemplateDefinition;
            if (data.TypeConcept != null) data.TypeConcept = data.TypeConcept?.EnsureExists(context) as Concept;
            data.TypeConceptKey = data.TypeConcept?.Key ?? data.TypeConceptKey;
            data.DeterminerConceptKey = data.DeterminerConcept?.Key ?? data.DeterminerConceptKey;
            data.ClassConceptKey = data.ClassConcept?.Key ?? data.ClassConceptKey;
            data.StatusConceptKey = data.StatusConcept?.Key ?? data.StatusConceptKey;
            data.StatusConceptKey = data.StatusConceptKey == Guid.Empty || data.StatusConceptKey == null ? StatusKeys.New : data.StatusConceptKey;

            var retVal = base.UpdateInternal(context, data);


            // Identifiers
            if (data.Identifiers != null)
            {
                this.VerifyIdentities(context, data, assertedCreator);

                // Validate unique values for IDs
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.EntityIdentifier, DbEntityIdentifier>(
                    data.Identifiers.Where(o => !o.IsEmpty()),
                    retVal,
                    context);
            }

            // Relationships
            if (data.Relationships != null)
                base.UpdateVersionedAssociatedItems<Core.Model.Entities.EntityRelationship, DbEntityRelationship>(
                   data.Relationships,
                    retVal,
                    context);

            // Telecoms
            if (data.Telecoms != null)
                base.UpdateVersionedAssociatedItems<Core.Model.Entities.EntityTelecomAddress, DbTelecomAddress>(
                   data.Telecoms.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);

            // Extensions
            if (data.Extensions != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.EntityExtension, DbEntityExtension>(
                   data.Extensions.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);

            // Names
            if (data.Names != null)
                base.UpdateVersionedAssociatedItems<Core.Model.Entities.EntityName, DbEntityName>(
                   data.Names.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);

            // Addresses
            if (data.Addresses != null)
                base.UpdateVersionedAssociatedItems<Core.Model.Entities.EntityAddress, DbEntityAddress>(
                   data.Addresses.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);

            // Notes
            if (data.Notes != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.EntityNote, DbEntityNote>(
                   data.Notes.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);

            // Tags
            if (data.Tags != null)
                base.UpdateAssociatedItems<Core.Model.DataTypes.EntityTag, DbEntityTag>(
                   data.Tags.Where(o => o != null && !o.IsEmpty() && !o.TagKey.StartsWith("$")),
                    retVal,
                    context);

            // Persist policies
            if (data.Policies != null && data.Policies.Any())
            {
                // Delete / obsolete any old policies
                foreach(var pol in context.Query<DbEntitySecurityPolicy>(o=>o.SourceKey == data.Key))
                    if (!data.Policies.Any(o => o.PolicyKey == pol.PolicyKey))
                    {
                        pol.ObsoleteVersionSequenceId = retVal.VersionSequence;
                        context.Update(pol);
                    }

                // Now update any policies that don't exist
                foreach (var p in data.Policies)
                {
                    var pol = p.Policy?.EnsureExists(context);
                    var polKey = pol?.Key ?? p.PolicyKey;
                    if (polKey == null) // maybe we can retrieve it from the PIP?
                    {
                        var pipInfo = ApplicationServiceContext.Current.GetService<IPolicyInformationService>().GetPolicy(p.PolicyKey.ToString());
                        if (pipInfo != null)
                        {
                            p.Policy = new Core.Model.Security.SecurityPolicy()
                            {
                                Oid = pipInfo.Oid,
                                Name = pipInfo.Name,
                                CanOverride = pipInfo.CanOverride
                            };
                            pol = p.Policy.EnsureExists(context);
                            polKey = pol.Key;

                        }
                        else throw new InvalidOperationException("Cannot find policy information");
                    }

                    // Insert
                    if(!context.Any<DbEntitySecurityPolicy>(o=>o.SourceKey == retVal.Key && o.ObsoleteVersionSequenceId == null && o.PolicyKey == polKey.Value))
                        context.Insert(new DbEntitySecurityPolicy()
                        {
                            Key = Guid.NewGuid(),
                            PolicyKey = polKey.Value,
                            SourceKey = retVal.Key.Value,
                            EffectiveVersionSequenceId = retVal.VersionSequence.Value
                        });
                }
            }

            return retVal;
        }

        /// <summary>
        /// Obsoleted status key
        /// </summary>
        public override Core.Model.Entities.Entity ObsoleteInternal(DataContext context, Core.Model.Entities.Entity data)
        {
            data.StatusConceptKey = StatusKeys.Obsolete;
            return base.UpdateInternal(context, data);
        }

        /// <summary>
        /// Insert the entity
        /// </summary>
        public override Entity InsertInternal(DataContext context, Entity data)
        {
            switch (data.ClassConceptKey.ToString().ToUpper())
            {
                case EntityClassKeyStrings.Device:
                    return new DeviceEntityPersistenceService(this.m_settingsProvider).InsertInternal(context, data.Convert<DeviceEntity>());
                case EntityClassKeyStrings.NonLivingSubject:
                    return new ApplicationEntityPersistenceService(this.m_settingsProvider).InsertInternal(context, data.Convert<ApplicationEntity>());
                case EntityClassKeyStrings.Person:
                    return new PersonPersistenceService(this.m_settingsProvider).InsertInternal(context, data.Convert<Person>());
                case EntityClassKeyStrings.Patient:
                    return new PatientPersistenceService(this.m_settingsProvider).InsertInternal(context, data.Convert<Patient>());
                case EntityClassKeyStrings.Provider:
                    return new ProviderPersistenceService(this.m_settingsProvider).InsertInternal(context, data.Convert<Provider>());
                case EntityClassKeyStrings.Place:
                case EntityClassKeyStrings.CityOrTown:
                case EntityClassKeyStrings.Country:
                case EntityClassKeyStrings.CountyOrParish:
                case EntityClassKeyStrings.State:
                case EntityClassKeyStrings.PrecinctOrBorough:
                case EntityClassKeyStrings.ServiceDeliveryLocation:
                    return new PlacePersistenceService(this.m_settingsProvider).InsertInternal(context, data.Convert<Place>());
                case EntityClassKeyStrings.Organization:
                    return new OrganizationPersistenceService(this.m_settingsProvider).InsertInternal(context, data.Convert<Organization>());
                case EntityClassKeyStrings.Material:
                    return new MaterialPersistenceService(this.m_settingsProvider).InsertInternal(context, data.Convert<Material>());
                case EntityClassKeyStrings.ManufacturedMaterial:
                    return new ManufacturedMaterialPersistenceService(this.m_settingsProvider).InsertInternal(context, data.Convert<ManufacturedMaterial>());
                default:
                    return this.InsertCoreProperties(context, data);

            }
        }

        /// <summary>
        /// Update entity
        /// </summary>
        public override Entity UpdateInternal(DataContext context, Entity data)
        {
            switch (data.ClassConceptKey.ToString().ToUpper())
            {
                case EntityClassKeyStrings.Device:
                    return new DeviceEntityPersistenceService(this.m_settingsProvider).UpdateInternal(context, data.Convert<DeviceEntity>());
                case EntityClassKeyStrings.NonLivingSubject:
                    return new ApplicationEntityPersistenceService(this.m_settingsProvider).UpdateInternal(context, data.Convert<ApplicationEntity>());
                case EntityClassKeyStrings.Person:
                    return new PersonPersistenceService(this.m_settingsProvider).UpdateInternal(context, data.Convert<Person>());
                case EntityClassKeyStrings.Patient:
                    return new PatientPersistenceService(this.m_settingsProvider).UpdateInternal(context, data.Convert<Patient>());
                case EntityClassKeyStrings.Provider:
                    return new ProviderPersistenceService(this.m_settingsProvider).UpdateInternal(context, data.Convert<Provider>());
                case EntityClassKeyStrings.Place:
                case EntityClassKeyStrings.CityOrTown:
                case EntityClassKeyStrings.Country:
                case EntityClassKeyStrings.CountyOrParish:
                case EntityClassKeyStrings.PrecinctOrBorough:
                case EntityClassKeyStrings.State:
                case EntityClassKeyStrings.ServiceDeliveryLocation:
                    return new PlacePersistenceService(this.m_settingsProvider).UpdateInternal(context, data.Convert<Place>());
                case EntityClassKeyStrings.Organization:
                    return new OrganizationPersistenceService(this.m_settingsProvider).UpdateInternal(context, data.Convert<Organization>());
                case EntityClassKeyStrings.Material:
                    return new MaterialPersistenceService(this.m_settingsProvider).UpdateInternal(context, data.Convert<Material>());
                case EntityClassKeyStrings.ManufacturedMaterial:
                    return new ManufacturedMaterialPersistenceService(this.m_settingsProvider).UpdateInternal(context, data.Convert<ManufacturedMaterial>());
                default:
                    return this.UpdateCoreProperties(context, data);

            }
        }

        /// <summary>
        /// Perform a purge of this data
        /// </summary>
        protected override void BulkPurgeInternal(DataContext context, Guid[] keysToPurge)
        {
            // Purge the related fields
            int ofs = 0;
            while (ofs < keysToPurge.Length)
            {
                var batchKeys = keysToPurge.Skip(ofs).Take(100).ToArray();
                ofs += 100;
                // Purge the related fields
                var versionKeys = context.Query<DbEntityVersion>(o => batchKeys.Contains(o.Key)).Select(o => o.VersionKey).ToArray();

                // Delete versions of this entity in sub table
                context.Delete<DbPatient>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbProvider>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbUserEntity>(o => versionKeys.Contains(o.ParentKey));

                // Person fields
                context.Delete<DbPersonLanguageCommunication>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbPerson>(o => versionKeys.Contains(o.ParentKey));

                // Other entities
                context.Delete<DbDeviceEntity>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbApplicationEntity>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbPlace>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbManufacturedMaterial>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbMaterial>(o => versionKeys.Contains(o.ParentKey));
                // Purge the related entity fields

                // Names
                var col = TableMapping.Get(typeof(DbEntityNameComponent)).Columns.First(o => o.IsPrimaryKey);
                var deleteStatement = context.CreateSqlStatement<DbEntityNameComponent>()
                    .SelectFrom(col)
                    .InnerJoin<DbEntityNameComponent, DbEntityName>(o => o.SourceKey, o => o.Key)
                    .Where<DbEntityName>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbEntityNameComponent>(deleteStatement);
                context.Delete<DbEntityName>(o => batchKeys.Contains(o.SourceKey));

                // Addresses
                col = TableMapping.Get(typeof(DbEntityAddressComponent)).Columns.First(o => o.IsPrimaryKey);
                deleteStatement = context.CreateSqlStatement<DbEntityAddressComponent>()
                    .SelectFrom(col)
                    .InnerJoin<DbEntityAddressComponent, DbEntityAddress>(o => o.SourceKey, o => o.Key)
                    .Where<DbEntityAddress>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbEntityAddressComponent>(deleteStatement);
                context.Delete<DbEntityAddress>(o => batchKeys.Contains(o.SourceKey));

                // Other Relationships
                context.Delete<DbEntityRelationship>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbEntityIdentifier>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbEntityExtension>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbEntityTag>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbEntityNote>(o => batchKeys.Contains(o.SourceKey));

                // Detach keys which are being deleted will need to be removed from the version heirarchy
                foreach(var rpl in context.Query<DbEntityVersion>(o => versionKeys.Contains(o.ReplacesVersionKey.Value)).ToArray())
                {
                    rpl.ReplacesVersionKey = null;
                    rpl.ReplacesVersionKeySpecified = true;
                    context.Update(rpl);
                }

                // Purge the core entity data
                context.Delete<DbEntityVersion>(o => batchKeys.Contains(o.Key));
                
                // Create a version which indicates this is PURGED
                context.Insert(
                    context.Query<DbEntity>(o=>batchKeys.Contains(o.Key))
                    .Select(o=>o.Key)
                    .Distinct()
                    .ToArray()
                    .Select(o => new DbEntityVersion()
                {
                    CreatedByKey = context.ContextId,
                    CreationTime = DateTimeOffset.Now,
                    Key = o,
                    StatusConceptKey = StatusKeys.Purged
                }));
                
            }

            context.ResetSequence("ENT_VRSN_SEQ", context.Query<DbEntityVersion>(o => true).Max(o => o.VersionSequenceId));

        }

        /// <summary>
        /// Copy entity data from <paramref name="fromContext"/> to <paramref name="toContext"/>
        /// </summary>
        public override void Copy(Guid[] keysToCopy, DataContext fromContext, DataContext toContext)
        {
            // TODO:Clean this mess up

            toContext.InsertOrUpdate(fromContext.Query<DbPhoneticValue>(o => o.SequenceId >= 0));
            toContext.InsertOrUpdate(fromContext.Query<DbEntityAddressComponentValue>(o => o.SequenceId >= 0));

            // Copy over users and protocols and other act tables
            IEnumerable<Guid> additionalKeys = fromContext.Query<DbExtensionType>(o => o.ObsoletionTime == null)
                .Select(o => o.CreatedByKey)
                .Distinct()
                .Union(
                    fromContext.Query<DbAssigningAuthority>(o => o.ObsoletionTime == null)
                    .Select(o => o.CreatedByKey)
                    .Distinct()
                )
                .Union(
                    fromContext.Query<DbTemplateDefinition>(o => o.ObsoletionTime == null)
                    .Select(o => o.CreatedByKey)
                    .Distinct()
                )
                .Union(
                    fromContext.Query<DbSecurityApplication>(o => o.ObsoletionTime == null)
                    .Select(o => o.CreatedByKey)
                    .Distinct()
                )
                .ToArray();
            toContext.InsertOrUpdate(fromContext.Query<DbSecurityProvenance>(o => additionalKeys.Contains(o.Key)));
            toContext.InsertOrUpdate(fromContext.Query<DbExtensionType>(o => o.ObsoletionTime == null));
            toContext.InsertOrUpdate(fromContext.Query<DbTemplateDefinition>(o => o.ObsoletionTime == null));

            additionalKeys = fromContext.Query<DbAssigningAuthority>(o => o.ObsoletionTime == null)
                .Select(o => o.AssigningApplicationKey).Distinct().ToArray()
                .Where(o => o.HasValue)
                .Select(o => o.Value);

            toContext.InsertOrUpdate(fromContext.Query<DbSecurityApplication>(o => additionalKeys.Contains(o.Key)).ToArray().Select(o=>new DbSecurityApplication()
            {   
                Key = o.Key,
                CreatedByKey = o.CreatedByKey,
                CreationTime =o.CreationTime,
                InvalidAuthAttempts = 0,
                Lockout = o.Lockout,
                LastAuthentication = o.LastAuthentication,
                PublicId = o.PublicId,
                ObsoletionTime =o.ObsoletionTime,
                ObsoletedByKey = o.ObsoletedByKey,
                Secret = o.Secret ?? "XXXX",
                UpdatedByKey = o.UpdatedByKey,
                UpdatedTime = o.UpdatedTime
            }));
            toContext.InsertOrUpdate(fromContext.Query<DbAssigningAuthority>(o => o.ObsoletionTime == null));

            additionalKeys = fromContext.Query<DbEntityRelationship>(o => o.ObsoleteVersionSequenceId == null)
                   .Select(o => o.RelationshipTypeKey)
                   .Distinct()
                   .Union(
                        fromContext.Query<DbEntity>(o => o.Key != null)
                        .Select(o => o.DeterminerConceptKey)
                        .Distinct()
                    ).Union(
                        fromContext.Query<DbEntity>(o => o.Key != null)
                        .Select(o => o.ClassConceptKey)
                        .Distinct()
                    ).Union(
                        fromContext.Query<DbEntityVersion>(o => o.VersionSequenceId > 0)
                        .Select(o => o.StatusConceptKey)
                        .Distinct()
                    ).Union(
                        fromContext.Query<DbEntityVersion>(o => o.VersionSequenceId > 0)
                        .Select(o => o.TypeConceptKey)
                        .Distinct()
                        .ToArray()
                        .Where(o => o.HasValue)
                        .Select(o => o.Value)
                    ).Union(
                        fromContext.Query<DbPatient>(o => o.ParentKey != null)
                        .Select(o => o.GenderConceptKey)
                        .Distinct()
                    )
                   .ToArray();
            additionalKeys = additionalKeys.Union(
               fromContext.Query<DbPatient>(o => o.ParentKey != null)
                        .Select(o => o.EducationLevelKey)
                        .Distinct()
                    .Union(
                        fromContext.Query<DbPatient>(o => o.ParentKey != null)
                        .Select(o => o.EthnicGroupCodeKey)
                        .Distinct()
                    )
                    .Union(
                        fromContext.Query<DbPatient>(o => o.ParentKey != null)
                        .Select(o => o.LivingArrangementKey)
                        .Distinct()
                    )
                    .Union(
                        fromContext.Query<DbPatient>(o => o.ParentKey != null)
                        .Select(o => o.MaritalStatusKey)
                        .Distinct()
                    )
                    .Where(o=>o.HasValue)
                    .Select(o=>o.Value)
                ).ToArray();
            toContext.InsertOrUpdate(fromContext.Query<DbConceptClass>(o => true));
            toContext.InsertOrUpdate(fromContext.Query<DbConcept>(o => additionalKeys.Contains(o.Key)));
            toContext.InsertOrUpdate(fromContext.Query<DbConceptVersion>(o => additionalKeys.Contains(o.Key)).OrderBy(o => o.VersionSequenceId));
            toContext.InsertOrUpdate(fromContext.Query<DbConceptSet>(o => true));
            toContext.InsertOrUpdate(fromContext.Query<DbConceptSetConceptAssociation>(o => additionalKeys.Contains(o.ConceptKey)));

            // Purge the related fields
            int ofs = 0;
            while (ofs < keysToCopy.Length)
            {
                var batchKeys = keysToCopy.Skip(ofs).Take(100).ToArray();
                ofs += 100;

                var versionKeys = fromContext.Query<DbEntityVersion>(o => batchKeys.Contains(o.Key)).Select(o => o.VersionKey).ToArray();

                // copy the core entity data
                toContext.InsertOrUpdate(fromContext.Query<DbEntity>(o => batchKeys.Contains(o.Key)));

                // copy the core entity data
                toContext.InsertOrUpdate(fromContext.Query<DbConcept>(o => additionalKeys.Contains(o.Key)));

                // Copy provenance of interest
                additionalKeys = fromContext.Query<DbEntityVersion>(o => batchKeys.Contains(o.Key))
                    .Select(o => o.CreatedByKey)
                    .Distinct()
                    .Union(
                        fromContext.Query<DbEntityVersion>(o => batchKeys.Contains(o.Key))
                        .Select(o => o.ObsoletedByKey)
                        .Distinct()
                        .ToArray()
                        .Where(o => o.HasValue)
                        .Select(o => o.Value)
                    )
                    .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbSecurityProvenance>(o => additionalKeys.Contains(o.Key)));

                toContext.InsertOrUpdate(fromContext.Query<DbEntityVersion>(o => batchKeys.Contains(o.Key)));

                additionalKeys = fromContext.Query<DbPlaceService>(o => batchKeys.Contains(o.SourceKey))
                    .Select(o => o.ServiceConceptKey)
                    .Distinct()
                    .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbConcept>(o => additionalKeys.Contains(o.Key)));
                toContext.InsertOrUpdate(fromContext.Query<DbPlaceService>(o => batchKeys.Contains(o.SourceKey)));

                // Person fields
                toContext.InsertOrUpdate(fromContext.Query<DbPerson>(o => versionKeys.Contains(o.ParentKey)));
                toContext.InsertOrUpdate(fromContext.Query<DbPersonLanguageCommunication>(o => batchKeys.Contains(o.SourceKey)));

                // Copy versions of persons
                toContext.InsertOrUpdate(fromContext.Query<DbPatient>(o => versionKeys.Contains(o.ParentKey)));

                additionalKeys = fromContext.Query<DbProvider>(o => versionKeys.Contains(o.ParentKey))
                    .Select(o => o.Specialty)
                    .Distinct()
                    .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbConcept>(o => additionalKeys.Contains(o.Key)));

                toContext.InsertOrUpdate(fromContext.Query<DbProvider>(o => versionKeys.Contains(o.ParentKey)));

                // Security Users
                additionalKeys = fromContext.Query<DbUserEntity>(o => versionKeys.Contains(o.ParentKey))
                    .Select(o => o.SecurityUserKey)
                    .Distinct()
                    .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbSecurityUser>(o => additionalKeys.Contains(o.Key)));
                toContext.InsertOrUpdate(fromContext.Query<DbUserEntity>(o => versionKeys.Contains(o.ParentKey)));

                // Other entities
                additionalKeys = fromContext.Query<DbDeviceEntity>(o => versionKeys.Contains(o.ParentKey))
                    .Select(o => o.SecurityDeviceKey)
                    .Distinct()
                    .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbSecurityDevice>(o => additionalKeys.Contains(o.Key)));
                toContext.InsertOrUpdate(fromContext.Query<DbDeviceEntity>(o => versionKeys.Contains(o.ParentKey)));

                additionalKeys = fromContext.Query<DbApplicationEntity>(o => versionKeys.Contains(o.ParentKey))
                   .Select(o => o.SecurityApplicationKey)
                   .Distinct()
                   .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbSecurityApplication>(o => additionalKeys.Contains(o.Key)));
                toContext.InsertOrUpdate(fromContext.Query<DbApplicationEntity>(o => versionKeys.Contains(o.ParentKey)));
                toContext.InsertOrUpdate(fromContext.Query<DbPlace>(o => versionKeys.Contains(o.ParentKey)));

                // Person fields
                additionalKeys = fromContext.Query<DbMaterial>(o => versionKeys.Contains(o.ParentKey))
                    .Select(o => o.QuantityConceptKey)
                    .Distinct()
                    .Union(
                        fromContext.Query<DbMaterial>(o => versionKeys.Contains(o.ParentKey))
                        .Select(o => o.FormConceptKey)
                        .Distinct()
                    )
                    .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbConcept>(o => additionalKeys.Contains(o.Key)));
                toContext.InsertOrUpdate(fromContext.Query<DbMaterial>(o => versionKeys.Contains(o.ParentKey)));
                toContext.InsertOrUpdate(fromContext.Query<DbManufacturedMaterial>(o => versionKeys.Contains(o.ParentKey)));

                // Names
                toContext.InsertOrUpdate(fromContext.Query<DbEntityName>(o => batchKeys.Contains(o.SourceKey)));

                // Insert component links
                var selectStatement = fromContext.CreateSqlStatement<DbEntityNameComponent>()
                    .SelectFrom()
                    .InnerJoin<DbEntityNameComponent, DbEntityName>(o => o.SourceKey, o => o.Key)
                    .Where<DbEntityName>(o => batchKeys.Contains(o.SourceKey));
                toContext.InsertOrUpdate(fromContext.Query<DbEntityNameComponent>(selectStatement));

                // Addresses
                
                toContext.InsertOrUpdate(fromContext.Query<DbEntityAddress>(o => batchKeys.Contains(o.SourceKey)));

                selectStatement = fromContext.CreateSqlStatement<DbEntityAddressComponent>()
                    .SelectFrom()
                    .InnerJoin<DbEntityAddressComponent, DbEntityAddress>(o => o.SourceKey, o => o.Key)
                    .Where<DbEntityAddress>(o => batchKeys.Contains(o.SourceKey));
                toContext.InsertOrUpdate(fromContext.Query<DbEntityAddressComponent>(selectStatement));

                // Other Relationships
                toContext.InsertOrUpdate(fromContext.Query<DbEntityIdentifier>(o => batchKeys.Contains(o.SourceKey)));

                // Entity Extension types
                toContext.InsertOrUpdate(fromContext.Query<DbEntityExtension>(o => batchKeys.Contains(o.SourceKey)));
                toContext.InsertOrUpdate(fromContext.Query<DbEntityTag>(o => batchKeys.Contains(o.SourceKey)));
                toContext.InsertOrUpdate(fromContext.Query<DbEntityNote>(o => batchKeys.Contains(o.SourceKey)));

                additionalKeys = fromContext.Query<DbEntityRelationship>(o => batchKeys.Contains(o.SourceKey))
                    .Select(o => o.TargetKey)
                    .Distinct()
                    .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbEntity>(o => additionalKeys.Contains(o.Key)));

               
                toContext.InsertOrUpdate(fromContext.Query<DbEntityRelationship>(o => batchKeys.Contains(o.SourceKey)));
            }

            toContext.ResetSequence("ENT_VRSN_SEQ", toContext.Query<DbEntityVersion>(o => true).Max(o => o.VersionSequenceId));

        }
    }
}
