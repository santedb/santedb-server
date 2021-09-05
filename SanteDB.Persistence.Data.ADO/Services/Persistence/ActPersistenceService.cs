/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2021-8-27
 */
using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
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
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Represents a persistence service which persists ACT classes
    /// </summary>
    public class ActPersistenceService : VersionedDataPersistenceService<Core.Model.Acts.Act, DbActVersion, DbAct>
    {

        public ActPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// To model instance
        /// </summary>
        public virtual TActType ToModelInstance<TActType>(DbActVersion dbInstance, DbAct actInstance, DataContext context) where TActType : Core.Model.Acts.Act, new()
        {

            var retVal = this.m_settingsProvider.GetMapper().MapDomainInstance<DbActVersion, TActType>(dbInstance);
            if (retVal == null) return null;

            retVal.ClassConceptKey = actInstance?.ClassConceptKey;
            retVal.MoodConceptKey = actInstance?.MoodConceptKey;
            retVal.TemplateKey = actInstance?.TemplateKey;
            return retVal;
        }

        /// <summary>
        /// Create an appropriate entity based on the class code
        /// </summary>
        public override Core.Model.Acts.Act ToModelInstance(object dataInstance, DataContext context)
        {
            // Alright first, which type am I mapping to?

            if (dataInstance == null) return null;

            DbActVersion dbActVersion = (dataInstance as CompositeResult)?.Values.OfType<DbActVersion>().FirstOrDefault() ?? dataInstance as DbActVersion ?? context.FirstOrDefault<DbActVersion>(o => o.VersionKey == (dataInstance as DbActSubTable).ParentKey);
            DbAct dbAct = (dataInstance as CompositeResult)?.Values.OfType<DbAct>().FirstOrDefault() ?? context.FirstOrDefault<DbAct>(o => o.Key == dbActVersion.Key);
            Act retVal = null;

            // 
            switch (dbAct.ClassConceptKey.ToString().ToLowerInvariant())
            {
                case ActClassKeyStrings.ControlAct:
                    retVal = new ControlActPersistenceService(this.m_settingsProvider).ToModelInstance(
                                (dataInstance as CompositeResult)?.Values.OfType<DbControlAct>().FirstOrDefault() ?? context.FirstOrDefault<DbControlAct>(o => o.ParentKey == dbActVersion.VersionKey),
                                dbActVersion,
                                dbAct,
                                context);
                    break;
                case ActClassKeyStrings.SubstanceAdministration:
                    retVal = new SubstanceAdministrationPersistenceService(this.m_settingsProvider).ToModelInstance(
                                (dataInstance as CompositeResult)?.Values.OfType<DbSubstanceAdministration>().FirstOrDefault() ?? context.FirstOrDefault<DbSubstanceAdministration>(o => o.ParentKey == dbActVersion.VersionKey),
                                dbActVersion,
                                dbAct,
                                context);
                    break;
                case ActClassKeyStrings.Observation:
                    var dbObs = (dataInstance as CompositeResult)?.Values.OfType<DbObservation>().FirstOrDefault() ?? context.FirstOrDefault<DbObservation>(o => o.ParentKey == dbActVersion.VersionKey);
                    if (dbObs == null)
                    {
                        this.m_tracer.TraceEvent(EventLevel.Warning, "Observation {0} is missing observation data! Even though class code is {1}", dbAct.Key, dbAct.ClassConceptKey);
                        retVal = this.ToModelInstance<Core.Model.Acts.Act>(dbActVersion, dbAct, context);
                    }
                    else
                        switch (dbObs.ValueType)
                        {
                            case "ST":
                                retVal = new TextObservationPersistenceService(this.m_settingsProvider).ToModelInstance(
                                    (dataInstance as CompositeResult)?.Values.OfType<DbTextObservation>().FirstOrDefault() ?? context.FirstOrDefault<DbTextObservation>(o => o.ParentKey == dbObs.ParentKey),
                                    dbObs,
                                    dbActVersion,
                                    dbAct,
                                    context);
                                break;
                            case "CD":
                                retVal = new CodedObservationPersistenceService(this.m_settingsProvider).ToModelInstance(
                                    (dataInstance as CompositeResult)?.Values.OfType<DbCodedObservation>().FirstOrDefault() ?? context.FirstOrDefault<DbCodedObservation>(o => o.ParentKey == dbObs.ParentKey),
                                    dbObs,
                                    dbActVersion,
                                    dbAct,
                                    context);
                                break;
                            case "PQ":
                                retVal = new QuantityObservationPersistenceService(this.m_settingsProvider).ToModelInstance(
                                    (dataInstance as CompositeResult)?.Values.OfType<DbQuantityObservation>().FirstOrDefault() ?? context.FirstOrDefault<DbQuantityObservation>(o => o.ParentKey == dbObs.ParentKey),
                                    dbObs,
                                    dbActVersion,
                                    dbAct,
                                    context);
                                break;
                            default:
                                retVal = new ObservationPersistenceService(this.m_settingsProvider).ToModelInstance(
                                    dbObs,
                                    dbActVersion,
                                    dbAct,
                                    context);
                                break;
                        }
                    break;
                case ActClassKeyStrings.Encounter:
                    retVal = new EncounterPersistenceService(this.m_settingsProvider).ToModelInstance(
                                (dataInstance as CompositeResult)?.Values.OfType<DbPatientEncounter>().FirstOrDefault() ?? context.FirstOrDefault<DbPatientEncounter>(o => o.ParentKey == dbActVersion.VersionKey),
                                dbActVersion,
                                dbAct,
                                context);
                    break;
                case ActClassKeyStrings.Condition:
                default:
                    retVal = this.ToModelInstance<Core.Model.Acts.Act>(dbActVersion, dbAct, context);
                    break;
            }

            retVal.LoadAssociations(context);
            return retVal;
        }

        /// <summary>
        /// Override cache conversion
        /// </summary>
        protected override Act CacheConvert(object dataInstance, DataContext context)
        {
            if (dataInstance == null) return null;
            DbActVersion dbActVersion = (dataInstance as CompositeResult)?.Values.OfType<DbActVersion>().FirstOrDefault() ?? dataInstance as DbActVersion ?? context.FirstOrDefault<DbActVersion>(o => o.VersionKey == (dataInstance as DbActSubTable).ParentKey);
            DbAct dbAct = (dataInstance as CompositeResult)?.Values.OfType<DbAct>().FirstOrDefault() ?? context.FirstOrDefault<DbAct>(o => o.Key == dbActVersion.Key);
            Act retVal = null;
            var cache = new AdoPersistenceCache(context);

            if (!dbActVersion.ObsoletionTime.HasValue)
                switch (dbAct.ClassConceptKey.ToString().ToLowerInvariant())
                {
                    case ActClassKeyStrings.ControlAct:
                        retVal = cache?.GetCacheItem<ControlAct>(dbAct.Key);
                        break;
                    case ActClassKeyStrings.SubstanceAdministration:
                        retVal = cache?.GetCacheItem<SubstanceAdministration>(dbAct.Key);
                        break;
                    case ActClassKeyStrings.Observation:
                        var dbObs = (dataInstance as CompositeResult)?.Values.OfType<DbObservation>().FirstOrDefault() ?? context.FirstOrDefault<DbObservation>(o => o.ParentKey == dbActVersion.VersionKey);
                        if (dbObs != null)
                            switch (dbObs.ValueType)
                            {
                                case "ST":
                                    retVal = cache?.GetCacheItem<TextObservation>(dbAct.Key);
                                    break;
                                case "CD":
                                    retVal = cache?.GetCacheItem<CodedObservation>(dbAct.Key);
                                    break;
                                case "PQ":
                                    retVal = cache?.GetCacheItem<QuantityObservation>(dbAct.Key);
                                    break;
                            }
                        break;
                    case ActClassKeyStrings.Encounter:
                        retVal = cache?.GetCacheItem<PatientEncounter>(dbAct.Key);
                        break;
                    case ActClassKeyStrings.Condition:
                    default:
                        retVal = cache?.GetCacheItem<Act>(dbAct.Key);
                        break;
                }

            // Return cache value
            if (retVal != null)
                return retVal;
            else
                return base.CacheConvert(dataInstance, context);
        }

        /// <summary>
        /// Insert the act into the database
        /// </summary>
        public Core.Model.Acts.Act InsertCoreProperties(DataContext context, Core.Model.Acts.Act data)
        {
            if (data.ClassConcept != null) data.ClassConcept = data.ClassConcept?.EnsureExists(context) as Concept;
            if (data.MoodConcept != null) data.MoodConcept = data.MoodConcept?.EnsureExists(context) as Concept;
            if (data.ReasonConcept != null) data.ReasonConcept = data.ReasonConcept?.EnsureExists(context) as Concept;
            if (data.StatusConcept != null) data.StatusConcept = data.StatusConcept?.EnsureExists(context) as Concept;
            if (data.TypeConcept != null) data.TypeConcept = data.TypeConcept?.EnsureExists(context) as Concept;
            if (data.Template != null) data.Template = data.Template?.EnsureExists(context) as TemplateDefinition;

            data.ClassConceptKey = data.ClassConcept?.Key ?? data.ClassConceptKey;
            data.MoodConceptKey = data.MoodConcept?.Key ?? data.MoodConceptKey;
            data.ReasonConceptKey = data.ReasonConcept?.Key ?? data.ReasonConceptKey;
            data.StatusConceptKey = data.StatusConcept?.Key ?? data.StatusConceptKey ?? StatusKeys.New;
            data.TypeConceptKey = data.TypeConcept?.Key ?? data.TypeConceptKey;

            // Do the insert
            var retVal = base.InsertInternal(context, data);

            if (data.Extensions != null && data.Extensions.Any())
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.ActExtension, DbActExtension>(
                   data.Extensions.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);

            if (data.Identifiers != null && data.Identifiers.Any())
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.ActIdentifier, DbActIdentifier>(
                   data.Identifiers.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);

            if (data.Notes != null && data.Notes.Any())
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.ActNote, DbActNote>(
                   data.Notes.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);

            if (data.Participations != null && data.Participations.Any())
            {
                data.Participations = data.Participations.Where(o => o != null && !o.IsEmpty()).Select(o => new ActParticipation(o.ParticipationRole?.EnsureExists(context)?.Key ?? o.ParticipationRoleKey, o.PlayerEntityKey) { Quantity = o.Quantity }).ToList();
                base.UpdateVersionedAssociatedItems<Core.Model.Acts.ActParticipation, DbActParticipation>(
                   data.Participations,
                    retVal,
                    context);
            }

            if (data.Relationships != null && data.Relationships.Any())
                base.UpdateVersionedAssociatedItems<Core.Model.Acts.ActRelationship, DbActRelationship>(
                   data.Relationships.Distinct(new ActRelationshipPersistenceService.Comparer()).Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);

            if (data.Tags != null && data.Tags.Any())
                base.UpdateAssociatedItems<Core.Model.DataTypes.ActTag, DbActTag>(
                   data.Tags.Where(o => o != null && !o.IsEmpty() && !o.TagKey.StartsWith("$")),
                    retVal,
                    context);

            if (data.Protocols != null && data.Protocols.Any())
                foreach (var p in data.Protocols)
                {
                    var proto = p.Protocol?.EnsureExists(context);

                    try
                    {
                        if (proto == null) // maybe we can retrieve the protocol from the protocol repository?
                        {
                            int t = 0;
                            proto = ApplicationServiceContext.Current.GetService<IClinicalProtocolRepositoryService>().FindProtocol(o => o.Key == p.ProtocolKey, 0, 1, out t).FirstOrDefault();
                            proto = proto.EnsureExists(context);
                        }

                    }
                    catch (Exception e)
                    {
                        // TODO: Add this to DI extension
                        this.m_tracer.TraceWarning("Could not find protocol {0} - ignoring", p.ProtocolKey);
                    }

                    if (proto != null)
                        context.Insert(new DbActProtocol()
                        {
                            SourceKey = retVal.Key.Value,
                            ProtocolKey = proto.Key.Value,
                            Sequence = p.Sequence
                        });
                }

            // Persist policies
            if (data.Policies != null && data.Policies.Any())
            {
                foreach (var p in data.Policies)
                {
                    var pol = p.Policy?.EnsureExists(context);
                    var polKey = pol?.Key ?? p.PolicyKey;
                    if (pol == null) // maybe we can retrieve it from the PIP?
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
                    context.Insert(new DbActSecurityPolicy()
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
        /// Update the specified data
        /// </summary>
        public Core.Model.Acts.Act UpdateCoreProperties(DataContext context, Core.Model.Acts.Act data)
        {
            if (data.ClassConcept != null) data.ClassConcept = data.ClassConcept?.EnsureExists(context) as Concept;
            if (data.MoodConcept != null) data.MoodConcept = data.MoodConcept?.EnsureExists(context) as Concept;
            if (data.ReasonConcept != null) data.ReasonConcept = data.ReasonConcept?.EnsureExists(context) as Concept;
            if (data.StatusConcept != null) data.StatusConcept = data.StatusConcept?.EnsureExists(context) as Concept;
            if (data.TypeConcept != null) data.TypeConcept = data.TypeConcept?.EnsureExists(context) as Concept;
            if (data.Template != null) data.Template = data.Template?.EnsureExists(context) as TemplateDefinition;

            data.ClassConceptKey = data.ClassConcept?.Key ?? data.ClassConceptKey;
            data.MoodConceptKey = data.MoodConcept?.Key ?? data.MoodConceptKey;
            data.ReasonConceptKey = data.ReasonConcept?.Key ?? data.ReasonConceptKey;
            data.StatusConceptKey = data.StatusConcept?.Key ?? data.StatusConceptKey ?? StatusKeys.New;

            // Do the update
            var retVal = base.UpdateInternal(context, data);

            if (data.Extensions != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.ActExtension, DbActExtension>(
                   data.Extensions.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);

            if (data.Identifiers != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.ActIdentifier, DbActIdentifier>(
                   data.Identifiers.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);

            if (data.Notes != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.ActNote, DbActNote>(
                   data.Notes.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context);

            if (data.Participations != null)
            {
                // Correct mixed keys
                if (this.m_settingsProvider.GetConfiguration().DataCorrectionKeys.Contains("edmonton-participation-keyfix"))
                {
                    // Obsolete all
                    foreach (var itm in context.Query<DbActParticipation>(o => o.SourceKey == retVal.Key && o.ObsoleteVersionSequenceId == null && o.ParticipationRoleKey == ActParticipationKey.Consumable))
                    {
                        itm.ObsoleteVersionSequenceId = retVal.VersionSequence;
                        context.Update(itm);
                    }
                    // Now we want to re-point to correct the issue
                    foreach (var itm in context.Query<DbActParticipation>(o => o.SourceKey == retVal.Key && o.ParticipationRoleKey == ActParticipationKey.Consumable && o.ObsoleteVersionSequenceId == retVal.VersionSequence))
                    {

                        var dItm = data.Participations.Find(o => o.Key == itm.Key);
                        if (dItm != null)
                            itm.TargetKey = dItm.PlayerEntityKey.Value;
                        itm.ObsoleteVersionSequenceId = null;
                        context.Update(itm);
                    }
                }

                // Update versioned association items
                base.UpdateVersionedAssociatedItems<Core.Model.Acts.ActParticipation, DbActParticipation>(
                      data.Participations.Where(o => o != null && !o.IsEmpty()),
                        retVal,
                        context);


            }

            if (data.Relationships != null)
                base.UpdateVersionedAssociatedItems<Core.Model.Acts.ActRelationship, DbActRelationship>(
                   data.Relationships.Distinct(new ActRelationshipPersistenceService.Comparer()).Where(o => o != null && !o.IsEmpty() && (o.SourceEntityKey == data.Key || !o.SourceEntityKey.HasValue)),
                    retVal,
                    context);

            if (data.Tags != null)
                base.UpdateAssociatedItems<Core.Model.DataTypes.ActTag, DbActTag>(
                   data.Tags.Where(o => o != null && !o.IsEmpty() && !o.TagKey.StartsWith("$")),
                    retVal,
                    context);

            return retVal;
        }

        /// <summary>
        /// Obsolete the act
        /// </summary>
        /// <param name="context"></param>
        public override Core.Model.Acts.Act ObsoleteInternal(DataContext context, Core.Model.Acts.Act data)
        {
            data.StatusConceptKey = StatusKeys.Obsolete;
            return base.UpdateInternal(context, data);
        }

        /// <summary>
        /// Perform insert
        /// </summary>
        public override Act InsertInternal(DataContext context, Act data)
        {
            switch (data.ClassConceptKey.ToString().ToLowerInvariant())
            {
                case ActClassKeyStrings.ControlAct:
                    return new ControlActPersistenceService(this.m_settingsProvider).InsertInternal(context, data.Convert<ControlAct>());
                case ActClassKeyStrings.SubstanceAdministration:
                    return new SubstanceAdministrationPersistenceService(this.m_settingsProvider).InsertInternal(context, data.Convert<SubstanceAdministration>());
                case ActClassKeyStrings.Observation:
                    switch (data.GetType().Name)
                    {
                        case "TextObservation":
                            return new TextObservationPersistenceService(this.m_settingsProvider).InsertInternal(context, data.Convert<TextObservation>());
                        case "CodedObservation":
                            return new CodedObservationPersistenceService(this.m_settingsProvider).InsertInternal(context, data.Convert<CodedObservation>());
                        case "QuantityObservation":
                            return new QuantityObservationPersistenceService(this.m_settingsProvider).InsertInternal(context, data.Convert<QuantityObservation>());
                        default:
                            return this.InsertCoreProperties(context, data);
                    }
                case ActClassKeyStrings.Encounter:
                    return new EncounterPersistenceService(this.m_settingsProvider).InsertInternal(context, data.Convert<PatientEncounter>());
                case ActClassKeyStrings.Condition:
                default:
                    return this.InsertCoreProperties(context, data);

            }
        }

        /// <summary>
        /// Perform update
        /// </summary>
        public override Act UpdateInternal(DataContext context, Act data)
        {
            switch (data.ClassConceptKey.ToString().ToLowerInvariant())
            {
                case ActClassKeyStrings.ControlAct:
                    return new ControlActPersistenceService(this.m_settingsProvider).UpdateInternal(context, data.Convert<ControlAct>());
                case ActClassKeyStrings.SubstanceAdministration:
                    return new SubstanceAdministrationPersistenceService(this.m_settingsProvider).UpdateInternal(context, data.Convert<SubstanceAdministration>());
                case ActClassKeyStrings.Observation:
                    switch (data.GetType().Name)
                    {
                        case "TextObservation":
                            return new TextObservationPersistenceService(this.m_settingsProvider).UpdateInternal(context, data.Convert<TextObservation>());
                        case "CodedObservation":
                            return new CodedObservationPersistenceService(this.m_settingsProvider).UpdateInternal(context, data.Convert<CodedObservation>());
                        case "QuantityObservation":
                            return new QuantityObservationPersistenceService(this.m_settingsProvider).UpdateInternal(context, data.Convert<QuantityObservation>());
                        default:
                            return this.UpdateCoreProperties(context, data);
                    }
                case ActClassKeyStrings.Encounter:
                    return new EncounterPersistenceService(this.m_settingsProvider).UpdateInternal(context, data.Convert<PatientEncounter>());
                case ActClassKeyStrings.Condition:
                default:
                    return this.UpdateCoreProperties(context, data);

            }
        }

        /// <summary>
        /// Perform bulk purge on expression
        /// </summary>
        /// <remarks>Since there are so many dependent tables this really calls QueryKeys and then BulkPurge</remarks>
        protected override void BulkPurgeInternal(DataContext connection, Expression<Func<Act, bool>> expression)
        {
            int offset = 0, totalResults = 1;
            while (offset < totalResults) {
                var k = this.QueryKeysInternal(connection, expression, offset, 1000, out totalResults).ToArray();
                this.BulkPurgeInternal(connection, k);
                offset += k.Length;
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
                var versionKeys = context.Query<DbActVersion>(o => batchKeys.Contains(o.Key)).Select(o => o.VersionKey).ToArray();

                // Delete versions of this act in sub tables
                context.Delete<DbTextObservation>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbQuantityObservation>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbCodedObservation>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbObservation>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbProcedure>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbSubstanceAdministration>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbControlAct>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbPatientEncounter>(o => versionKeys.Contains(o.ParentKey));

                // TODO: Other acts


                // Purge the related fields
                context.Delete<DbActIdentifier>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbActExtension>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbActTag>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbActNote>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbActProtocol>(o => batchKeys.Contains(o.SourceKey));

                // Note: Security tags are not deleted as they still apply even when this record is purged

                // TODO: Do we orphan sub-objects (delete the link) or do we clean those up to?
                context.Delete<DbActRelationship>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbActParticipation>(o => batchKeys.Contains(o.SourceKey));

                // Detach keys which are being deleted will need to be removed from the version heirarchy
                foreach (var rpl in context.Query<DbActVersion>(o => versionKeys.Contains(o.ReplacesVersionKey.Value)))
                {
                    rpl.ReplacesVersionKey = null;
                    rpl.ReplacesVersionKeySpecified = true;
                    context.Update(rpl);
                }

                // Purge the core entity data
                context.Delete<DbActVersion>(o => batchKeys.Contains(o.Key));


                // Create a version which indicates this is PURGED
                context.Insert(context.Query<DbAct>(o => batchKeys.Contains(o.Key))
                    .Select(o => o.Key)
                    .Distinct()
                    .ToArray()
                    .Select(o => new DbActVersion()
                    {
                        CreatedByKey = context.ContextId,
                        CreationTime = DateTimeOffset.Now,
                        Key = o,
                        StatusConceptKey = StatusKeys.Purged
                    }));

            }

            context.ResetSequence("ACT_VRSN_SEQ",
                context.Query<DbActVersion>(o => true).Max(o => o.VersionSequenceId));

        }

        /// <summary>
        /// Copy the specified ACT data 
        /// </summary>
        public override void Copy(Guid[] keysToCopy, DataContext fromContext, DataContext toContext)
        {
            // TODO:Clean this mess up
            // Purge the related fields
            int ofs = 0;
            IEnumerable<Guid> additionalKeys = fromContext.Query<DbExtensionType>(o => o.ObsoletionTime == null)
                .Select(o => o.CreatedByKey)
                .Distinct()
                .Union(
                    fromContext.Query<DbAssigningAuthority>(o => o.ObsoletionTime == null)
                    .Select(o => o.CreatedByKey)
                    .Distinct()
                )
                .Union(
                    fromContext.Query<DbProtocol>(o => o.ObsoletionTime == null)
                    .Select(o => o.CreatedByKey)
                    .Distinct()
                )
                .Union(
                    fromContext.Query<DbTemplateDefinition>(o => o.ObsoletionTime == null)
                    .Select(o => o.CreatedByKey)
                    .Distinct()
                ).Union(
                    fromContext.Query<DbProtocolHandler>(o => o.ObsoletionTime == null)
                    .Select(o => o.CreatedByKey)
                    .Distinct()
                ).Union(
                    fromContext.Query<DbConceptVersion>(o => o.Key != null)
                    .Select(o => o.CreatedByKey)
                    .Distinct()
                )
                .ToArray();

            toContext.InsertOrUpdate(fromContext.Query<DbSecurityProvenance>(o => additionalKeys.Contains(o.Key)));

            // copy all concepts that are referenced in the Act tabls
            additionalKeys = fromContext.Query<DbAct>(o => o.Key != null)
                .Select(o => o.MoodConceptKey)
                .Distinct()
                .Union(
                    fromContext.Query<DbAct>(o => o.Key != null)
                    .Select(o => o.ClassConceptKey)
                    .Distinct()
                ).Union(
                    fromContext.Query<DbActVersion>(o => o.Key != null)
                    .Select(o => o.StatusConceptKey)
                    .Distinct()
                ).Union(
                    fromContext.Query<DbActVersion>(o => o.Key != null)
                    .Select(o => o.TypeConceptKey)
                    .Distinct()
                ).Union(
                    fromContext.Query<DbPatientEncounter>(o => o.ParentKey != null)
                    .Select(o => o.DischargeDispositionKey)
                    .Distinct()
                ).Union(
                    fromContext.Query<DbSubstanceAdministration>(o => o.ParentKey != null)
                    .Select(o => o.RouteConceptKey)
                    .Distinct()
                ).Union(
                    fromContext.Query<DbSubstanceAdministration>(o => o.ParentKey != null)
                    .Select(o => o.DoseUnitConceptKey)
                    .Distinct()
                ).Union(
                    fromContext.Query<DbObservation>(o => o.ParentKey != null)
                    .Select(o => o.InterpretationConceptKey)
                    .Distinct()
                ).Union(
                    fromContext.Query<DbActRelationship>(o => o.Key != null)
                    .Select(o => o.RelationshipTypeKey)
                    .Distinct()
                ).Union(
                    fromContext.Query<DbActVersion>(o => o.Key != null)
                    .Select(o => o.ReasonConceptKey)
                    .Distinct()
                ).Union(
                    fromContext.Query<DbActParticipation>(o => o.Key != null)
                    .Select(o => o.ParticipationRoleKey)
                    .Distinct()
                ).Union(
                    fromContext.Query<DbQuantityObservation>(o => o.ParentKey != null)
                    .Select(o => o.UnitOfMeasureKey)
                    .Distinct()
                ).Union(
                    fromContext.Query<DbCodedObservation>(o => o.ParentKey != null)
                    .Select(o => o.Value)
                    .Distinct()
                ).Union(
                    fromContext.Query<DbSubstanceAdministration>(o => o.ParentKey != null)
                    .Select(o => o.SiteConceptKey)
                    .Distinct()
                ).Union(
                    fromContext.Query<DbProcedure>(o => o.ParentKey != null)
                    .Select(o => o.TargetSiteConceptKey)
                    .Distinct()
                    .ToArray()
                    .Where(o => o.HasValue)
                    .Select(o => o.Value)
                ).Union(
                    fromContext.Query<DbProcedure>(o => o.ParentKey != null)
                    .Select(o => o.MethodConceptKey)
                    .Distinct()
                    .ToArray()
                    .Where(o => o.HasValue)
                    .Select(o => o.Value)
                ).Union(
                    fromContext.Query<DbProcedure>(o => o.ParentKey != null)
                    .Select(o => o.ApproachSiteConceptKey)
                    .Distinct()
                    .ToArray()
                    .Where(o => o.HasValue)
                    .Select(o => o.Value)
                )
                .ToArray();
            toContext.InsertOrUpdate(fromContext.Query<DbConceptClass>(o => true));
            toContext.InsertOrUpdate(fromContext.Query<DbConcept>(o => additionalKeys.Contains(o.Key)));
            toContext.InsertOrUpdate(fromContext.Query<DbConceptVersion>(o => additionalKeys.Contains(o.Key)).OrderBy(o => o.VersionSequenceId));
            toContext.InsertOrUpdate(fromContext.Query<DbConceptSet>(o => true));
            toContext.InsertOrUpdate(fromContext.Query<DbConceptSetConceptAssociation>(o => additionalKeys.Contains(o.ConceptKey)));

            additionalKeys = fromContext.Query<DbAssigningAuthority>(o => o.ObsoletionTime == null)
               .Select(o => o.AssigningApplicationKey).Distinct().ToArray()
               .Where(o => o.HasValue)
               .Select(o => o.Value);

            toContext.InsertOrUpdate(fromContext.Query<DbSecurityApplication>(o => additionalKeys.Contains(o.Key)).ToArray().Select(o => new DbSecurityApplication()
            {
                Key = o.Key,
                CreatedByKey = o.CreatedByKey,
                CreationTime = o.CreationTime,
                InvalidAuthAttempts = 0,
                Lockout = o.Lockout,
                LastAuthentication = o.LastAuthentication,
                PublicId = o.PublicId,
                ObsoletionTime = o.ObsoletionTime,
                ObsoletedByKey = o.ObsoletedByKey,
                Secret = o.Secret ?? "XXXX",
                UpdatedByKey = o.UpdatedByKey,
                UpdatedTime = o.UpdatedTime
            }));

            toContext.InsertOrUpdate(fromContext.Query<DbProtocolHandler>(o => o.ObsoletionTime == null));
            toContext.InsertOrUpdate(fromContext.Query<DbProtocol>(o => o.ObsoletionTime == null));
            toContext.InsertOrUpdate(fromContext.Query<DbAssigningAuthority>(o => o.ObsoletionTime == null));
            toContext.InsertOrUpdate(fromContext.Query<DbExtensionType>(o => o.ObsoletionTime == null));
            toContext.InsertOrUpdate(fromContext.Query<DbTemplateDefinition>(o => o.ObsoletionTime == null));

            while (ofs < keysToCopy.Length)
            {
                var batchKeys = keysToCopy.Skip(ofs).Take(100).ToArray();
                ofs += 100;
                // Purge the related fields
                var versionKeys = fromContext.Query<DbActVersion>(o => batchKeys.Contains(o.Key)).Select(o => o.VersionKey).ToArray();

                // Copy users of interest
                additionalKeys = fromContext.Query<DbActVersion>(o => batchKeys.Contains(o.Key))
                    .Select(o => o.CreatedByKey)
                    .Distinct()
                    .Union(
                        fromContext.Query<DbActTag>(o => batchKeys.Contains(o.SourceKey))
                        .Select(o => o.CreatedByKey)
                        .Distinct()
                    )
                    .Union(
                        fromContext.Query<DbActVersion>(o => batchKeys.Contains(o.Key))
                        .Select(o => o.ObsoletedByKey)
                        .Distinct()
                        .ToArray()
                        .Where(o => o.HasValue)
                        .Select(o => o.Value)
                    )
                    .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbSecurityProvenance>(o => additionalKeys.Contains(o.Key)));

                toContext.InsertOrUpdate(fromContext.Query<DbAct>(o => batchKeys.Contains(o.Key)));
                toContext.InsertOrUpdate(fromContext.Query<DbActVersion>(o => batchKeys.Contains(o.Key)));

                // Other types of acts
                toContext.InsertOrUpdate(fromContext.Query<DbPatientEncounter>(o => versionKeys.Contains(o.ParentKey)));
                toContext.InsertOrUpdate(fromContext.Query<DbControlAct>(o => versionKeys.Contains(o.ParentKey)));
                toContext.InsertOrUpdate(fromContext.Query<DbConcept>(o => additionalKeys.Contains(o.Key)));
                toContext.InsertOrUpdate(fromContext.Query<DbSubstanceAdministration>(o => versionKeys.Contains(o.ParentKey)));

                toContext.InsertOrUpdate(fromContext.Query<DbProcedure>(o => versionKeys.Contains(o.ParentKey)));
                toContext.InsertOrUpdate(fromContext.Query<DbObservation>(o => versionKeys.Contains(o.ParentKey)));

                // Delete versions of this act in sub tables
                toContext.InsertOrUpdate(fromContext.Query<DbTextObservation>(o => versionKeys.Contains(o.ParentKey)));
                toContext.InsertOrUpdate(fromContext.Query<DbQuantityObservation>(o => versionKeys.Contains(o.ParentKey)));
                toContext.InsertOrUpdate(fromContext.Query<DbCodedObservation>(o => versionKeys.Contains(o.ParentKey)));

                // TODO: Other acts

                // Purge the related fields
                toContext.InsertOrUpdate(fromContext.Query<DbActIdentifier>(o => batchKeys.Contains(o.SourceKey) && o.ObsoleteVersionSequenceId == null));
                toContext.InsertOrUpdate(fromContext.Query<DbActExtension>(o => batchKeys.Contains(o.SourceKey) && o.ObsoleteVersionSequenceId == null));
                toContext.InsertOrUpdate(fromContext.Query<DbActTag>(o => batchKeys.Contains(o.SourceKey) && o.ObsoletionTime == null));
                toContext.InsertOrUpdate(fromContext.Query<DbActNote>(o => batchKeys.Contains(o.SourceKey) && o.ObsoleteVersionSequenceId == null));

                toContext.InsertOrUpdate(fromContext.Query<DbActProtocol>(o => batchKeys.Contains(o.SourceKey)));

                // Note: Security tags are not deleted as they still apply even when this record is purged

                additionalKeys = fromContext.Query<DbActRelationship>(o => batchKeys.Contains(o.SourceKey))
                    .Select(o => o.TargetKey)
                    .Distinct()
                    .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbAct>(o => additionalKeys.Contains(o.Key)));

                toContext.InsertOrUpdate(fromContext.Query<DbActRelationship>(o => batchKeys.Contains(o.SourceKey) && o.ObsoleteVersionSequenceId == null));

                additionalKeys = fromContext.Query<DbActParticipation>(o => batchKeys.Contains(o.SourceKey))
                   .Select(o => o.TargetKey)
                   .Distinct()
                   .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbEntity>(o => additionalKeys.Contains(o.Key)));
                toContext.InsertOrUpdate(fromContext.Query<DbActParticipation>(o => batchKeys.Contains(o.SourceKey) && o.ObsoleteVersionSequenceId == null));
            }

            toContext.ResetSequence("ACT_VRSN_SEQ", toContext.Query<DbActVersion>(o => true).Max(o => o.VersionSequenceId));
        }

    }
}
