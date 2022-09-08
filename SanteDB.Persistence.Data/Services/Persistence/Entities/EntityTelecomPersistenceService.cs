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
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
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
    /// A persistence service that operates on telecoms
    /// </summary>
    public class EntityTelecomPersistenceService : EntityAssociationPersistenceService<EntityTelecomAddress, DbTelecomAddress>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public EntityTelecomPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare references for the insertion/update
        /// </summary>
        protected override EntityTelecomAddress BeforePersisting(DataContext context, EntityTelecomAddress data)
        {
            data.AddressUseKey = this.EnsureExists(context, data.AddressUse)?.Key ?? data.AddressUseKey;
            data.TypeConceptKey = this.EnsureExists(context, data.TypeConcept)?.Key ?? data.TypeConceptKey;

            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Convert the telecom address
        /// </summary>
        protected override EntityTelecomAddress DoConvertToInformationModel(DataContext context, DbTelecomAddress dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.AddressUse = retVal.AddressUse.GetRelatedPersistenceService().Get(context, dbModel.TelecomUseKey);
                    retVal.SetLoaded(nameof(EntityTelecomAddress.AddressUse));
                    retVal.TypeConcept = retVal.TypeConcept.GetRelatedPersistenceService().Get(context, dbModel.TypeConceptKey.GetValueOrDefault());
                    retVal.SetLoaded(nameof(EntityTelecomAddress.TypeConcept));
                    break;
            }

            return retVal;
        }
    }
}