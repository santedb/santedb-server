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
using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data.Model;
using SanteDB.Persistence.Data.ADO.Exceptions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Generic persistnece service for identified entities
    /// </summary>
    public abstract class IdentifiedPersistenceService<TModel, TDomain> : IdentifiedPersistenceService<TModel, TDomain, TDomain>
        where TModel : IdentifiedData, new()
        where TDomain : class, IDbIdentified, new()
    { }

    /// <summary>
    /// Generic persistence service which can persist between two simple types.
    /// </summary>
    public abstract class IdentifiedPersistenceService<TModel, TDomain, TQueryResult> : CorePersistenceService<TModel, TDomain, TQueryResult>
        where TModel : IdentifiedData, new()
        where TDomain : class, IDbIdentified, new()
    {

        #region implemented abstract members of LocalDataPersistenceService

        /// <summary>
        /// Return true if the specified object exists
        /// </summary>
        public override bool Exists(DataContext context, Guid key)
        {
            return context.Any<TDomain>(o => o.Key == key);
        }

        /// <summary>
        /// Performthe actual insert.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel InsertInternal(DataContext context, TModel data)
        {
            try
            {
                var domainObject = this.FromModelInstance(data, context) as TDomain;

                domainObject = context.Insert<TDomain>(domainObject);
                data.Key = domainObject.Key;

                return data;
            }
            catch (DbException ex)
            {
                this.m_tracer.TraceEvent(EventLevel.Error,  "Error inserting {0} - {1}", data, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Perform the actual update.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel UpdateInternal(DataContext context, TModel data)
        {
            try
            {
                // Sanity 
                if (data.Key == Guid.Empty)
                    throw new AdoFormalConstraintException(AdoFormalConstraintType.NonIdentityUpdate);

                // Map and copy
                var newDomainObject = this.FromModelInstance(data, context) as TDomain;
                var oldDomainObject = context.SingleOrDefault<TDomain>(o => o.Key == newDomainObject.Key);
                if (oldDomainObject == null)
                    throw new KeyNotFoundException(data.Key.ToString());

                // Is this an un-delete?
                if (oldDomainObject is IDbVersionedAssociation dba && dba.ObsoleteVersionSequenceId.HasValue &&
                    newDomainObject is IDbVersionedAssociation dbb && !dbb.ObsoleteVersionSequenceId.HasValue)
                {
                    dba.ObsoleteVersionSequenceId = dbb.ObsoleteVersionSequenceId;
                    dba.ObsoleteVersionSequenceIdSpecified = true;
                }

                oldDomainObject.CopyObjectData(newDomainObject);
                context.Update<TDomain>(oldDomainObject);
                return data;
            }
            catch (DbException ex)
            {
                this.m_tracer.TraceEvent(EventLevel.Error,  "Error updating {0} - {1}", data, ex.Message);
                throw;
            }

        }

        /// <summary>
        /// Performs the actual obsoletion
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel ObsoleteInternal(DataContext context, TModel data)
        {
            try
            {
                if (data.Key == Guid.Empty)
                    throw new AdoFormalConstraintException(AdoFormalConstraintType.NonIdentityUpdate);

                var domainObject = context.FirstOrDefault<TDomain>(o => o.Key == data.Key);

                if (domainObject == null)
                    throw new KeyNotFoundException(data.Key.ToString());

                context.Delete(domainObject);

                return data;
            }
            catch (DbException ex)
            {
                this.m_tracer.TraceEvent(EventLevel.Error,  "Error obsoleting {0} - {1}", data, ex.Message);
                throw;
            }

        }

        /// <summary>
        /// Performs the actual query
        /// </summary>
        public override IEnumerable<TModel> QueryInternal(DataContext context, Expression<Func<TModel, bool>> query, Guid queryId, int offset, int? count, out int totalResults, ModelSort<TModel>[] orderBy, bool countResults = false)
        {
            return this.DoQueryInternal(context, query, queryId, offset, count, out totalResults, orderBy, countResults).ToList().Select(o => o is Guid ? this.Get(context, (Guid)o) : this.CacheConvert(o, context));
        }

        /// <summary>
        /// Get the specified object
        /// </summary>
        internal override TModel Get(DataContext context, Guid key)
        {
            var cacheService = new AdoPersistenceCache(context);
            var retVal = cacheService?.GetCacheItem<TModel>(key);
            if (retVal != null)
                return retVal;
            else
                return this.CacheConvert(context.FirstOrDefault<TDomain>(o => o.Key == key), context);
        }

        #endregion
    }
}

