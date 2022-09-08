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
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// Application entity persistence serivce for application entities
    /// </summary>
    public class ApplicationEntityPersistenceService : EntityDerivedPersistenceService<ApplicationEntity, DbApplicationEntity>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public ApplicationEntityPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Before persisting the data to the database
        /// </summary>
        protected override ApplicationEntity BeforePersisting(DataContext context, ApplicationEntity data)
        {
            data.SecurityApplicationKey = this.EnsureExists(context, data.SecurityApplication)?.Key ?? data.SecurityApplicationKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override ApplicationEntity DoConvertToInformationModelEx(DataContext context,  DbEntityVersion dbModel, params object[] referenceObjects)
        {

            var modelData = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            var dbApplication = referenceObjects.OfType<DbApplicationEntity>().FirstOrDefault();
            if (dbApplication == null)
            {
                this.m_tracer.TraceWarning("Using slow cross reference of application");
                dbApplication = context.FirstOrDefault<DbApplicationEntity>(o => o.ParentKey == dbModel.VersionKey);
            }

            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    modelData.SecurityApplication = modelData.SecurityApplication.GetRelatedPersistenceService().Get(context, dbApplication.SecurityApplicationKey);
                    modelData.SetLoaded(o => o.SecurityApplication);
                    break;
            }

            return modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbApplicationEntity, ApplicationEntity>(dbApplication), false, declaredOnly: true);

        }
    }
}