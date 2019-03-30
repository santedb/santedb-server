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
using SanteDB.Core;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
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
    /// Entity name persistence service
    /// </summary>
    public class EntityNamePersistenceService : IdentifiedPersistenceService<Core.Model.Entities.EntityName, DbEntityName>, IAdoAssociativePersistenceService
    {

        /// <summary>
        /// Convert data instance to model instance
        /// </summary>
        public override EntityName ToModelInstance(object dataInstance, DataContext context)
        {
            return base.ToModelInstance(dataInstance, context);
        }

        /// <summary>
        /// Get from source
        /// </summary>
        public IEnumerable GetFromSource(DataContext context, Guid id, decimal? versionSequenceId)
        {
            int tr = 0;
            var addrLookupQuery = context.CreateSqlStatement<DbEntityNameComponent>().SelectFrom(typeof(DbEntityNameComponent), typeof(DbEntityName), typeof(DbPhoneticValue))
                .InnerJoin<DbEntityName>(o => o.SourceKey, o => o.Key)
                .InnerJoin<DbPhoneticValue>(o => o.ValueSequenceId, o => o.SequenceId)
                .Where<DbEntityName>(o => o.SourceKey == id && o.ObsoleteVersionSequenceId == null);

            /// Yowza! But it appears to be faster than the other way 
            return this.DomainQueryInternal<CompositeResult<DbEntityNameComponent, DbEntityName, DbPhoneticValue>>(context, addrLookupQuery)
                .GroupBy(o => o.Object2.Key)
                .Select(o =>
                    new EntityName()
                    {
                        NameUseKey = o.FirstOrDefault().Object2.UseConceptKey,
                        EffectiveVersionSequenceId = o.FirstOrDefault().Object2.EffectiveVersionSequenceId,
                        Key = o.Key,
                        LoadState = Core.Model.LoadState.PartialLoad,
                        ObsoleteVersionSequenceId = o.FirstOrDefault().Object2.ObsoleteVersionSequenceId,
                        SourceEntityKey = o.FirstOrDefault().Object2.SourceKey,
                        Component = o.Select(c => new EntityNameComponent()
                        {
                            ComponentTypeKey = c.Object1.ComponentTypeKey,
                            Key = c.Object1.Key,
                            LoadState = Core.Model.LoadState.FullLoad,
                            SourceEntityKey = c.Object2.Key,
                            Value = c.Object3.Value,
                            PhoneticAlgorithmKey = c.Object3.PhoneticAlgorithmKey,
                            PhoneticCode = c.Object3.PhoneticCode
                        }).ToList()
                    });

        }

        /// <summary>
        /// Insert the specified object
        /// </summary>
        public override Core.Model.Entities.EntityName InsertInternal(DataContext context, Core.Model.Entities.EntityName data)
        {
            // Ensure exists
            if (data.NameUse != null) data.NameUse = data.NameUse?.EnsureExists(context) as Concept;
            data.NameUseKey = data.NameUse?.Key ?? data.NameUseKey;
            var retVal = base.InsertInternal(context, data);

            // Data component
            if (data.Component != null)
                base.UpdateAssociatedItems<Core.Model.Entities.EntityNameComponent, DbEntityNameComponent>(
                   data.Component,
                    data,
                    context);

            return retVal;
        }

        /// <summary>
        /// Update the entity name
        /// </summary>
        public override Core.Model.Entities.EntityName UpdateInternal(DataContext context, Core.Model.Entities.EntityName data)
        {
            // Ensure exists
            if (data.NameUse != null) data.NameUse = data.NameUse?.EnsureExists(context) as Concept;

            data.NameUseKey = data.NameUse?.Key ?? data.NameUseKey;

            var retVal = base.UpdateInternal(context, data);

            var sourceKey = data.Key.Value.ToByteArray();

            // Data component
            if (data.Component != null)
                base.UpdateAssociatedItems<Core.Model.Entities.EntityNameComponent, DbEntityNameComponent>(
                   data.Component,
                    data,
                    context);

            return retVal;
        }

    }

    /// <summary>
    /// Represents an entity name component persistence service
    /// </summary>
    public class EntityNameComponentPersistenceService : IdentifiedPersistenceService<Core.Model.Entities.EntityNameComponent, DbEntityNameComponent, CompositeResult<DbEntityNameComponent, DbPhoneticValue>>, IAdoAssociativePersistenceService
    {

        /// <summary>
        /// Append order by
        /// </summary>
        protected override SqlStatement AppendOrderBy(SqlStatement rawQuery, ModelSort<EntityNameComponent>[] orderBy)
        {
            rawQuery = base.AppendOrderBy(rawQuery, orderBy);            
            return rawQuery.OrderBy<DbEntityNameComponent>(o => o.Sequence);
        }

        /// <summary>
        /// From model instance
        /// </summary>
        public override object FromModelInstance(Core.Model.Entities.EntityNameComponent modelInstance, DataContext context)
        {
            var retVal = base.FromModelInstance(modelInstance, context) as DbEntityNameComponent;

            // Duplicate name?
            var existing = context.FirstOrDefault<DbPhoneticValue>(o => o.Value == modelInstance.Value);
            if (existing != null && existing.SequenceId != retVal.ValueSequenceId)
                retVal.ValueSequenceId = existing.SequenceId.Value;
            else if (existing == null)
            {
                var phoneticCoder = ApplicationServiceContext.Current.GetService<IPhoneticAlgorithmHandler>();
                var value = context.Insert(new DbPhoneticValue()
                {
                    Value = modelInstance.Value,
                    PhoneticAlgorithmKey = phoneticCoder?.AlgorithmId ?? PhoneticAlgorithmKeys.None,
                    PhoneticCode = phoneticCoder?.GenerateCode(modelInstance.Value)
                });
                retVal.ValueSequenceId = value.SequenceId.Value;
            }
            else System.Diagnostics.Debugger.Break();

            return retVal;
        }

        /// <summary>
        /// Convert to model instance
        /// </summary>
        public override EntityNameComponent ToModelInstance(object dataInstance, DataContext context)
        {
            if (dataInstance == null) return null;

            var nameComp = (dataInstance as CompositeResult)?.Values.OfType<DbEntityNameComponent>().FirstOrDefault() ?? dataInstance as DbEntityNameComponent;
            var nameValue = (dataInstance as CompositeResult)?.Values.OfType<DbPhoneticValue>().FirstOrDefault();
            if(nameValue == null)
                nameValue = context.FirstOrDefault<DbPhoneticValue>(o => o.SequenceId == nameComp.ValueSequenceId);
            return new EntityNameComponent()
            {
                ComponentTypeKey = nameComp.ComponentTypeKey,
                PhoneticAlgorithmKey = nameValue.PhoneticAlgorithmKey,
                PhoneticCode = nameValue.PhoneticCode,
                Value = nameValue.Value,
                Key = nameComp.Key,
                SourceEntityKey = nameComp.SourceKey
            };
        }

        /// <summary>
        /// Insert context
        /// </summary>
        public override Core.Model.Entities.EntityNameComponent InsertInternal(DataContext context, Core.Model.Entities.EntityNameComponent data)
        {
            if (data.ComponentType != null) data.ComponentType = data.ComponentType?.EnsureExists(context) as Concept;
            data.ComponentTypeKey = data.ComponentType?.Key ?? data.ComponentTypeKey;
            return base.InsertInternal(context, data);
        }

        /// <summary>
        /// Update
        /// </summary>
        public override Core.Model.Entities.EntityNameComponent UpdateInternal(DataContext context, Core.Model.Entities.EntityNameComponent data)
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
            return this.QueryInternal(context, base.BuildSourceQuery<EntityNameComponent>(id), Guid.Empty, 0, null, out tr, null, false).ToList();
        }
    }
}
