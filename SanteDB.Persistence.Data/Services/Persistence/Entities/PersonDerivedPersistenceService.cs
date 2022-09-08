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
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// A persistence service which is derived from a person persistence
    /// </summary>
    /// <remarks>This class exists to ensure that LanguageCommunication is properly inserted on sub-classes of the Person class</remarks>
    public abstract class PersonDerivedPersistenceService<TModel, TDbModel>
        : EntityDerivedPersistenceService<TModel, TDbModel, DbPerson>
        where TModel : Person, new()
        where TDbModel : DbEntitySubTable, new()
    {
        /// <inheritdoc/>
        protected PersonDerivedPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc />
        protected override TModel BeforePersisting(DataContext context, TModel data)
        {
            data.OccupationKey = this.EnsureExists(context, data.Occupation)?.Key ?? data.OccupationKey;
            data.GenderConceptKey = this.EnsureExists(context, data.GenderConcept)?.Key ?? data.GenderConceptKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override TModel DoInsertModel(DataContext context, TModel data)
        {
            var retVal = base.DoInsertModel(context, data);

            if (data.LanguageCommunication != null)
            {
                retVal.LanguageCommunication = this.UpdateModelVersionedAssociations(context, retVal, data.LanguageCommunication).ToList();
                retVal.SetLoaded(o => o.LanguageCommunication);
            }

            return retVal;
        }

        /// <summary>
        /// Perform the update
        /// </summary>ialpers
        protected override TModel DoUpdateModel(DataContext context, TModel data)
        {
            var retVal = base.DoUpdateModel(context, data);

            if (data.LanguageCommunication != null)
            {
                retVal.LanguageCommunication = this.UpdateModelVersionedAssociations(context, retVal, data.LanguageCommunication).ToList();
                retVal.SetLoaded(o => o.LanguageCommunication);
            }

            return retVal;
        }

        /// <inheritdoc/>
        protected override TModel DoConvertToInformationModelEx(DataContext context,  DbEntityVersion dbModel, params object[] referenceObjects)
        {
            var modelData = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            var personData = referenceObjects.OfType<DbPerson>().FirstOrDefault();
            if (personData == null)
            {
                this.m_tracer.TraceWarning("Using slow join to DbPerson from DbEntityVersion");
                personData = context.FirstOrDefault<DbPerson>(o => o.ParentKey == dbModel.VersionKey);
            }

            // Deep loading?
            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    modelData.GenderConcept = modelData.GenderConcept.GetRelatedPersistenceService().Get(context, personData.GenderConceptKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.GenderConcept);
                    modelData.Occupation = modelData.Occupation.GetRelatedPersistenceService().Get(context, personData.OccupationKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.Occupation);
                    goto case LoadMode.SyncLoad;
                case LoadMode.SyncLoad:
                    modelData.LanguageCommunication = modelData.LanguageCommunication.GetRelatedPersistenceService().Query(context, r => r.SourceEntityKey == dbModel.Key)?.ToList();
                    modelData.SetLoaded(o => o.LanguageCommunication);
                    break;
            }
            modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbPerson, Person>(personData), false, declaredOnly: true);
            return modelData;
        }
    }
}
