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
using SanteDB.Persistence.Data.Model.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// Place vs/ service persistence manager
    /// </summary>
    public class PlaceServicePersistenceService : EntityAssociationPersistenceService<PlaceService, DbPlaceService>
    {
        /// <inheritdoc />
        public PlaceServicePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc />
        protected override PlaceService BeforePersisting(DataContext context, PlaceService data)
        {
            data.ServiceConceptKey = this.EnsureExists(context, data.ServiceConcept)?.Key ?? data.ServiceConceptKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc />
        protected override PlaceService DoConvertToInformationModel(DataContext context, DbPlaceService dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.ServiceConcept = retVal.ServiceConcept.GetRelatedPersistenceService().Get(context, dbModel.ServiceConceptKey);
                    retVal.SetLoaded(o => o.ServiceConcept);
                    break;
            }

            return retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbPlaceService, PlaceService>(dbModel));
        }
    }
}
