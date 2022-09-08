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
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Acts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// Persistence service which can store and retrieve patient procedures
    /// </summary>
    public class ProcedurePersistenceService : ActDerivedPersistenceService<Procedure, DbProcedure>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public ProcedurePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override Procedure BeforePersisting(DataContext context, Procedure data)
        {
            data.ApproachSiteKey = this.EnsureExists(context, data.ApproachSite)?.Key ?? data.ApproachSiteKey;
            data.MethodKey = this.EnsureExists(context, data.Method)?.Key ?? data.MethodKey;
            data.TargetSiteKey = this.EnsureExists(context, data.TargetSite)?.Key ?? data.TargetSiteKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override Procedure DoConvertToInformationModelEx(DataContext context, DbActVersion dbModel, params object[] referenceObjects)
        {
            var modelData = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            var procedureData = referenceObjects.OfType<DbProcedure>().FirstOrDefault();
            if(procedureData == null)
            {
                this.m_tracer.TraceWarning("Using slow method of loading DbNarrative data from DbActVersion - Consider using the Narrative persistence service instead");
                procedureData = context.FirstOrDefault<DbProcedure>(o => o.ParentKey == dbModel.VersionKey);
            }

            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    modelData.ApproachSite = modelData.ApproachSite.GetRelatedMappingProvider().Get(context, procedureData.ApproachSiteConceptKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.ApproachSite);
                    modelData.Method = modelData.Method.GetRelatedMappingProvider().Get(context, procedureData.MethodConceptKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.Method);
                    modelData.TargetSite = modelData.TargetSite.GetRelatedMappingProvider().Get(context, procedureData.TargetSiteConceptKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.TargetSite);
                    break;
            }

            modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbProcedure, Procedure>(procedureData), declaredOnly: true);
            return modelData;
        }
    }
}
