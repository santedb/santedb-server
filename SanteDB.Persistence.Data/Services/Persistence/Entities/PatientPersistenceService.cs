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
    /// Persistence service which handles the storage of Patient resources
    /// </summary>
    public class PatientPersistenceService : PersonDerivedPersistenceService<Patient, DbPatient>
    {
        /// <summary>
        /// DI Constructor
        /// </summary>
        public PatientPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc />
        protected override Patient BeforePersisting(DataContext context, Patient data)
        {
            data.EducationLevelKey = this.EnsureExists(context, data.EducationLevel)?.Key ?? data.EducationLevelKey;
            data.EthnicGroupKey = this.EnsureExists(context, data.EthnicGroup)?.Key ?? data.EthnicGroupKey;
            data.LivingArrangementKey = this.EnsureExists(context, data.LivingArrangement)?.Key ?? data.LivingArrangementKey;
            data.MaritalStatusKey = this.EnsureExists(context, data.MaritalStatus)?.Key ?? data.MaritalStatusKey;
            data.NationalityKey = this.EnsureExists(context, data.Nationality)?.Key ?? data.NationalityKey;
            data.ReligiousAffiliationKey = this.EnsureExists(context, data.ReligiousAffiliation)?.Key ?? data.ReligiousAffiliationKey;
            data.VipStatusKey = this.EnsureExists(context, data.VipStatus)?.Key ?? data.VipStatusKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override Patient DoConvertToInformationModelEx(DataContext context, DbEntityVersion dbModel, params object[] referenceObjects)
        {
            var modelData = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);

            var dbPatient = referenceObjects.OfType<DbPatient>().FirstOrDefault();
            if(dbPatient == null)
            {
                this.m_tracer.TraceWarning("Using slow fetch of DbPatient for DbEntityVersion (consider using the appropriate persister class)");
                dbPatient = context.FirstOrDefault<DbPatient>(o => o.ParentKey == dbModel.VersionKey);
            }

            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    var conceptPersister = typeof(Concept).GetRelatedPersistenceService() as IAdoPersistenceProvider<Concept>;
                    modelData.EducationLevel = conceptPersister.Get(context, dbPatient.EducationLevelKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.EducationLevel);
                    modelData.EthnicGroup = conceptPersister.Get(context, dbPatient.EthnicGroupKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.EthnicGroup);
                    modelData.LivingArrangement = conceptPersister.Get(context, dbPatient.LivingArrangementKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.LivingArrangement);
                    modelData.MaritalStatus = conceptPersister.Get(context, dbPatient.MaritalStatusKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.MaritalStatus);
                    modelData.Nationality = conceptPersister.Get(context, dbPatient.NationalityKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.Nationality);
                    modelData.ReligiousAffiliation = conceptPersister.Get(context, dbPatient.ReligiousAffiliationKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.ReligiousAffiliation);
                    modelData.VipStatus = conceptPersister.Get(context, dbPatient.VipStatusKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.VipStatus);
                    break;
            }

            modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbPatient, Patient>(dbPatient), false, declaredOnly: true);

            return modelData;
        }
    }
}
