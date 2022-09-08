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
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// A persistence service class which stores and retrieves places
    /// </summary>
    public class PlacePersistenceService : EntityDerivedPersistenceService<Place, DbPlace>
    {
        /// <inheritdoc />
        public PlacePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc />
        protected override Place DoInsertModel(DataContext context, Place data)
        {
            var retVal = base.DoInsertModel(context, data);

            if(data.Services != null)
            {
                retVal.Services = this.UpdateModelVersionedAssociations(context, retVal, data.Services).ToList();
                retVal.SetLoaded(o => o.Services);
            }

            return retVal;
        }

        /// <inheritdoc/>
        protected override Place DoUpdateModel(DataContext context, Place data)
        {
            var retVal = base.DoUpdateModel(context, data);

            if (data.Services != null)
            {
                retVal.Services = this.UpdateModelVersionedAssociations(context, retVal, data.Services).ToList();
                retVal.SetLoaded(o => o.Services);
            }

            return retVal;
        }

        /// <inheritdoc/>
        protected override Place DoConvertToInformationModelEx(DataContext context, DbEntityVersion dbModel, params object[] referenceObjects)
        {
            var modelData = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);

            var placeData = referenceObjects.OfType<DbPlace>().FirstOrDefault();
            if (placeData == null)
            {
                this.m_tracer.TraceWarning("Using slow join to DbPlace from DbEntityVersion");
                placeData = context.FirstOrDefault<DbPlace>(o => o.ParentKey == dbModel.VersionKey);
            }

            // Deep loading?
            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                case LoadMode.SyncLoad:
                    modelData.Services = modelData.Services.GetRelatedPersistenceService().Query(context, r => r.SourceEntityKey == dbModel.Key)?.ToList();
                    modelData.SetLoaded(o => o.Services);
                    break;
            }
            modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbPlace, Place>(placeData), false, declaredOnly: true);
            return modelData;
        }
    }
}
