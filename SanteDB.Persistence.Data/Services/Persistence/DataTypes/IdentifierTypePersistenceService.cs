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
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// Identifier type persistence service
    /// </summary>
    public class IdentifierTypePersistenceService : BaseEntityDataPersistenceService<IdentifierType, DbIdentifierType>
    {
        /// <summary>
        /// Creates a DI instance of the identifier type
        /// </summary>
        public IdentifierTypePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Prepare all references
        /// </summary>
        protected override IdentifierType BeforePersisting(DataContext context, IdentifierType data)
        {
            data.ScopeConceptKey = this.EnsureExists(context, data.ScopeConcept)?.Key ?? data.ScopeConceptKey;
            data.TypeConceptKey = this.EnsureExists(context, data.TypeConcept)?.Key ?? data.TypeConceptKey;
            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Convert the database model to an information model object
        /// </summary>
        protected override IdentifierType DoConvertToInformationModel(DataContext context, DbIdentifierType dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.TypeConcept = retVal.TypeConcept.GetRelatedPersistenceService().Get(context, dbModel.TypeConceptKey);
                    retVal.SetLoaded(nameof(IdentifierType.TypeConcept));
                    retVal.ScopeConcept = retVal.ScopeConcept.GetRelatedPersistenceService().Get(context, dbModel.ScopeConceptKey.GetValueOrDefault());
                    retVal.SetLoaded(nameof(IdentifierType.ScopeConcept));
                    break;
            }
            return retVal;
        }
    }
}