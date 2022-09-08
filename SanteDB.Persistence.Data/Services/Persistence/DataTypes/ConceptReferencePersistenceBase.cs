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
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Concepts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// Represents the concept relationship persistence service
    /// </summary>
    public abstract class ConceptReferencePersistenceBase<TModel, TDbModel> : VersionedAssociationPersistenceService<TModel, TDbModel>
        where TModel : VersionedAssociation<Concept>, new()
        where TDbModel : DbIdentified, IDbVersionedAssociation, new()
    {
        /// <summary>
        /// Creates a DI
        /// </summary>
        public ConceptReferencePersistenceBase(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Get the most current version sequence from the database
        /// </summary>
        protected override long GetCurrentVersionSequenceForSource(DataContext context, Guid sourceKey)
        {
            if (context.Data.TryGetValue($"Concept{sourceKey}Version", out object versionSequenceObject) && versionSequenceObject is long versionSequence)
            {
                return versionSequence;
            }
            else
            {
                versionSequence = context.Query<DbConceptVersion>(o => o.Key == sourceKey && !o.ObsoletionTime.HasValue).OrderByDescending(o => o.VersionSequenceId).FirstOrDefault()?.VersionSequenceId ?? -1;
                if (versionSequence == -1)
                {
                    throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { id = sourceKey, type = "Entity" }));
                }
                context.Data.Add($"Concept{sourceKey}Version", versionSequence);
                return versionSequence;
            }
        }
    }
}