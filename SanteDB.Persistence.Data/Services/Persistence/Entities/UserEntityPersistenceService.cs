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
using SanteDB.Core.Model.Security;
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
    /// Persistence service which stores and manages <seealso cref="UserEntity"/>
    /// </summary>
    public class UserEntityPersistenceService : PersonDerivedPersistenceService<UserEntity, DbUserEntity>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public UserEntityPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Called prior to persisting
        /// </summary>
        protected override UserEntity BeforePersisting(DataContext context, UserEntity data)
        {
            data.SecurityUserKey = this.EnsureExists(context, data.SecurityUser)?.Key ?? data.SecurityUserKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override UserEntity DoConvertToInformationModelEx(DataContext context, DbEntityVersion dbModel, params object[] referenceObjects)
        {
            var modelData = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);

            var userData = referenceObjects.OfType<DbUserEntity>().FirstOrDefault();
            if (userData == null)
            {
                this.m_tracer.TraceWarning("Will use slow loading method for DbUserEntity from DbEntityVersion");
                userData = context.FirstOrDefault<DbUserEntity>(o => o.ParentKey == dbModel.VersionKey);
            }

            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    modelData.SecurityUser = modelData.SecurityUser.GetRelatedPersistenceService().Get(context, userData.SecurityUserKey);
                    modelData.SetLoaded(o => o.SecurityUser);
                    break;
            }
            modelData.SecurityUserKey = userData.SecurityUserKey;
            return modelData;
        }
    }

}
