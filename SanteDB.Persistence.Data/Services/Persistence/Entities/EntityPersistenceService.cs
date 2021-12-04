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
        /// Insert the model using the proper type of persistence
        /// </summary>
        /// <param name="context">The context where the data should be inserted</param>
        /// <param name="data">The data to be inserted</param>
        /// <returns>The inserted entity</returns>
        protected override Entity DoInsertModel(DataContext context, Entity data)
        {
            switch (data)
            {
                case Place place:
                    return this.GetRelatedPersistenceService<Place>().Insert(context, place);

                case Organization organization:
                    return this.GetRelatedPersistenceService<Organization>().Insert(context, organization);

                case Patient patient:
                    return this.GetRelatedPersistenceService<Patient>().Insert(context, patient);

                case Provider provider:
                    return this.GetRelatedPersistenceService<Provider>().Insert(context, provider);

                case UserEntity userEntity:
                    return this.GetRelatedPersistenceService<UserEntity>().Insert(context, userEntity);

                case DeviceEntity deviceEntity:
                    return this.GetRelatedPersistenceService<DeviceEntity>().Insert(context, deviceEntity);

                case ApplicationEntity applicationEntity:
                    return this.GetRelatedPersistenceService<ApplicationEntity>().Insert(context, applicationEntity);

                case Person person:
                    return this.GetRelatedPersistenceService<Person>().Insert(context, person);

                case ManufacturedMaterial manufacturedMaterial:
                    return this.GetRelatedPersistenceService<ManufacturedMaterial>().Insert(context, manufacturedMaterial);

                case Material material:
                    return this.GetRelatedPersistenceService<Material>().Insert(context, material);

                default:
                    return base.DoInsertModel(context, data);
            }
        }

        /// <summary>
        /// update the model using the proper type of persistence
        /// </summary>
        /// <param name="context">The context where the data should be updated</param>
        /// <param name="data">The data to be updated</param>
        /// <returns>The updated entity</returns>
        protected override Entity DoUpdateModel(DataContext context, Entity data)
        {
            switch (data)
            {
                case Place place:
                    return this.GetRelatedPersistenceService<Place>().Update(context, place);

                case Organization organization:
                    return this.GetRelatedPersistenceService<Organization>().Update(context, organization);

                case Patient patient:
                    return this.GetRelatedPersistenceService<Patient>().Update(context, patient);

                case Provider provider:
                    return this.GetRelatedPersistenceService<Provider>().Update(context, provider);

                case UserEntity userEntity:
                    return this.GetRelatedPersistenceService<UserEntity>().Update(context, userEntity);

                case DeviceEntity deviceEntity:
                    return this.GetRelatedPersistenceService<DeviceEntity>().Update(context, deviceEntity);

                case ApplicationEntity applicationEntity:
                    return this.GetRelatedPersistenceService<ApplicationEntity>().Update(context, applicationEntity);

                case Person person:
                    return this.GetRelatedPersistenceService<Person>().Update(context, person);

                case ManufacturedMaterial manufacturedMaterial:
                    return this.GetRelatedPersistenceService<ManufacturedMaterial>().Update(context, manufacturedMaterial);

                case Material material:
                    return this.GetRelatedPersistenceService<Material>().Update(context, material);

                default:
                    return base.DoUpdateModel(context, data);
            }
        }

        /// <summary>
        /// Convert data to information model
        /// </summary>
        protected override Entity DoConvertToInformationModel(DataContext context, DbEntityVersion dbModel, params Object[] referenceObjects)
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

                case EntityClassKeyStrings.NonLivingSubject:
                    return this.GetRelatedMappingProvider<ApplicationEntity>().ToModelInstance(context, dbModel);

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