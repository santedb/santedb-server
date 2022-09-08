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
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// Persistence service which is responsible for management of non-person living subjects (like animals, food, substances, viruses, etc.)
    /// </summary>
    public class NonPersonLivingSubjectPersistenceService : EntityDerivedPersistenceService<NonPersonLivingSubject, DbNonPersonLivingSubject>
    {
        /// <inheritdoc/>
        public NonPersonLivingSubjectPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc />
        protected override NonPersonLivingSubject BeforePersisting(DataContext context, NonPersonLivingSubject data)
        {
            data.StrainKey = this.EnsureExists(context, data.Strain)?.Key ?? data.StrainKey;
            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Convert to a sub-class
        /// </summary>
        protected override NonPersonLivingSubject DoConvertToInformationModelEx(DataContext context, DbEntityVersion dbModel, params object[] referenceObjects)
        {
            var modelData = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            var nplsData = referenceObjects.OfType<DbNonPersonLivingSubject>().FirstOrDefault();
            if(nplsData == null )
            {
                this.m_tracer.TraceWarning("Using slow load of NonPersonLivingSubjectData to DbEntityVersion");
                nplsData = context.FirstOrDefault<DbNonPersonLivingSubject>(o => o.ParentKey == modelData.VersionKey);
            }

            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    modelData.Strain = modelData.Strain.GetRelatedPersistenceService().Get(context, nplsData.StrainKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.Strain);
                    break;
            }
            return modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbNonPersonLivingSubject, NonPersonLivingSubject>(nplsData), false, declaredOnly: true);

        }
    }
}
