/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2021-8-5
 */
using SanteDB.Core.Model.DataTypes;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model.DataType;
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;
using System;
using System.Collections;
using System.Linq;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Entity identifier persistence service
    /// </summary>
    public class EntityIdentifierPersistenceService : IdentifiedPersistenceService<EntityIdentifier, DbEntityIdentifier, CompositeResult<DbEntityIdentifier, DbAssigningAuthority>>, IAdoAssociativePersistenceService
    {

        public EntityIdentifierPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// Convert to model
        /// </summary>
        public override EntityIdentifier ToModelInstance(object dataInstance, DataContext context)
        {
            var identifier = (dataInstance as CompositeResult)?.Values.OfType<DbEntityIdentifier>().FirstOrDefault() ?? dataInstance as DbEntityIdentifier;
            var authority = (dataInstance as CompositeResult)?.Values.OfType<DbAssigningAuthority>().FirstOrDefault();

            return new EntityIdentifier()
            {
                AuthorityKey = identifier.AuthorityKey,
                Authority = authority != null ? new AssigningAuthority(authority.DomainName, authority.Name, authority.Oid) { Key = authority.Key } : null,
                EffectiveVersionSequenceId = identifier.EffectiveVersionSequenceId,
                IdentifierTypeKey = identifier.TypeKey,
                LoadState = Core.Model.LoadState.PartialLoad,
                Key = identifier.Key,
                SourceEntityKey = identifier.SourceKey,
                ObsoleteVersionSequenceId = identifier.ObsoleteVersionSequenceId,
                Value = identifier.Value
            };
        }

        /// <summary>
        /// Get from source
        /// </summary>
        public IEnumerable GetFromSource(DataContext context, Guid id, decimal? versionSequenceId)
        {
            int tr = 0;
            return this.QueryInternal(context, base.BuildSourceQuery<EntityIdentifier>(id, versionSequenceId), Guid.Empty, 0, null, out tr, null, false).ToList();
        }

        /// <summary>
        /// Insert the entity identifier
        /// </summary>
        public override EntityIdentifier InsertInternal(DataContext context, EntityIdentifier data)
        {
            if (data.Authority != null) data.Authority = data.Authority.EnsureExists(context) as AssigningAuthority;
            data.AuthorityKey = data.Authority?.Key ?? data.AuthorityKey;

            if (!data.EffectiveVersionSequenceId.HasValue) // Retrieve
                data.EffectiveVersionSequenceId = context.Query<DbEntityVersion>(o => o.Key == data.SourceEntityKey && o.ObsoletionTime == null).Select(o => o.VersionSequenceId).First();

            return base.InsertInternal(context, data);
        }

        /// <summary>
        /// Update the data
        /// </summary>
        public override EntityIdentifier UpdateInternal(DataContext context, EntityIdentifier data)
        {
            if (data.Authority != null) data.Authority = data.Authority.EnsureExists(context) as AssigningAuthority;
            data.AuthorityKey = data.Authority?.Key ?? data.AuthorityKey;

            if (!data.EffectiveVersionSequenceId.HasValue) // Retrieve
                data.EffectiveVersionSequenceId = context.Query<DbEntityVersion>(o => o.Key == data.SourceEntityKey && o.ObsoletionTime == null).Select(o => o.VersionSequenceId).First();
            if(data.ObsoleteVersionSequenceId == Int32.MaxValue) // Retrieve for obs
                data.ObsoleteVersionSequenceId = context.Query<DbEntityVersion>(o => o.Key == data.SourceEntityKey && o.ObsoletionTime == null).Select(o => o.VersionSequenceId).First();

            return base.UpdateInternal(context, data);
        }

    }
}
