/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-9-7
 */
using SanteDB.Core.Model;
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

        /// <inheritdoc/>
        protected override void DoCopyVersionSubTableInternal(DataContext context, DbEntityVersion newVersion)
        {
            if(this.TryGetSubclassPersister(newVersion.ClassConceptKey, out var persistenceService) && persistenceService is IEntityDerivedPersistenceService edps)
            {
                edps.DoCopyVersionSubTable(context, newVersion);
            }
        }

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
                case NonPersonLivingSubject npls:
                    return npls.GetRelatedPersistenceService().Insert(context, npls);
                case Place place:
                    return place.GetRelatedPersistenceService().Insert(context, place);
                case Organization organization:
                    return organization.GetRelatedPersistenceService().Insert(context, organization);
                case Patient patient:
                    return patient.GetRelatedPersistenceService().Insert(context, patient);
                case Provider provider:
                    return provider.GetRelatedPersistenceService().Insert(context, provider);
                case UserEntity userEntity:
                    return userEntity.GetRelatedPersistenceService().Insert(context, userEntity);
                case DeviceEntity deviceEntity:
                    return deviceEntity.GetRelatedPersistenceService().Insert(context, deviceEntity);
                case ApplicationEntity applicationEntity:
                    return applicationEntity.GetRelatedPersistenceService().Insert(context, applicationEntity);
                case Person person:
                    return person.GetRelatedPersistenceService().Insert(context, person);
                case ManufacturedMaterial manufacturedMaterial:
                    return manufacturedMaterial.GetRelatedPersistenceService().Insert(context, manufacturedMaterial);
                case Material material:
                    return material.GetRelatedPersistenceService().Insert(context, material);
                default:
                    if (this.TryGetSubclassPersister(data.ClassConceptKey.GetValueOrDefault(), out var service))
                    {
                        return (Entity)service.Insert(context, data);
                    }
                    else
                    {
                        return base.DoInsertModel(context, data);
                    }
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
                case NonPersonLivingSubject npls:
                    return npls.GetRelatedPersistenceService().Update(context, npls);
                case Place place:
                    return place.GetRelatedPersistenceService().Update(context, place);
                case Organization organization:
                    return organization.GetRelatedPersistenceService().Update(context, organization);
                case Patient patient:
                    return patient.GetRelatedPersistenceService().Update(context, patient);
                case Provider provider:
                    return provider.GetRelatedPersistenceService().Update(context, provider);
                case UserEntity userEntity:
                    return userEntity.GetRelatedPersistenceService().Update(context, userEntity);
                case DeviceEntity deviceEntity:
                    return deviceEntity.GetRelatedPersistenceService().Update(context, deviceEntity);
                case ApplicationEntity applicationEntity:
                    return applicationEntity.GetRelatedPersistenceService().Update(context, applicationEntity);
                case Person person:
                    return person.GetRelatedPersistenceService().Update(context, person);
                case ManufacturedMaterial manufacturedMaterial:
                    return manufacturedMaterial.GetRelatedPersistenceService().Update(context, manufacturedMaterial);
                case Material material:
                    return material.GetRelatedPersistenceService().Update(context, material);
                default:
                    if (this.TryGetSubclassPersister(data.ClassConceptKey.GetValueOrDefault(), out var service))
                    {
                        return (Entity)service.Update(context, data);
                    }
                    else
                    {
                        return base.DoUpdateModel(context, data);
                    }
            }
        }

    }
}