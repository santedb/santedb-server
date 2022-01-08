/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */

using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SanteDB.Core.Model;
using System.Data;
using System.Data.Common;
using SanteDB.Core.Exceptions;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Entity relationship persistence service
    /// </summary>
    public class EntityRelationshipPersistenceService : IdentifiedPersistenceService<EntityRelationship, DbEntityRelationship>, IAdoAssociativePersistenceService
    {
        public EntityRelationshipPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// Get relationships from source
        /// </summary>
        public IEnumerable GetFromSource(DataContext context, Guid id, decimal? versionSequenceId)
        {
            int tr = 0;
            return this.QueryInternal(context, base.BuildSourceQuery<EntityRelationship>(id, versionSequenceId), Guid.Empty, 0, null, out tr, null, false).ToList();
        }

        /// <summary>
        /// Represents as a model instance
        /// </summary>
        public override EntityRelationship ToModelInstance(object dataInstance, DataContext context)
        {
            if (dataInstance == null) return null;

            var entPart = dataInstance as DbEntityRelationship;
            return new EntityRelationship()
            {
                EffectiveVersionSequenceId = entPart.EffectiveVersionSequenceId,
                ObsoleteVersionSequenceId = entPart.ObsoleteVersionSequenceId,
                HolderKey = entPart.SourceKey,
                TargetEntityKey = entPart.TargetKey,
                RelationshipType = context.LoadState == Core.Model.LoadState.FullLoad ? this.m_settingsProvider.GetPersister(typeof(Concept)).Get(entPart.RelationshipTypeKey) as Concept : null,
                RelationshipTypeKey = entPart.RelationshipTypeKey,
                Quantity = entPart.Quantity,
                LoadState = context.LoadState,
                Key = entPart.Key,
                SourceEntityKey = entPart.SourceKey,
                ClassificationKey = entPart.ClassificationKey,
                RelationshipRoleKey = entPart.RelationshipRoleKey,
                Strength = entPart.Strength
            };
        }

        /// <summary>
        /// Insert the relationship
        /// </summary>
        public override EntityRelationship InsertInternal(DataContext context, EntityRelationship data)
        {
            // Ensure we haven't already persisted this
            if (data.InversionIndicator)
                return data; // don't persist inverted
            if (data.TargetEntity != null) data.TargetEntity = data.TargetEntity.EnsureExists(context, this.m_settingsProvider.GetConfiguration().AutoInsertChildren, typeof(Entity)) as Entity;
            data.TargetEntityKey = data.TargetEntity?.Key ?? data.TargetEntityKey;
            if (data.RelationshipType != null) data.RelationshipType = data.RelationshipType.EnsureExists(context, false) as Concept;
            data.RelationshipTypeKey = data.RelationshipType?.Key ?? data.RelationshipTypeKey;
            data.EffectiveVersionSequenceId = data.EffectiveVersionSequenceId ?? data.SourceEntity?.VersionSequence;
            // Lookup the original
            if (!data.EffectiveVersionSequenceId.HasValue)
                data.EffectiveVersionSequenceId = context.Query<DbEntityVersion>(o => o.Key == data.SourceEntityKey && !o.ObsoletionTime.HasValue).OrderByDescending(o => o.VersionSequenceId).Select(o => o.VersionSequenceId).ToArray().FirstOrDefault();
            else if (data.ObsoleteVersionSequenceId.HasValue) // No sense in inserting an obsolete object
                return data;

            // Duplicate check
            var existing = context.FirstOrDefault<DbEntityRelationship>(r => r.SourceKey == data.SourceEntityKey && r.TargetKey == data.TargetEntityKey && r.RelationshipTypeKey == data.RelationshipTypeKey && !r.ObsoleteVersionSequenceId.HasValue);
            if (existing == null)
                return base.InsertInternal(context, data);
            else if (existing.Quantity != data.Quantity || existing.Strength != data.Strength)
            {
                data.Key = existing.Key;
                return base.UpdateInternal(context, data);
            }
            else
            {
                data.Key = existing.Key;
                data.EffectiveVersionSequenceId = existing.EffectiveVersionSequenceId;
                data.ObsoleteVersionSequenceId = existing.ObsoleteVersionSequenceId;
                return this.ToModelInstance(existing, context);
            }
        }

        /// <summary>
        /// Update the specified object
        /// </summary>
        public override EntityRelationship UpdateInternal(DataContext context, EntityRelationship data)
        {
            // Ensure we haven't already persisted this
            if (data.InversionIndicator)
                return data; // don't persist inverted

            data.TargetEntityKey = data.TargetEntity?.Key ?? data.TargetEntityKey;
            if (data.RelationshipType != null) data.RelationshipType = data.RelationshipType.EnsureExists(context, false) as Concept;
            data.RelationshipTypeKey = data.RelationshipType?.Key ?? data.RelationshipTypeKey;

            if (!data.EffectiveVersionSequenceId.HasValue)
                data.EffectiveVersionSequenceId = context.Query<DbEntityVersion>(o => o.Key == data.SourceEntityKey && !o.ObsoletionTime.HasValue).OrderByDescending(o => o.VersionSequenceId).Select(o => o.VersionSequenceId).FirstOrDefault();

            if (data.ObsoleteVersionSequenceId == Int32.MaxValue || data.BatchOperation == BatchOperationType.Delete)
                data.ObsoleteVersionSequenceId = context.Query<DbEntityVersion>(o => o.Key == data.SourceEntityKey && !o.ObsoletionTime.HasValue).OrderByDescending(o => o.VersionSequenceId).Select(o => o.VersionSequenceId).FirstOrDefault();

            // Duplicate check
            var existing = context.FirstOrDefault<DbEntityRelationship>(r => r.SourceKey == data.SourceEntityKey && r.TargetKey == data.TargetEntityKey && r.RelationshipTypeKey == data.RelationshipTypeKey && !r.ObsoleteVersionSequenceId.HasValue);
            if (existing != null && existing.Key != data.Key) // There is an existing relationship which isn't this one, obsolete it
            {
                existing.ObsoleteVersionSequenceId = data.SourceEntity?.VersionSequence;
                if (existing.ObsoleteVersionSequenceId.HasValue)
                    context.Update(existing);
                else
                {
                    this.m_tracer.TraceWarning("EntityRelationship {0} would conflict with existing {1} -> {2} (role {3}, quantity = {4}) already exists and this update would violate unique constraint.", data, existing.SourceKey, existing.TargetKey, existing.RelationshipTypeKey, existing.Quantity);
                    existing.ObsoleteVersionSequenceId = 1;
                    context.Update(existing);
                }
            }

            return base.UpdateInternal(context, data);
        }

        /// <summary>
        /// Obsolete the data
        /// </summary>
        public override EntityRelationship ObsoleteInternal(DataContext context, EntityRelationship data)
        {
            var obsoletionSequence = data.SourceEntity?.VersionSequence;
            if (obsoletionSequence == null)
                obsoletionSequence = context.FirstOrDefault<DbEntityVersion>(o => o.Key == data.SourceEntityKey && o.ObsoletionTime == null)?.VersionSequenceId;

            data.ObsoleteVersionSequenceId = obsoletionSequence;
            return base.UpdateInternal(context, data);
        }

        /// <summary>
        /// Comparer for entity relationships
        /// </summary>
        internal class Comparer : IEqualityComparer<EntityRelationship>
        {
            /// <summary>
            /// Determine equality between the two relationships
            /// </summary>
            public bool Equals(EntityRelationship x, EntityRelationship y)
            {
                return x.SourceEntityKey == y.SourceEntityKey &&
                    x.TargetEntityKey == y.TargetEntityKey &&
                    (x.RelationshipTypeKey == y.RelationshipTypeKey || x.RelationshipType?.Mnemonic == y.RelationshipType?.Mnemonic);
            }

            /// <summary>
            /// Get hash code
            /// </summary>
            public int GetHashCode(EntityRelationship obj)
            {
                int result = obj.SourceEntityKey.GetHashCode();
                result = 37 * result + obj.RelationshipTypeKey.GetHashCode();
                result = 37 * result + obj.TargetEntityKey.GetHashCode();
                result = 37 * result + (obj.RelationshipType?.Mnemonic.GetHashCode() ?? 0);
                return result;
            }
        }
    }
}