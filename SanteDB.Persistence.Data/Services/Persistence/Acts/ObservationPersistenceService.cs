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
using SanteDB.Core.i18n;
using SanteDB.Core.Model.Acts;
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
    /// Generic observation persistence service to handle Observation calls
    /// </summary>
    public class ObservationPersistenceService : ActDerivedPersistenceService<Observation, DbObservation>
    {
        /// <summary>
        /// DI Constructor
        /// </summary>
        public ObservationPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override Observation DoInsertModel(DataContext context, Observation data)
        {
            switch(data.ValueType)
            {
                case "ST":
                    return (TextObservation)typeof(TextObservation).GetRelatedPersistenceService().Insert(context, data);
                case "PQ":
                    return (QuantityObservation)typeof(QuantityObservation).GetRelatedPersistenceService().Insert(context, data);
                case "CD":
                    return (CodedObservation)typeof(CodedObservation).GetRelatedPersistenceService().Insert(context, data);
                default:
                    throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_OUT_OF_RANGE, data.ValueType, "ST,CD,PQ"));
            }
        }

        /// <inheritdoc/>
        protected override Observation DoUpdateModel(DataContext context, Observation data)
        {
            switch (data.ValueType)
            {
                case "ST":
                    return (TextObservation)typeof(TextObservation).GetRelatedPersistenceService().Update(context, data);
                case "PQ":
                    return (QuantityObservation)typeof(QuantityObservation).GetRelatedPersistenceService().Update(context, data);
                case "CD":
                    return (CodedObservation)typeof(CodedObservation).GetRelatedPersistenceService().Update(context, data);
                default:
                    throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_OUT_OF_RANGE, data.ValueType, "ST,CD,PQ"));
            }
        }

        /// <summary>
        /// Convert to information model extended
        /// </summary>
        protected override Observation DoConvertToInformationModelEx(DataContext context, DbActVersion dbModel, params object[] referenceObjects)
        {
            var obsData = referenceObjects.OfType<DbObservation>().FirstOrDefault();
            if(obsData == null)
            {
                obsData = context.FirstOrDefault<DbObservation>(o => o.ParentKey == dbModel.VersionKey);
            }

            IAdoClassMapper mapper = null;
            switch (obsData?.ValueType)
            {
                case "ST":
                    mapper = typeof(TextObservation).GetRelatedPersistenceService() as IAdoClassMapper;
                    break;
                case "PQ":
                    mapper = typeof(QuantityObservation).GetRelatedPersistenceService() as IAdoClassMapper;
                    break;
                case "CD":
                    mapper = typeof(CodedObservation).GetRelatedPersistenceService() as IAdoClassMapper;
                    break;
            }
            return mapper?.MapToModelInstanceEx(context, dbModel, referenceObjects) as Observation ??
                base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
        }
    } 
}
