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
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Acts;
using System;
using System.Collections.Generic;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// A generic implementation of the version association which points at an act
    /// </summary>
    public abstract class ActAssociationPersistenceService<TModel, TDbModel> : VersionedAssociationPersistenceService<TModel, TDbModel>
        where TDbModel : DbIdentified, IDbVersionedAssociation, new()
        where TModel : IdentifiedData, IVersionedAssociation, new()
    {
        /// <summary>
        /// DI injected class
        /// </summary>
        public ActAssociationPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// The object <paramref name="key"/> is being purged - delete all references for the object
        /// </summary>
        protected override void DoDeleteReferencesInternal(DataContext context, Guid key)
        { }

        /// <summary>
        /// Get current version sequence
        /// </summary>
        /// <returns></returns>
        protected override long GetCurrentVersionSequenceForSource(DataContext context, Guid sourceKey)
        {
            if (context.Data.TryGetValue($"Act{sourceKey}Version", out object versionSequenceObject) && versionSequenceObject is long versionSequence)
            {
                return versionSequence;
            }
            else
            {
                versionSequence = context.Query<DbActVersion>(o => o.Key == sourceKey && !o.ObsoletionTime.HasValue).OrderByDescending(o => o.VersionSequenceId).FirstOrDefault()?.VersionSequenceId ?? -1;
                if (versionSequence == -1)
                {
                    throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { id = sourceKey, type = nameof(Act) }));
                }
                context.Data.Add($"Act{sourceKey}Version", versionSequence);
                return versionSequence;
            }
        }
    }
}