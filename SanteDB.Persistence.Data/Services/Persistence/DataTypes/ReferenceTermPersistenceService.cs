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
using SanteDB.Persistence.Data.Model.Concepts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// Persistence service for reference terms.
    /// </summary>
    public class ReferenceTermPersistenceService : NonVersionedDataPersistenceService<ReferenceTerm, DbReferenceTerm>
    {
        /// <summary>
        /// Reference term persistence service
        /// </summary>
        public ReferenceTermPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Delete references
        /// </summary>
        protected override void DoDeleteReferencesInternal(DataContext context, Guid key)
        {
            context.DeleteAll<DbReferenceTermName>(o => o.SourceKey == key);
            context.DeleteAll<DbConceptReferenceTerm>(o => o.TargetKey == key);

            base.DoDeleteReferencesInternal(context, key);
        }

        /// <summary>
        /// Prepare references for persistence
        /// </summary>
        protected override ReferenceTerm BeforePersisting(DataContext context, ReferenceTerm data)
        {
            data.CodeSystemKey = this.EnsureExists(context, data.CodeSystem)?.Key ?? data.CodeSystemKey;
            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Convert the refeence term to reference model
        /// </summary>
        protected override ReferenceTerm DoConvertToInformationModel(DataContext context, DbReferenceTerm dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.CodeSystem = retVal.CodeSystem.GetRelatedPersistenceService().Get(context, dbModel.CodeSystemKey);
                    retVal.SetLoaded(nameof(ReferenceTerm.CodeSystem));
                    goto case LoadMode.SyncLoad;
                case LoadMode.SyncLoad:
                    retVal.DisplayNames = retVal.DisplayNames.GetRelatedPersistenceService().Query(context, o => o.SourceEntityKey == dbModel.Key).ToList();
                    retVal.SetLoaded(nameof(ReferenceTerm.DisplayNames));
                    break;
            }

            return retVal;
        }

        /// <summary>
        /// Perform an insertion of the obect and dependent properties
        /// </summary>
        protected override ReferenceTerm DoInsertModel(DataContext context, ReferenceTerm data)
        {
            var retVal = base.DoInsertModel(context, data);

            if (data.DisplayNames != null)
            {
                retVal.DisplayNames = base.UpdateModelAssociations(context, retVal, data.DisplayNames).ToList();
                retVal.SetLoaded(o => o.DisplayNames);

            }

            return retVal;
        }

        /// <summary>
        /// Perform an insertion of the obect and dependent properties
        /// </summary>
        protected override ReferenceTerm DoUpdateModel(DataContext context, ReferenceTerm data)
        {
            var retVal = base.DoUpdateModel(context, data);

            if (data.DisplayNames != null)
            {
                retVal.DisplayNames = base.UpdateModelAssociations(context, retVal, data.DisplayNames).ToList();
                retVal.SetLoaded(o => o.DisplayNames);

            }

            return retVal;
        }
    }
}