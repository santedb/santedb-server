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
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.DataType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// Persistence service for act identifiers
    /// </summary>
    public class ActIdentifierPersistenceService : ActAssociationPersistenceService<ActIdentifier, DbActIdentifier>
    {
        /// <summary>
        /// Dependency injection of configuration
        /// </summary>
        public ActIdentifierPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare references
        /// </summary>
        protected override ActIdentifier BeforePersisting(DataContext context, ActIdentifier data)
        {
            data.IdentityDomainKey = this.EnsureExists(context, data.IdentityDomain)?.Key ?? data.IdentityDomainKey;
            data.IdentifierTypeKey = this.EnsureExists(context, data.IdentifierType)?.Key ?? data.IdentifierTypeKey;
            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Convert from db model to information model
        /// </summary>
        protected override ActIdentifier DoConvertToInformationModel(DataContext context, DbActIdentifier dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.IdentifierType = retVal.IdentifierType.GetRelatedPersistenceService().Get(context, dbModel.TypeKey.GetValueOrDefault());
                    retVal.SetLoaded(nameof(EntityIdentifier.IdentifierType));
                    goto case LoadMode.SyncLoad;
                case LoadMode.SyncLoad:
                    retVal.IdentityDomain = retVal.IdentityDomain.GetRelatedMappingProvider().ToModelInstance(context, referenceObjects.OfType<DbIdentityDomain>().FirstOrDefault()) ??
                        retVal.IdentityDomain.GetRelatedPersistenceService().Get(context, dbModel.IdentityDomainKey);
                    retVal.SetLoaded(o=>o.IdentityDomain);
                    break;

                case LoadMode.QuickLoad:
                    retVal.IdentityDomain = retVal.IdentityDomain.GetRelatedMappingProvider().ToModelInstance(context, referenceObjects.OfType<DbIdentityDomain>().FirstOrDefault());
                    if (retVal.IdentityDomain != null)
                    {
                        retVal.SetLoaded(o => o.IdentityDomain);
                    }
                    break;
            }
            return retVal;
        }
    }
}