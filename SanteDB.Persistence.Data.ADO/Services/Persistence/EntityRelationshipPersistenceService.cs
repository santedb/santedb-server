/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
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
 * User: JustinFyfe
 * Date: 2019-1-22
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

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Entity relationship persistence service
    /// </summary>
    public class EntityRelationshipPersistenceService : IdentifiedPersistenceService<EntityRelationship, DbEntityRelationship>, IAdoAssociativePersistenceService
    {

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
                RelationshipType = context.LoadState == Core.Model.LoadState.FullLoad ? this.m_persistenceService.GetPersister(typeof(Concept)).Get(entPart.RelationshipTypeKey) as Concept : null,
                RelationshipTypeKey = entPart.RelationshipTypeKey,
                Quantity = entPart.Quantity,
                LoadState = context.LoadState,
                Key = entPart.Key,
                SourceEntityKey = entPart.SourceKey
            };
        }

        /// <summary>
        /// Insert the relationship
        /// </summary>
        public override EntityRelationship InsertInternal(DataContext context, EntityRelationship data)
        {
            
            // Ensure we haven't already persisted this
            if(data.TargetEntity != null && !data.InversionIndicator) data.TargetEntity = data.TargetEntity.EnsureExists(context) as Entity;
            data.TargetEntityKey = data.TargetEntity?.Key ?? data.TargetEntityKey;
            data.RelationshipTypeKey = data.RelationshipType?.Key ?? data.RelationshipTypeKey;
            data.EffectiveVersionSequenceId = data.EffectiveVersionSequenceId ?? data.SourceEntity?.VersionSequence;
            return base.InsertInternal(context, data);
        }

        /// <summary>
        /// Update the specified object
        /// </summary>
        public override EntityRelationship UpdateInternal(DataContext context, EntityRelationship data)
        {
            // Ensure we haven't already persisted this
            data.TargetEntityKey = data.TargetEntity?.Key ?? data.TargetEntityKey;
            data.RelationshipTypeKey = data.RelationshipType?.Key ?? data.RelationshipTypeKey;

            if(data.ObsoleteVersionSequenceId == Int32.MaxValue)
                data.ObsoleteVersionSequenceId = data.SourceEntity?.VersionSequence ?? data.ObsoleteVersionSequenceId;

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
                    (x.RelationshipTypeKey == y.RelationshipTypeKey ||  x.RelationshipType?.Mnemonic == y.RelationshipType?.Mnemonic);
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
