using SanteDB.Core.BusinessRules;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Extensions;
using SanteDB.Core.i18n;
using SanteDB.Core.Interfaces;
using System.Reflection;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Attributes;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.DataType;
using SanteDB.Persistence.Data.Model.Entities;
using SanteDB.Persistence.Data.Model.Extensibility;
using SanteDB.Persistence.Data.Model.Roles;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{

    /// <summary>
    /// Entity derived persistence service which is responsible for persisting entities which have an intermediary table
    /// </summary>
    /// <remarks>This class is used for higher level entities where the entity is comprised of three sub-tables where 
    /// <typeparamref name="TDbTopLevelTable"/> links to <see cref="DbEntityVersion"/> via <typeparamref name="TDbEntitySubTable"/></remarks>
    /// <typeparam name="TEntity">The type of model entity this table handles</typeparam>
    /// <typeparam name="TDbEntitySubTable">The sub-table which points to <see cref="DbEntityVersion"/></typeparam>
    /// <typeparam name="TDbTopLevelTable">The top level table which <typeparamref name="TEntity"/> stores its data</typeparam>
    public abstract class EntityDerivedPersistenceService<TEntity, TDbTopLevelTable, TDbEntitySubTable> : EntityDerivedPersistenceService<TEntity, TDbEntitySubTable>
        where TEntity : Entity, IVersionedEntity, new()
        where TDbEntitySubTable : DbEntitySubTable, new()
        where TDbTopLevelTable : DbEntitySubTable, new()
    {


        /// <summary>
        /// DI constructor
        /// </summary>
        protected EntityDerivedPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {

        }

        /// <inheritdoc/>
        protected override void DoCopyVersionInternal(DataContext context, Guid previousVersionKey, Guid newVersionKey)
        {
            base.DoCopyVersionInternal(context, previousVersionKey, newVersionKey);
            var existingVersion = context.FirstOrDefault<TDbTopLevelTable>(o => o.ParentKey == previousVersionKey);
            if (existingVersion == null)
            {
                existingVersion = new TDbTopLevelTable();
            }
            existingVersion.ParentKey = newVersionKey;
            context.Insert(existingVersion);
        }

        /// <inheritdoc/>
        public override IOrmResultSet ExecuteQueryOrm(DataContext context, Expression<Func<TEntity, bool>> query)
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
            return base.DoQueryInternalAs<CompositeResult<DbEntityVersion, TDbEntitySubTable, TDbTopLevelTable>>(context, query, (o) =>
            {
                var columns = TableMapping.Get(typeof(TDbEntitySubTable)).Columns.Union(
                        TableMapping.Get(typeof(DbEntityVersion)).Columns, new ColumnMapping.ColumnComparer()
                        ).Union(TableMapping.Get(typeof(TDbTopLevelTable)).Columns, new ColumnMapping.ColumnComparer());
                var retVal = context.CreateSqlStatement().SelectFrom(typeof(DbEntityVersion), columns.ToArray())
                    .InnerJoin<DbEntityVersion, TDbEntitySubTable>(q => q.VersionKey, q => q.ParentKey)
                    .InnerJoin<TDbEntitySubTable, TDbTopLevelTable>(q => q.ParentKey, q => q.ParentKey);
                return retVal;
            });
        }

        /// <inheritdoc/>
        protected override TEntity DoInsertModel(DataContext context, TEntity data)
        {
            var retVal = base.DoInsertModel(context, data);
            var dbSubInstance = this.m_modelMapper.MapModelInstance<TEntity, TDbTopLevelTable>(data);
            dbSubInstance.ParentKey = retVal.VersionKey.Value;
            dbSubInstance = context.Insert(dbSubInstance);
            retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<TDbTopLevelTable, TEntity>(dbSubInstance), onlyNullFields: true);
            return retVal;
        }

        /// <inheritdoc/>
        protected override TEntity DoUpdateModel(DataContext context, TEntity data)
        {
            var retVal = base.DoUpdateModel(context, data);
            // Update sub entity table
            var dbSubEntity = this.m_modelMapper.MapModelInstance<TEntity, TDbTopLevelTable>(data);
            dbSubEntity.ParentKey = retVal.VersionKey.Value;
            if (this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.FullVersioning))
            {
                dbSubEntity = context.Insert(dbSubEntity);
            }
            else
            {
                dbSubEntity = context.Update(dbSubEntity);
            }
            retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<TDbTopLevelTable, TEntity>(dbSubEntity), onlyNullFields: true);
            return retVal;
        }
    }

    /// <summary>
    /// Entity derived persistence service with one sub entity table
    /// </summary>
    /// <typeparam name="TEntity">The model type of entity</typeparam>
    /// <typeparam name="TDbEntitySubTable">The sub table instance</typeparam>
    public abstract class EntityDerivedPersistenceService<TEntity, TDbEntitySubTable> : EntityDerivedPersistenceService<TEntity>
        where TEntity : Entity, IVersionedEntity, new()
        where TDbEntitySubTable : DbEntitySubTable, new()
    {


        /// <inheritdoc />
        protected override void DoCopyVersionInternal(DataContext context, Guid previousVersionKey, Guid newVersionKey)
        {
            var existingVersion = context.FirstOrDefault<TDbEntitySubTable>(o => o.ParentKey == previousVersionKey);
            if (existingVersion == null)
            {
                existingVersion = new TDbEntitySubTable();
            }
            existingVersion.ParentKey = newVersionKey;
            context.Insert(existingVersion);
        }

        /// <summary>
        /// Creates a dependency injected
        /// </summary>
        public EntityDerivedPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        public override IOrmResultSet ExecuteQueryOrm(DataContext context, Expression<Func<TEntity, bool>> query)
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
            return base.DoQueryInternalAs<CompositeResult<DbEntityVersion, TDbEntitySubTable>>(context, query, (o) =>
            {
                var columns = TableMapping.Get(typeof(TDbEntitySubTable)).Columns.Union(
                        TableMapping.Get(typeof(DbEntityVersion)).Columns, new ColumnMapping.ColumnComparer());
                var retVal = context.CreateSqlStatement().SelectFrom(typeof(DbEntityVersion), columns.ToArray())
                    .InnerJoin<DbEntityVersion, TDbEntitySubTable>(q => q.VersionKey, q => q.ParentKey);
                return retVal;
            });
        }

        /// <inheritdoc/>
        protected override TEntity DoInsertModel(DataContext context, TEntity data)
        {
            var retVal = base.DoInsertModel(context, data);
            var dbSubInstance = this.m_modelMapper.MapModelInstance<TEntity, TDbEntitySubTable>(data);
            dbSubInstance.ParentKey = retVal.VersionKey.Value;
            dbSubInstance = context.Insert(dbSubInstance);
            retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<TDbEntitySubTable, TEntity>(dbSubInstance), onlyNullFields: true);
            return retVal;
        }

        /// <inheritdoc/>
        protected override TEntity DoUpdateModel(DataContext context, TEntity data)
        {
            var retVal = base.DoUpdateModel(context, data);
            // Update sub entity table
            var dbSubEntity = this.m_modelMapper.MapModelInstance<TEntity, TDbEntitySubTable>(data);
            dbSubEntity.ParentKey = retVal.VersionKey.Value;
            if (this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.FullVersioning))
            {
                dbSubEntity = context.Insert(dbSubEntity);
            }
            else
            {
                dbSubEntity = context.Update(dbSubEntity);
            }
            retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<TDbEntitySubTable, TEntity>(dbSubEntity), onlyNullFields: true);
            return retVal;
        }
    }

    /// <summary>
    /// Persistence service that is responsible for storing and retrieving entities
    /// </summary>
    public abstract class EntityDerivedPersistenceService<TEntity> : VersionedDataPersistenceService<TEntity, DbEntityVersion, DbEntity>, IHasSubclassConversion
        where TEntity : Entity, IVersionedEntity, new()
    {

        // Class key map
        private readonly IDictionary<Guid, Type> m_classKeyMap;

        /// <summary>
        /// Try to resolve a persister by class concept key
        /// </summary>
        /// <param name="classKey">The class concept key to be resolved</param>
        /// <param name="persistenceService">The persistence service</param>
        /// <returns>True if the class key has a persister resolved</returns>
        protected bool TryGetSubclassPersister(Guid classKey, out IAdoPersistenceProvider persistenceService)
        {
            if (this.m_classKeyMap.TryGetValue(classKey, out Type modelType))
            {
                persistenceService = this.GetRelatedPersistenceService(modelType);
                return true;
            }
            else
            {
                persistenceService = null;
                return false;
            }
        }

        /// <summary>
        /// Creates a dependency injected
        /// </summary>
        public EntityDerivedPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
            m_classKeyMap = AppDomain.CurrentDomain.GetAllTypes()
               .Where(t => typeof(Entity).IsAssignableFrom(t))
               .SelectMany(t => t.GetCustomAttributes<ClassConceptKeyAttribute>().Select(c => new { classKey = Guid.Parse(c.ClassConcept), type = t }))
               .ToDictionary(k => k.classKey, k => k.type);
        }

        /// <summary>
        /// Perform a delete references
        /// </summary>
        protected override void DoDeleteReferencesInternal(DataContext context, Guid key)
        {
            context.Delete<DbEntityRelationship>(o => o.SourceKey == key);
            var addressIds = context.Query<DbEntityAddress>(o => o.SourceKey == key).Select(o => o.Key).ToArray();
            context.Delete<DbEntityAddressComponent>(o => addressIds.Contains(o.SourceKey));
            context.Delete<DbEntityAddress>(o => addressIds.Contains(o.Key));
            var nameIds = context.Query<DbEntityName>(o => o.SourceKey == key).Select(o => o.Key).ToArray();
            context.Delete<DbEntityNameComponent>(o => nameIds.Contains(o.SourceKey));
            context.Delete<DbEntityName>(o => nameIds.Contains(o.Key));
            context.Delete<DbEntityIdentifier>(o => o.SourceKey == key);
            context.Delete<DbEntityRelationship>(o => o.SourceKey == key);
            context.Delete<DbApplicationEntity>(o => o.ParentKey == key);
            context.Delete<DbEntityTag>(o => o.SourceKey == key);
            context.Delete<DbEntityExtension>(o => o.SourceKey == key);
            context.Delete<DbEntityNote>(o => o.SourceKey == key);
            context.Delete<DbTelecomAddress>(o => o.SourceKey == key);
            context.Delete<DbDeviceEntity>(o => o.ParentKey == key);
            context.Delete<DbPatient>(o => o.ParentKey == key);
            context.Delete<DbContainer>(o => o.ParentKey == key);
            context.Delete<DbProvider>(o => o.ParentKey == key);
            context.Delete<DbUserEntity>(o => o.ParentKey == key);
            context.Delete<DbNonPersonLivingSubject>(o => o.ParentKey == key);
            context.Delete<DbPerson>(o => o.ParentKey == key);
            context.Delete<DbOrganization>(o => o.ParentKey == key);
            context.Delete<DbPlaceService>(o => o.SourceKey == key);
            context.Delete<DbPlace>(o => o.ParentKey == key);

            base.DoDeleteReferencesInternal(context, key);
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

            // Geo-tagging
            data.GeoTagKey = this.EnsureExists(context, data.GeoTag)?.Key ?? data.GeoTagKey;

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
        /// Convert the sub-class data of the return value
        /// </summary>
        /// <param name="context">The context on which the data is being retrieved</param>
        /// <param name="dbModel">The model which was retrieved</param>
        /// <param name="modelData">The model data which has already be constructed</param>
        /// <param name="referenceObjects">The referenced objects</param>
        internal abstract TEntity DoConvertSubclassData(DataContext context, TEntity modelData, DbEntityVersion dbModel, params object[] referenceObjects);

        /// <summary>
        /// Convert the data model back to information model
        /// </summary>
        protected override TEntity DoConvertToInformationModel(DataContext context, DbEntityVersion dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch (DataPersistenceQueryContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.ClassConcept = this.GetRelatedPersistenceService<Concept>().Get(context, dbModel.ClassConceptKey);
                    retVal.SetLoaded(o => o.ClassConcept);
                    retVal.CreationAct = this.GetRelatedPersistenceService<Act>().Get(context, dbModel.CreationActKey.GetValueOrDefault());
                    retVal.SetLoaded(o => o.CreationAct);
                    retVal.DeterminerConcept = this.GetRelatedPersistenceService<Concept>().Get(context, dbModel.DeterminerConceptKey);
                    retVal.SetLoaded(o => o.DeterminerConcept);
                    retVal.StatusConcept = this.GetRelatedPersistenceService<Concept>().Get(context, dbModel.StatusConceptKey);
                    retVal.SetLoaded(o => o.StatusConcept);
                    retVal.TypeConcept = this.GetRelatedPersistenceService<Concept>().Get(context, dbModel.TypeConceptKey.GetValueOrDefault());
                    retVal.SetLoaded(o => o.TypeConcept);
                    goto case LoadMode.SyncLoad;
                case LoadMode.SyncLoad:
                    retVal.Addresses = this.GetRelatedPersistenceService<EntityAddress>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(o => o.Addresses);
                    retVal.Extensions = this.GetRelatedPersistenceService<EntityExtension>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(o => o.Extensions);
                    retVal.Identifiers = this.GetRelatedPersistenceService<EntityIdentifier>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(o => o.Identifiers);
                    retVal.Names = this.GetRelatedPersistenceService<EntityName>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(o => o.Names);
                    retVal.Notes = this.GetRelatedPersistenceService<EntityNote>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(o => o.Notes);
                    retVal.Relationships = this.GetRelatedPersistenceService<EntityRelationship>().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(o => o.Relationships);
                    retVal.Tags = this.GetRelatedPersistenceService<EntityTag>().Query(context, o => o.SourceEntityKey == dbModel.Key).ToList();
                    retVal.SetLoaded(o => o.Tags);
                    retVal.Telecoms = this.GetRelatedPersistenceService<EntityTelecomAddress>().Query(context, o => o.SourceEntityKey == dbModel.Key).ToList();
                    retVal.SetLoaded(o => o.Telecoms);

                    if (dbModel.GeoTagKey.HasValue)
                    {
                        var dbGeoTag = referenceObjects.OfType<DbGeoTag>().FirstOrDefault();
                        if (dbGeoTag == null)
                        {
                            this.m_tracer.TraceWarning("Using slow geo-tag reference of device");
                            dbGeoTag = context.FirstOrDefault<DbGeoTag>(o => o.Key == dbModel.GeoTagKey);
                        }
                        retVal.GeoTag = this.GetRelatedMappingProvider<GeoTag>().ToModelInstance(context, dbGeoTag);
                        retVal.SetLoaded(o => o.GeoTag);
                    }

                    goto case LoadMode.QuickLoad;
                case LoadMode.QuickLoad:
                    var query = context.CreateSqlStatement<DbEntitySecurityPolicy>().SelectFrom(typeof(DbEntitySecurityPolicy), typeof(DbSecurityPolicy))
                        .InnerJoin<DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
                        .Where(o => o.SourceKey == dbModel.Key);
                    retVal.Policies = context.Query<CompositeResult<DbEntitySecurityPolicy, DbSecurityPolicy>>(query)
                        .ToList()
                        .Select(o => new SecurityPolicyInstance(new SecurityPolicy(o.Object2.Name, o.Object2.Oid, o.Object2.IsPublic, o.Object2.CanOverride), PolicyGrantType.Grant))
                        .ToList();
                    retVal.SetLoaded(o => o.Policies);
                    break;
            }


            // switch the class concept
            // this code ensures that no matter where the entry point is into the persistence layer, that the proper return 
            // class is loaded based on the class code of the data being saved
            if (this.TryGetSubclassPersister(dbModel.ClassConceptKey, out var subClassProvider) && subClassProvider is IHasSubclassConversion edps)
            {
                return (TEntity)edps.Convert(context, retVal, dbModel, referenceObjects);
            }
            else
            {
                return retVal;
            }
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
                retVal.SetLoaded(o => o.Addresses);

            }

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

            if (data.Names != null)
            {
                retVal.Names = this.UpdateModelVersionedAssociations(context, retVal, data.Names).ToList();
                retVal.SetLoaded(o => o.Names);
            }

            if (data.Notes != null)
            {
                retVal.Notes = this.UpdateModelVersionedAssociations(context, retVal, data.Notes).ToList();
                retVal.SetLoaded(o => o.Notes);
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
                retVal.SetLoaded(o => o.Relationships);
            }

            if (data.Tags != null)
            {
                retVal.Tags = this.UpdateModelAssociations(context, retVal, data.Tags).ToList();
                retVal.SetLoaded(o => o.Tags);
            }

            if (data.Telecoms != null)
            {
                retVal.Telecoms = this.UpdateModelVersionedAssociations(context, retVal, data.Telecoms).ToList();
                retVal.SetLoaded(o => o.Telecoms);
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
                retVal.SetLoaded(o => o.Addresses);
            }

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

            if (data.Names != null)
            {
                retVal.Names = this.UpdateModelVersionedAssociations(context, retVal, data.Names).ToList();
                retVal.SetLoaded(o => o.Names);
            }

            if (data.Notes != null)
            {
                retVal.Notes = this.UpdateModelVersionedAssociations(context, retVal, data.Notes).ToList();
                retVal.SetLoaded(o => o.Notes);
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
                retVal.SetLoaded(o => o.Relationships);
            }

            if (data.Tags != null)
            {
                retVal.Tags = this.UpdateModelAssociations(context, retVal, data.Tags).ToList();
                retVal.SetLoaded(o => o.Tags);
            }

            if (data.Telecoms != null)
            {
                retVal.Telecoms = this.UpdateModelVersionedAssociations(context, retVal, data.Telecoms).ToList();
                retVal.SetLoaded(o => o.Telecoms);
            }

            return retVal;
        }

        /// <summary>
        /// Convert the object to the subclass
        /// </summary>
        public object Convert(DataContext context, object existingModel, object dbModel, params object[] referenceObjects)
        {
            // Convert
            var retVal = existingModel.Convert<TEntity>();
            return this.DoConvertSubclassData(context, retVal, (DbEntityVersion)dbModel, referenceObjects);
        }
    }
}