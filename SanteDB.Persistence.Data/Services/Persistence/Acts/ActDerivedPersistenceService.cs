using SanteDB.Core.BusinessRules;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Extensions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Attributes;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Acts;
using SanteDB.Persistence.Data.Model.DataType;
using SanteDB.Persistence.Data.Model.Extensibility;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using SanteDB.Persistence.Data.Model;
using System.Linq.Expressions;
using SanteDB.Core.i18n;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{

    /// <summary>
    /// Generic persistence service interface which can be used for calling other act derived persistence functions
    /// </summary>
    internal interface IActDerivedPersistenceService
    {
        /// <summary>
        /// Copy sub-version information for the specified type of data
        /// </summary>
        /// <param name="context">The context on which the data should be copied</param>
        /// <param name="newVersion">The new version to copy data into</param>
        void DoCopyVersionSubTable(DataContext context, DbActVersion newVersion);
    }

    /// <summary>
    /// An act derived persistence service where the act has a sub-table storing child data
    /// </summary>
    /// <typeparam name="TAct">The type of act being persisted</typeparam>
    /// <typeparam name="TDbActSubTable">The database table which stores additional information for the type of data</typeparam>
    public abstract class ActDerivedPersistenceService<TAct, TDbActSubTable> : ActDerivedPersistenceService<TAct>
        where TAct : Act, IVersionedEntity, new()
        where TDbActSubTable : DbActSubTable, new()
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        protected ActDerivedPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc />
        protected override void DoCopyVersionSubTableInternal(DataContext context, DbActVersion newVersion)
        {
            var existingVersion = context.FirstOrDefault<TDbActSubTable>(o => o.ParentKey == newVersion.ReplacesVersionKey);
            if (existingVersion == null)
            {
                existingVersion = new TDbActSubTable();
            }
            existingVersion.ParentKey = newVersion.VersionKey;
            context.Insert(existingVersion);
        }


        /// <inheritdoc/>
        public override IOrmResultSet ExecuteQueryOrm(DataContext context, Expression<Func<TAct, bool>> query)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (query == null)
            {
                throw new ArgumentNullException(nameof(query), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            // Perform sub query
            return base.DoQueryInternalAs<CompositeResult<DbActVersion, TDbActSubTable>>(context, query, (o) =>
            {
                var columns = TableMapping.Get(typeof(TDbActSubTable)).Columns.Union(
                        TableMapping.Get(typeof(DbActVersion)).Columns, new ColumnMapping.ColumnComparer());
                var retVal = context.CreateSqlStatement().SelectFrom(typeof(DbActVersion), columns.ToArray())
                    .InnerJoin<DbActVersion, TDbActSubTable>(q => q.VersionKey, q => q.ParentKey);
                return retVal;
            });
        }

        /// <inheritdoc/>
        protected override TAct DoInsertModel(DataContext context, TAct data)
        {
            var retVal = base.DoInsertModel(context, data);
            var dbSubInstance = this.m_modelMapper.MapModelInstance<TAct, TDbActSubTable>(data);
            dbSubInstance.ParentKey = retVal.VersionKey.Value;
            dbSubInstance = context.Insert(dbSubInstance);
            retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<TDbActSubTable, TAct>(dbSubInstance), onlyNullFields: true);
            return retVal;
        }

        /// <inheritdoc/>
        protected override TAct DoUpdateModel(DataContext context, TAct data)
        {
            var retVal = base.DoUpdateModel(context, data);
            // Update sub table
            var dbSubEntity = this.m_modelMapper.MapModelInstance<TAct, TDbActSubTable>(data);
            dbSubEntity.ParentKey = retVal.VersionKey.Value;
            if (this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.FullVersioning))
            {
                dbSubEntity = context.Insert(dbSubEntity);
            }
            else
            {
                dbSubEntity = context.Update(dbSubEntity);
            }
            retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<TDbActSubTable, TAct>(dbSubEntity), onlyNullFields: true);
            return retVal;
        }
    }

    /// <summary>
    /// Persistence service that is responsible for the storing and retrieving of acts
    /// </summary>
    /// <typeparam name="TAct">The model type of act</typeparam>
    public abstract class ActDerivedPersistenceService<TAct> : VersionedDataPersistenceService<TAct, DbActVersion, DbAct>, IAdoClassMapper, IActDerivedPersistenceService
        where TAct : Act, IVersionedEntity, new()
    {

        // Class key map
        private readonly IDictionary<Guid, Type> m_classKeyMap;

        /// <inheritdoc/>
        void IActDerivedPersistenceService.DoCopyVersionSubTable(DataContext context, DbActVersion newVersion) => this.DoCopyVersionSubTableInternal(context, newVersion);

        /// <summary>
        /// Creates a dependency injected persistence service
        /// </summary>
        protected ActDerivedPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {

            var classAttributes = AppDomain.CurrentDomain.GetAllTypes()
                .Where(t => typeof(Act).IsAssignableFrom(t))
                .SelectMany(t => t.GetCustomAttributes<ClassConceptKeyAttribute>(false).Select(c => new { classKey = Guid.Parse(c.ClassConcept), type = t }))
                .ToArray();

            this.m_classKeyMap = new Dictionary<Guid, Type>();
            foreach (var ca in classAttributes)
            {
                if (this.m_classKeyMap.ContainsKey(ca.classKey))
                {
                    throw new InvalidOperationException(String.Format(ErrorMessages.DUPLICATE_CLASS_CONCEPT, ca.classKey, ca.type));
                }
                else
                {
                    this.m_classKeyMap.Add(ca.classKey, ca.type);
                }
            }

        }

        /// <summary>
        /// Attempts to load the persistence provider for a subclass described by <paramref name="classKey"/>
        /// </summary>
        /// <param name="classKey">The classification key to fetch the subclass persistence service of</param>
        /// <param name="persistenceProvider">The located persistence provider</param>
        /// <returns>True if the persistence provider was located</returns>
        protected bool TryGetSubclassPersister(Guid classKey, out IAdoPersistenceProvider persistenceProvider)
        {
            if (this.m_classKeyMap.TryGetValue(classKey, out Type modelType))
            {
                persistenceProvider = modelType.GetRelatedPersistenceService();
                return true;
            }
            else
            {
                persistenceProvider = null;
                return false;
            }

        }

        /// <summary>
        /// Delete all references to <paramref name="key"/>
        /// </summary>
        protected override void DoDeleteReferencesInternal(DataContext context, Guid key)
        {
            context.Delete<DbActParticipation>(o => o.SourceKey == key);
            context.Delete<DbActProtocol>(o => o.SourceKey == key);
            context.Delete<DbActRelationship>(o => o.SourceKey == key);
            context.Delete<DbActExtension>(o => o.SourceKey == key);
            context.Delete<DbActIdentifier>(o => o.SourceKey == key);
            context.Delete<DbActNote>(o => o.SourceKey == key);
            context.Delete<DbActTag>(o => o.SourceKey == key);

            // Delete act data
            context.Delete<DbControlAct>(o => o.ParentKey == key);
            context.Delete<DbCodedObservation>(o => o.ParentKey == key);
            context.Delete<DbTextObservation>(o => o.ParentKey == key);
            context.Delete<DbQuantityObservation>(o => o.ParentKey == key);
            context.Delete<DbObservation>(o => o.ParentKey == key);
            context.Delete<DbPatientEncounter>(o => o.ParentKey == key);
            context.Delete<DbProcedure>(o => o.ParentKey == key);
            context.Delete<DbSubstanceAdministration>(o => o.ParentKey == key);

            base.DoDeleteReferencesInternal(context, key);
        }

        /// <inheritdoc/>
        protected override TAct BeforePersisting(DataContext context, TAct data)
        {
            if (!data.StatusConceptKey.HasValue)
            {
                data.StatusConceptKey = StatusKeys.New;
            }

            data.ClassConceptKey = this.EnsureExists(context, data.ClassConcept)?.Key ?? data.ClassConceptKey;
            data.MoodConceptKey = this.EnsureExists(context, data.MoodConcept)?.Key ?? data.MoodConceptKey;
            data.StatusConceptKey = this.EnsureExists(context, data.StatusConcept)?.Key ?? data.StatusConceptKey;
            data.TemplateKey = this.EnsureExists(context, data.Template)?.Key ?? data.TemplateKey;
            data.TypeConceptKey = this.EnsureExists(context, data.TypeConcept)?.Key ?? data.TypeConceptKey;
            data.ReasonConceptKey = this.EnsureExists(context, data.ReasonConcept)?.Key ?? data.ReasonConceptKey;

            // Geo-tagging
            data.GeoTagKey = this.EnsureExists(context, data.GeoTag)?.Key ?? data.GeoTagKey;

            // Verify the act
            var issues = this.VerifyEntity(context, data).ToArray();
            if (issues.Any(i => i.Priority == Core.BusinessRules.DetectedIssuePriorityType.Error))
            {
                throw new DetectedIssueException(issues);
            }
            else if (issues.Any()) // there are non-serious issues
            {
                if (data.Extensions == null)
                {
                    if (data.Key.HasValue)
                    { // load from DB because there may be some existing stuff we don't want to erase
                        data.Extensions = data.Extensions.GetRelatedPersistenceService().Query(context, o => o.SourceEntityKey == data.Key).ToList();
                    }
                    else
                    {
                        data.Extensions = new List<ActExtension>();
                    }
                }

                var extension = data.Extensions.FirstOrDefault(o => o.ExtensionTypeKey == ExtensionTypeKeys.DataQualityExtension);
                if (extension == null)
                {
                    data.Extensions.Add(new ActExtension(ExtensionTypeKeys.DataQualityExtension, typeof(DictionaryExtensionHandler), issues));
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
        /// Convert to appropriate sub-class model 
        /// </summary>
        protected virtual TAct DoConvertToInformationModelEx(DataContext context, DbActVersion dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            var conceptPersistence = typeof(Concept).GetRelatedPersistenceService() as IAdoPersistenceProvider<Concept>;

            switch (DataPersistenceQueryContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.ClassConcept = conceptPersistence.Get(context, dbModel.ClassConceptKey);
                    retVal.SetLoaded(o => o.ClassConcept);
                    retVal.MoodConcept = conceptPersistence.Get(context, dbModel.MoodConceptKey);
                    retVal.SetLoaded(o => o.MoodConcept);
                    retVal.StatusConcept = conceptPersistence.Get(context, dbModel.StatusConceptKey);
                    retVal.SetLoaded(o => o.StatusConcept);
                    retVal.TypeConcept = conceptPersistence.Get(context, dbModel.TypeConceptKey);
                    retVal.SetLoaded(o => o.TypeConcept);
                    retVal.Template = retVal.Template.GetRelatedPersistenceService().Get(context, dbModel.TemplateKey.GetValueOrDefault());
                    retVal.SetLoaded(o => o.Template);
                    retVal.ReasonConcept = conceptPersistence.Get(context, dbModel.ReasonConceptKey.GetValueOrDefault());
                    retVal.SetLoaded(o => o.ReasonConcept);
                    goto case LoadMode.SyncLoad;
                case LoadMode.SyncLoad:
                    retVal.Extensions = retVal.Extensions.GetRelatedPersistenceService().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(o => o.Extensions);
                    retVal.Identifiers = retVal.Identifiers.GetRelatedPersistenceService().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(o => o.Identifiers);
                    retVal.Notes = retVal.Notes.GetRelatedPersistenceService().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(o => o.Notes);
                    retVal.Relationships = retVal.Relationships.GetRelatedPersistenceService().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(o => o.Relationships);
                    retVal.Tags = retVal.Tags.GetRelatedPersistenceService().Query(context, o => o.SourceEntityKey == dbModel.Key).ToList();
                    retVal.SetLoaded(o => o.Tags);
                    retVal.Participations = retVal.Participations.GetRelatedPersistenceService().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(o => o.Participations);
                    retVal.Protocols = retVal.Protocols.GetRelatedPersistenceService().Query(context, o => o.SourceEntityKey == dbModel.Key).ToList();
                    retVal.SetLoaded(o => o.Protocols);
                    if (dbModel.GeoTagKey.HasValue)
                    {
                        var dbGeoTag = referenceObjects.OfType<DbGeoTag>().FirstOrDefault();
                        if (dbGeoTag == null)
                        {
                            this.m_tracer.TraceWarning("Using slow geo-tag reference of device");
                            dbGeoTag = context.FirstOrDefault<DbGeoTag>(o => o.Key == dbModel.GeoTagKey);
                        }
                        retVal.GeoTag = retVal.GeoTag.GetRelatedMappingProvider().ToModelInstance(context, dbGeoTag);
                        retVal.SetLoaded(o => o.GeoTag);
                    }

                    goto case LoadMode.QuickLoad;
                case LoadMode.QuickLoad:
                    var query = context.CreateSqlStatement<DbActSecurityPolicy>().SelectFrom(typeof(DbActSecurityPolicy), typeof(DbSecurityPolicy))
                       .InnerJoin<DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
                       .Where(o => o.SourceKey == dbModel.Key);
                    retVal.Policies = context.Query<CompositeResult<DbActSecurityPolicy, DbSecurityPolicy>>(query)
                        .ToList()
                        .Select(o => new SecurityPolicyInstance(new SecurityPolicy(o.Object2.Name, o.Object2.Oid, o.Object2.IsPublic, o.Object2.CanOverride), PolicyGrantType.Grant))
                        .ToList();
                    retVal.SetLoaded(o => o.Policies);
                    break;
            }

            return retVal;
        }

        /// <inheritdoc/>
        protected override TAct DoConvertToInformationModel(DataContext context, DbActVersion dbModel, params object[] referenceObjects)
        {

            // Get subclass persister
            if (this.TryGetSubclassPersister(dbModel.ClassConceptKey, out var persistenceProvider) && persistenceProvider is IAdoClassMapper edps)
            {
                return (TAct)edps.MapToModelInstanceEx(context, dbModel, referenceObjects);
            }
            else
            {
                return this.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            }
        }

        /// <summary>
        /// Perform an insert of the core related objects (in this case for an act)
        /// </summary>
        /// <param name="context">The context on which the data should be inserted</param>
        /// <param name="data">The data for the object</param>
        /// <returns>The inserted act</returns>
        protected override TAct DoInsertModel(DataContext context, TAct data)
        {
            var retVal = base.DoInsertModel(context, data);

            if (data.Extensions != null)
            {
                retVal.Extensions = this.UpdateModelVersionedAssociations(context, retVal, data.Extensions).ToList();
                retVal.SetLoaded(o => o.Extensions);

            }

            if (data.Identifiers != null)
            {
                retVal.Identifiers = this.UpdateModelVersionedAssociations(context, retVal, data.Identifiers).ToList();
                retVal.SetLoaded(o => o.Identifiers);
            }

            if (data.Notes != null)
            {
                retVal.Notes = this.UpdateModelVersionedAssociations(context, retVal, data.Notes).ToList();
                retVal.SetLoaded(o => o.Notes);
            }

            if (data.Policies != null)
            {
                retVal.Policies = this.UpdateInternalAssociations(context, retVal.Key.Value, data.Policies.Select(o => new DbActSecurityPolicy()
                {
                    PolicyKey = o.PolicyKey.Value
                }), o => o.SourceKey == retVal.Key && !o.ObsoleteVersionSequenceId.HasValue).Select(o => o.ToSecurityPolicyInstance(context)).ToList();
            }

            if (data.Relationships != null)
            {
                retVal.Relationships = this.UpdateModelVersionedAssociations(context, retVal, data.Relationships).ToList();
                retVal.SetLoaded(o => o.Relationships);
            }

            if (data.Tags != null)
            {
                retVal.Tags = this.UpdateModelAssociations(context, retVal, data.Tags).ToList();
                retVal.SetLoaded(o => o.Tags);
            }

            if (data.Participations != null)
            {
                retVal.Participations = this.UpdateModelVersionedAssociations(context, retVal, data.Participations).ToList();
                retVal.SetLoaded(o => o.Participations);
            }

            if (data.Protocols != null)
            {
                // This is a special case since the dbactprotocol <> acts are not specifically identified (they are combination)
                retVal.Protocols = data.Protocols.Select(p =>
                {
                    p.SourceEntityKey = retVal.Key;
                    return typeof(ActProtocol).GetRelatedPersistenceService().Insert(context, p) as ActProtocol;
                }).ToList();
                retVal.SetLoaded(o => o.Protocols);
            }

            return retVal;
        }

        /// <summary>
        /// Perform an update of the core related objects (in this case for an act)
        /// </summary>
        /// <param name="context">The context on which the data should be updated</param>
        /// <param name="data">The data for the object</param>
        /// <returns>The updated act</returns>
        protected override TAct DoUpdateModel(DataContext context, TAct data)
        {
            var retVal = base.DoUpdateModel(context, data);

            if (data.Extensions != null)
            {
                retVal.Extensions = this.UpdateModelVersionedAssociations(context, retVal, data.Extensions).ToList();
                retVal.SetLoaded(o => o.Extensions);

            }

            if (data.Identifiers != null)
            {
                retVal.Identifiers = this.UpdateModelVersionedAssociations(context, retVal, data.Identifiers).ToList();
                retVal.SetLoaded(o => o.Identifiers);
            }

            if (data.Notes != null)
            {
                retVal.Notes = this.UpdateModelVersionedAssociations(context, retVal, data.Notes).ToList();
                retVal.SetLoaded(o => o.Notes);
            }

            if (data.Policies != null)
            {
                retVal.Policies = this.UpdateInternalAssociations(context, retVal.Key.Value, data.Policies.Select(o => new DbActSecurityPolicy()
                {
                    PolicyKey = o.PolicyKey.Value
                }), o => o.SourceKey == retVal.Key && !o.ObsoleteVersionSequenceId.HasValue).Select(o => o.ToSecurityPolicyInstance(context)).ToList();
            }

            if (data.Relationships != null)
            {
                retVal.Relationships = this.UpdateModelVersionedAssociations(context, retVal, data.Relationships).ToList();
                retVal.SetLoaded(o => o.Relationships);
            }

            if (data.Tags != null)
            {
                retVal.Tags = this.UpdateModelAssociations(context, retVal, data.Tags).ToList();
                retVal.SetLoaded(o => o.Tags);
            }

            if (data.Participations != null)
            {
                retVal.Participations = this.UpdateModelVersionedAssociations(context, retVal, data.Participations).ToList();
                retVal.SetLoaded(o => o.Participations);
            }

            if (data.Protocols != null)
            {
                // This is a special case since the dbactprotocol <> acts are not specifically identified (they are combination)
                context.Delete<DbActProtocol>(o => o.SourceKey == retVal.Key);
                retVal.Protocols = data.Protocols.Select(p =>
                {
                    p.SourceEntityKey = retVal.Key;
                    return typeof(ActProtocol).GetRelatedPersistenceService().Insert(context, p) as ActProtocol;
                }).ToList();
                retVal.SetLoaded(o => o.Protocols);
            }

            return retVal;
        }

        /// <summary>
        /// Map to model instance
        /// </summary>
        object IAdoClassMapper.MapToModelInstanceEx(DataContext context, object dbModel, params object[] referenceObjects) => this.DoConvertToInformationModelEx(context, (DbActVersion)dbModel, referenceObjects);

    }
}
