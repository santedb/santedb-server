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
using System.Linq;


namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Represents a persistence service for entity addresses
    /// </summary>
    public class EntityAddressPersistenceService : IdentifiedPersistenceService<Core.Model.Entities.EntityAddress, DbEntityAddress>, IAdoAssociativePersistenceService
    {

        /// <summary>
        /// Convert data instance to model instance
        /// </summary>
        public override EntityAddress ToModelInstance(object dataInstance, DataContext context)
        {
            return base.ToModelInstance(dataInstance, context);
        }

        /// <summary>
        /// Get addresses from source
        /// </summary>
        public IEnumerable GetFromSource(DataContext context, Guid id, decimal? versionSequenceId)
        {
            int tr = 0;
            var addrLookupQuery = context.CreateSqlStatement<DbEntityAddressComponent>().SelectFrom(typeof(DbEntityAddressComponent), typeof(DbEntityAddress), typeof(DbEntityAddressComponentValue))
                .InnerJoin<DbEntityAddress>(o=>o.SourceKey, o=>o.Key)
                .InnerJoin<DbEntityAddressComponentValue>(o=>o.ValueSequenceId, o=>o.SequenceId)
                .Where<DbEntityAddress>(o=>o.SourceKey == id && o.ObsoleteVersionSequenceId == null);

            /// Yowza! But it appears to be faster than the other way 
            return this.DomainQueryInternal<CompositeResult<DbEntityAddressComponent, DbEntityAddress, DbEntityAddressComponentValue>>(context, addrLookupQuery, ref tr)
                .GroupBy(o=>o.Object2.Key)
                .Select(o =>
                    new EntityAddress()
                    {
                        AddressUseKey = o.FirstOrDefault().Object2.UseConceptKey,
                        EffectiveVersionSequenceId = o.FirstOrDefault().Object2.EffectiveVersionSequenceId,
                        Key = o.Key,
                        LoadState = Core.Model.LoadState.PartialLoad,
                        ObsoleteVersionSequenceId = o.FirstOrDefault().Object2.ObsoleteVersionSequenceId,
                        SourceEntityKey = o.FirstOrDefault().Object2.SourceKey,
                        Component = o.Select(c=>new EntityAddressComponent()
                        {
                            ComponentTypeKey = c.Object1.ComponentTypeKey,
                            Key = c.Object1.Key,
                            LoadState = Core.Model.LoadState.FullLoad,
                            SourceEntityKey = c.Object2.Key,
                            Value = c.Object3.Value
                        }).ToList()
                    });

        }

        /// <summary>
        /// Insert the specified object
        /// </summary>
        public override Core.Model.Entities.EntityAddress InsertInternal(DataContext context, EntityAddress data)
        {

            // Ensure exists
            if (data.AddressUse != null) data.AddressUse = data.AddressUse?.EnsureExists(context) as Concept;
            data.AddressUseKey = data.AddressUse?.Key ?? data.AddressUseKey;

            var retVal = base.InsertInternal(context, data);

            // Data component
            if (data.Component != null)
                base.UpdateAssociatedItems<Core.Model.Entities.EntityAddressComponent, DbEntityAddressComponent>(
                   data.Component,
                    data,
                    context);

            return retVal;
        }

        /// <summary>
        /// Update the entity name
        /// </summary>
        public override Core.Model.Entities.EntityAddress UpdateInternal(DataContext context, Core.Model.Entities.EntityAddress data)
        {

            // Ensure exists
            if (data.AddressUse != null) data.AddressUse = data.AddressUse?.EnsureExists(context) as Concept;
            data.AddressUseKey = data.AddressUse?.Key ?? data.AddressUseKey;

            var retVal = base.UpdateInternal(context, data);

            var sourceKey = data.Key.Value.ToByteArray();

            // Data component
            if (data.Component != null && data.Component.Count > 0)
                base.UpdateAssociatedItems<Core.Model.Entities.EntityAddressComponent, DbEntityAddressComponent>(
                   data.Component,
                    data,
                    context);

            return retVal;
        }


    }

    /// <summary>
    /// Entity address component persistence service
    /// </summary>
    public class EntityAddressComponentPersistenceService : IdentifiedPersistenceService<Core.Model.Entities.EntityAddressComponent, DbEntityAddressComponent, CompositeResult<DbEntityAddressComponent, DbEntityAddressComponentValue>>, IAdoAssociativePersistenceService
    {

        /// <summary>
        /// To model instance
        /// </summary>
        public override EntityAddressComponent ToModelInstance(object dataInstance, DataContext context)
        {
            if (dataInstance == null) return null;

            var addrComp = (dataInstance as CompositeResult)?.Values.OfType<DbEntityAddressComponent>().FirstOrDefault() ?? dataInstance as DbEntityAddressComponent;
            var addrValue = (dataInstance as CompositeResult)?.Values.OfType<DbEntityAddressComponentValue>().FirstOrDefault();
            if (addrValue == null)
                addrValue = context.FirstOrDefault<DbEntityAddressComponentValue>(o => o.SequenceId == addrComp.ValueSequenceId);

            return new EntityAddressComponent()
            {
                ComponentTypeKey = addrComp.ComponentTypeKey,
                Value = addrValue.Value,
                Key = addrComp.Key,
                SourceEntityKey = addrComp.SourceKey
            };
        }

        /// <summary>
        /// From the model instance
        /// </summary>
        public override object FromModelInstance(Core.Model.Entities.EntityAddressComponent modelInstance, DataContext context)
        {
            var retVal = base.FromModelInstance(modelInstance, context) as DbEntityAddressComponent;

            // Address component already exists?
            var existing = context.FirstOrDefault<DbEntityAddressComponentValue>(o => o.Value == modelInstance.Value);
            if (existing != null && existing.SequenceId != retVal.ValueSequenceId)
                retVal.ValueSequenceId = existing.SequenceId.Value;
            else
                retVal.ValueSequenceId = context.Insert(new DbEntityAddressComponentValue()
                {
                    Value = modelInstance.Value
                }).SequenceId.Value;
            return retVal;
        }

        /// <summary>
        /// Entity address component
        /// </summary>
        public override Core.Model.Entities.EntityAddressComponent InsertInternal(DataContext context, Core.Model.Entities.EntityAddressComponent data)
        {
            if (data.ComponentType != null) data.ComponentType = data.ComponentType?.EnsureExists(context) as Concept;
            data.ComponentTypeKey = data.ComponentType?.Key ?? data.ComponentTypeKey;
            return base.InsertInternal(context, data);
        }

        /// <summary>
        /// Update 
        /// </summary>
        public override Core.Model.Entities.EntityAddressComponent UpdateInternal(DataContext context, Core.Model.Entities.EntityAddressComponent data)
        {
            if (data.ComponentType != null) data.ComponentType = data.ComponentType?.EnsureExists(context) as Concept;

            data.ComponentTypeKey = data.ComponentType?.Key ?? data.ComponentTypeKey;
            return base.UpdateInternal(context, data);
        }

        /// <summary>
        /// Get components from source
        /// </summary>
        public IEnumerable GetFromSource(DataContext context, Guid id, decimal? versionSequenceId)
        {
            int tr = 0;
            return this.QueryInternal(context, base.BuildSourceQuery<EntityAddressComponent>(id), Guid.Empty, 0, null, out tr, null, false).ToList();
        }

    }
}
