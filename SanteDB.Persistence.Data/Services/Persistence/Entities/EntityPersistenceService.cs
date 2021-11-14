using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.DataType;
using SanteDB.Persistence.Data.Model.Entities;
using SanteDB.Persistence.Data.Model.Extensibility;
using SanteDB.Persistence.Data.Model.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// An persistence service that handles <see cref="Entity"/>
    /// </summary>
    public class EntityPersistenceService : EntityDerivedPersistenceService<Entity>
    {
        /// <summary>
        /// Entity persistence service
        /// </summary>
        public EntityPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
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
            context.Delete<DbProvider>(o => o.ParentKey == key);
            context.Delete<DbUserEntity>(o => o.ParentKey == key);
            context.Delete<DbPerson>(o => o.ParentKey == key);
            context.Delete<DbOrganization>(o => o.ParentKey == key);
            context.Delete<DbPlaceService>(o => o.SourceKey == key);
            context.Delete<DbPlace>(o => o.ParentKey == key);

            base.DoDeleteReferencesInternal(context, key);
        }

        /// <summary>
        /// Convert data to information model
        /// </summary>
        protected override Entity DoConvertToInformationModel(DataContext context, DbEntityVersion dbModel, params IDbIdentified[] referenceObjects)
        {
            switch (dbModel.ClassConceptKey.ToString().ToLowerInvariant())
            {
                case EntityClassKeyStrings.Place:
                case EntityClassKeyStrings.CityOrTown:
                case EntityClassKeyStrings.Country:
                case EntityClassKeyStrings.CountyOrParish:
                case EntityClassKeyStrings.PrecinctOrBorough:
                case EntityClassKeyStrings.State:
                case EntityClassKeyStrings.ServiceDeliveryLocation:
                    return this.GetRelatedMappingProvider<Place>().ToModelInstance(context, dbModel);

                case EntityClassKeyStrings.Container:
                    return this.GetRelatedMappingProvider<Container>().ToModelInstance(context, dbModel);

                case EntityClassKeyStrings.Device:
                    return this.GetRelatedMappingProvider<DeviceEntity>().ToModelInstance(context, dbModel);

                case EntityClassKeyStrings.ManufacturedMaterial:
                    return this.GetRelatedMappingProvider<ManufacturedMaterial>().ToModelInstance(context, dbModel);

                case EntityClassKeyStrings.Material:
                    return this.GetRelatedMappingProvider<Material>().ToModelInstance(context, dbModel);

                case EntityClassKeyStrings.Organization:
                    return this.GetRelatedMappingProvider<Organization>().ToModelInstance(context, dbModel);

                case EntityClassKeyStrings.Patient:
                    return this.GetRelatedMappingProvider<Patient>().ToModelInstance(context, dbModel);

                case EntityClassKeyStrings.Person:
                    return this.GetRelatedMappingProvider<Person>().ToModelInstance(context, dbModel);

                case EntityClassKeyStrings.Provider:
                    return this.GetRelatedMappingProvider<Provider>().ToModelInstance(context, dbModel);

                case EntityClassKeyStrings.UserEntity:
                    return this.GetRelatedMappingProvider<UserEntity>().ToModelInstance(context, dbModel);

                default:
                    return base.DoConvertToInformationModel(context, dbModel, null);
            }
        }
    }
}