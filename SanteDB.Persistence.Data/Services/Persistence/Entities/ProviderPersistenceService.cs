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
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Entities;
using SanteDB.Persistence.Data.Model.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// Persistence service which handles provider classes
    /// </summary>
    public class ProviderPersistenceService : PersonDerivedPersistenceService<Provider, DbProvider>
    {
        /// <summary>
        /// DI constructor for providers
        /// </summary>
        public ProviderPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override Provider BeforePersisting(DataContext context, Provider data)
        {
            data.SpecialtyKey = this.EnsureExists(context, data.Specialty)?.Key ?? data.SpecialtyKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override Provider DoConvertToInformationModelEx(DataContext context, DbEntityVersion dbModel, params object[] referenceObjects)
        {
            var modelData = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);

            var dbProvider = referenceObjects.OfType<DbProvider>().FirstOrDefault();
            if(dbProvider == null)
            {
                this.m_tracer.TraceWarning("Using slow loading for DbProvider data on DbEntityVersion. Consider using the IDataPersistenceService<Provider> instead");
                dbProvider = context.FirstOrDefault<DbProvider>(o => o.ParentKey == dbModel.VersionKey);
            }

            switch(DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    modelData.Specialty = modelData.Specialty.GetRelatedPersistenceService().Get(context, dbProvider.SpecialtyKey);
                    modelData.SetLoaded(o => o.Specialty);
                    break;
            }
            modelData.SpecialtyKey = dbProvider.SpecialtyKey;
            return modelData; // no additional data to be loaded
        }
    }
}
