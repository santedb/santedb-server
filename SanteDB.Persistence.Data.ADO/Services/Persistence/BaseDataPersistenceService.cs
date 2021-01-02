/*
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
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model;
using SanteDB.Persistence.Data.ADO.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Base persistence service
    /// </summary>
    public abstract class BaseDataPersistenceService<TModel, TDomain> : BaseDataPersistenceService<TModel, TDomain, TDomain>
        where TModel : BaseEntityData, new()
        where TDomain : class, IDbBaseData, new()
    {
        public BaseDataPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }
    }

    /// <summary>
    /// Base data persistence service
    /// </summary>
    public abstract class BaseDataPersistenceService<TModel, TDomain, TQueryResult> : IdentifiedPersistenceService<TModel, TDomain, TQueryResult>, IBulkDataPersistenceService
        where TModel : BaseEntityData, new()
        where TDomain : class, IDbBaseData, new()
    {

        public BaseDataPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// Performthe actual insert.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel InsertInternal(DataContext context, TModel data)
        {
            if (data.CreatedBy != null) data.CreatedBy = data.CreatedBy?.EnsureExists(context) as SecurityProvenance;

            // HACK: For now, modified on can only come from one property, some non-versioned data elements are bound on UpdatedTime
            var nvd = data as NonVersionedEntityData;
            if (nvd != null)
            {
                nvd.UpdatedByKey = context.ContextId;
                nvd.UpdatedTime = DateTimeOffset.Now;
            }

            if (data.CreationTime == DateTimeOffset.MinValue || data.CreationTime.Year < 100)
                data.CreationTime = DateTimeOffset.Now;

            var domainObject = this.FromModelInstance(data, context) as TDomain;

            // Ensure created by exists
            data.CreatedByKey = domainObject.CreatedByKey = context.ContextId;
            domainObject = context.Insert<TDomain>(domainObject);
            data.CreationTime = (DateTimeOffset)domainObject.CreationTime;
            data.Key = domainObject.Key;
            return data;

        }

        /// <summary>
        /// Perform the actual update.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel UpdateInternal(DataContext context, TModel data)
        {
            var nvd = data as NonVersionedEntityData;
            if (nvd != null)
            {
                nvd.UpdatedByKey = context.ContextId;
            }

            // Check for key
            if (data.Key == Guid.Empty)
                throw new AdoFormalConstraintException(AdoFormalConstraintType.NonIdentityUpdate);

            // Get current object
            var domainObject = this.FromModelInstance(data, context) as TDomain;
            var currentObject = context.FirstOrDefault<TDomain>(o => o.Key == data.Key);
            // Not found
            if (currentObject == null)
                throw new KeyNotFoundException(data.Key.ToString());

            // VObject
            var vobject = domainObject as IDbNonVersionedBaseData;
            if (vobject != null)
            {
                nvd.UpdatedByKey = context.ContextId;
                nvd.UpdatedTime = vobject.UpdatedTime = DateTimeOffset.Now;
            }

            //if (currentObject.CreationTime == domainObject.CreationTime) // HACK: Someone keeps passing up the same data so we have to correct here
            //    domainObject.CreationTime = DateTimeOffset.Now;

            if (currentObject.ObsoletedByKey.HasValue && !domainObject.ObsoletedByKey.HasValue) // We are un-deleting
            {
                currentObject.ObsoletedByKey = null;
                currentObject.ObsoletionTime = null;
                domainObject.ObsoletedByKeySpecified = domainObject.ObsoletionTimeSpecified = true;
            }

            currentObject.CopyObjectData(domainObject);
            currentObject = context.Update<TDomain>(currentObject);

            return data;
        }

        /// <summary>
        /// Query the specified object ordering by creation time
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<TModel> QueryInternal(DataContext context, Expression<Func<TModel, bool>> query, Guid queryId, int offset, int? count, out int totalResults, ModelSort<TModel>[] orderBy, bool countResults = true)
        {
            var qresult = this.DoQueryInternal(context, query, queryId, offset, count, out totalResults, orderBy, countResults).ToList();
            return qresult.Select(o => o is Guid ? this.Get(context, (Guid)o) : this.CacheConvert(o, context)).ToList();
        }

        /// <summary>
        /// Performs the actual obsoletion
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel ObsoleteInternal(DataContext context, TModel data)
        {
            if (data.Key == Guid.Empty)
                throw new AdoFormalConstraintException(AdoFormalConstraintType.NonIdentityUpdate);

            data.ObsoletedByKey = context.ContextId;

            // Current object
            var currentObject = context.FirstOrDefault<TDomain>(o => o.Key == data.Key);
            if (currentObject == null)
                throw new KeyNotFoundException(data.Key.ToString());

            //data.ObsoletedBy?.EnsureExists(context);
            data.ObsoletedByKey = currentObject.ObsoletedByKey = context.ContextId;
            data.ObsoletionTime = currentObject.ObsoletionTime = currentObject.ObsoletionTime ?? DateTimeOffset.Now;

            context.Update(currentObject);
            return data;
        }

        /// <summary>
        /// Perform the bulk obsoletion operation
        /// </summary>
        protected override void BulkObsoleteInternal(DataContext context, Guid[] keysToObsolete)
        {
            // By default we're just going to set obsoletion time
            foreach (var itm in context.Query<TDomain>(o => keysToObsolete.Contains(o.Key)))
            {
                itm.ObsoletionTime = DateTimeOffset.Now;
                itm.ObsoletedByKey = context.ContextId;
                context.Update(itm);
            }
        }



    }
}

