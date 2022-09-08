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
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Acts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// A <see cref="IDataPersistenceService{TModel}"/> which is responsible for the storage and maintenance of <see cref="Protocol"/> definitions
    /// </summary>
    public class ProtocolPersistenceService : BaseEntityDataPersistenceService<Protocol, DbProtocol>
    {
        /// <inheritdoc/>
        public ProtocolPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override Protocol BeforePersisting(DataContext context, Protocol data)
        {
            data.NarrativeKey = this.EnsureExists(context, data.Narrative)?.Key ?? data.NarrativeKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override Protocol DoConvertToInformationModel(DataContext context, DbProtocol dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    if (dbModel.NarrativeKey.HasValue)
                    {
                        retVal.Narrative = retVal.Narrative.GetRelatedPersistenceService<Narrative>().Get(context, dbModel.NarrativeKey.Value);
                        retVal.SetLoaded(o => o.Narrative);
                    }
                    break;
            }

            // copy data
            retVal.Definition = dbModel.Definition;
            retVal.HandlerClassName = dbModel.HandlerClassName;
            retVal.Name = dbModel.Name;
            retVal.Oid = dbModel.Oid;
            return retVal;
        }
    }
}
