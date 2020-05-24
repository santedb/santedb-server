/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2019-11-27
 */
using SanteDB.Core.Model.Acts;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data.Model;
using SanteDB.Persistence.Data.ADO.Data.Model.Acts;
namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Represents a persistence service which is derived from an act
    /// </summary>
    public abstract class ActDerivedPersistenceService<TModel, TData> : ActDerivedPersistenceService<TModel, TData, CompositeResult<TData, DbActVersion, DbAct>>
        where TModel : Core.Model.Acts.Act, new()
        where TData : DbActSubTable, new()
    { }

    /// <summary>
    /// Represents a persistence service which is derived from an act
    /// </summary>
    public abstract class ActDerivedPersistenceService<TModel, TData, TQueryReturn> : SimpleVersionedEntityPersistenceService<TModel, TData, TQueryReturn, DbActVersion>
        where TModel : Core.Model.Acts.Act, new()
        where TData : DbActSubTable, new()
        where TQueryReturn : CompositeResult
    {
        // act persister
        protected ActPersistenceService m_actPersister = new ActPersistenceService();


        /// <summary>
        /// From model instance
        /// </summary>
        public override object FromModelInstance(TModel modelInstance, DataContext context)
        {
            var retVal = base.FromModelInstance(modelInstance, context);
            (retVal as DbActSubTable).ParentKey = modelInstance.VersionKey.Value;
            return retVal;
        }

        /// <summary>
        /// Entity model instance
        /// </summary>
        public override sealed TModel ToModelInstance(object dataInstance, DataContext context)
        {
            return (TModel)this.m_actPersister.ToModelInstance(dataInstance, context);
        }

        /// <summary>
        /// Insert the specified TModel into the database
        /// </summary>
        public override TModel InsertInternal(DataContext context, TModel data)
        {
            if (typeof(TModel).BaseType == typeof(Act))
            {
                var inserted = this.m_actPersister.InsertCoreProperties(context, data);
                data.Key = inserted.Key;
            }
            return base.InsertInternal(context, data);
        }

        /// <summary>
        /// Update the specified TModel
        /// </summary>
        public override TModel UpdateInternal(DataContext context, TModel data)
        {
            if (typeof(TModel).BaseType == typeof(Act))
                this.m_actPersister.UpdateCoreProperties(context, data);
            return base.InsertInternal(context, data);
        }

        /// <summary>
        /// Obsolete the object
        /// </summary>
        public override TModel ObsoleteInternal(DataContext context, TModel data)
        {
            var retVal = this.m_actPersister.ObsoleteInternal(context, data);
            return base.InsertInternal(context, data);
        }

    }
}
