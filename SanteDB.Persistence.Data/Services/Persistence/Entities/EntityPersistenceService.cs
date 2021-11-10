using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Entities;
using System;
using System.Collections.Generic;
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