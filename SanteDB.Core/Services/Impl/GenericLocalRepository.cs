/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: justin
 * Date: 2018-6-22
 */
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Represents a base class for entity repository services
    /// </summary>
    public class GenericLocalRepository<TEntity> :
        IValidatingRepositoryService<TEntity>,
        IRepositoryService<TEntity>,
        IPersistableQueryRepositoryService<TEntity>,
        IFastQueryRepositoryService<TEntity>,
        INotifyRepositoryService<TEntity>,
        ISecuredRepositoryService
        where TEntity : IdentifiedData
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => $"Local repository service for {typeof(TEntity).FullName}";

        /// <summary>
        /// Trace source
        /// </summary>
        protected TraceSource m_traceSource = new TraceSource(SanteDBConstants.DataTraceSourceName);

        /// <summary>
        /// Fired after data is inserted
        /// </summary>
        public event EventHandler<RepositoryEventArgs<TEntity>> Inserted;
        /// <summary>
        /// Fired after data is saved
        /// </summary>
        public event EventHandler<RepositoryEventArgs<TEntity>> Saved;
        /// <summary>
        /// Fired after data was retrieved
        /// </summary>
        public event EventHandler<RepositoryEventArgs<TEntity>> Retrieved;
        /// <summary>
        /// Fired after data was queried
        /// </summary>
        public event EventHandler<RepositoryEventArgs<IEnumerable<TEntity>>> Queried;

        /// <summary>
        /// Gets the policy required for querying
        /// </summary>
        protected virtual String QueryPolicy => PermissionPolicyIdentifiers.LoginAsService;
        /// <summary>
        /// Gets the policy required for reading
        /// </summary>
        protected virtual String ReadPolicy => PermissionPolicyIdentifiers.LoginAsService;
        /// <summary>
        /// Gets the policy required for writing
        /// </summary>
        protected virtual String WritePolicy => PermissionPolicyIdentifiers.LoginAsService;
        /// <summary>
        /// Gets the policy required for deleting
        /// </summary>
        protected virtual String DeletePolicy => PermissionPolicyIdentifiers.LoginAsService;
        /// <summary>
        /// Gets the policy for altering
        /// </summary>
        protected virtual String AlterPolicy => PermissionPolicyIdentifiers.LoginAsService;

        /// <summary>
        /// Find with stored query parameters
        /// </summary>
        public virtual IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> query, int offset, int? count, out int totalResults, Guid queryId, params ModelSort<TEntity>[] orderBy)
        {

            // Demand permission
            this.DemandQuery();

            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TEntity>>();

            if (persistenceService == null)
            {
                throw new InvalidOperationException($"Unable to locate {typeof(IDataPersistenceService<TEntity>).FullName}");
            }

            var businessRulesService = ApplicationServiceContext.Current.GetBusinessRulesService<TEntity>();

            IEnumerable<TEntity> results = null;
            if (queryId != Guid.Empty && persistenceService is IStoredQueryDataPersistenceService<TEntity>)
                results = (persistenceService as IStoredQueryDataPersistenceService<TEntity>).Query(query, queryId, offset, count,  out totalResults, AuthenticationContext.Current.Principal, orderBy);
            else
                results = persistenceService.Query(query, offset, count,  out totalResults, AuthenticationContext.Current.Principal, orderBy);

            var retVal = businessRulesService != null ? businessRulesService.AfterQuery(results) : results;
            this.Queried?.Invoke(this, new RepositoryEventArgs<IEnumerable<TEntity>>(retVal));
            return retVal;
        }

        /// <summary>
        /// Performs insert of object
        /// </summary>
        public virtual TEntity Insert(TEntity data)
        {
            // Demand permission
            this.DemandWrite(data);

            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TEntity>>();

            if (persistenceService == null)
            {
                throw new InvalidOperationException($"Unable to locate {nameof(IDataPersistenceService<TEntity>)}");
            }

            data = this.Validate(data);

            var businessRulesService = ApplicationServiceContext.Current.GetBusinessRulesService<TEntity>();

            data = businessRulesService?.BeforeInsert(data) ?? data;

            data = persistenceService.Insert(data, TransactionMode.Commit, AuthenticationContext.Current.Principal);

            businessRulesService?.AfterInsert(data);

            this.Inserted?.Invoke(this, new RepositoryEventArgs<TEntity>(data));
            return data;
        }

        /// <summary>
        /// Obsolete the specified data
        /// </summary>
        public virtual TEntity Obsolete(Guid key) 
        {
            // Demand permission
            this.DemandDelete(key);

            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TEntity>>();

            if (persistenceService == null)
            {
                throw new InvalidOperationException($"Unable to locate {nameof(IDataPersistenceService<TEntity>)}");
            }

            var entity = persistenceService.Get(key, null,  true, AuthenticationContext.Current.Principal);

            if (entity == null)
            {
                throw new KeyNotFoundException($"Entity {key} not found");
            }

            var businessRulesService = ApplicationServiceContext.Current.GetBusinessRulesService<TEntity>();

            entity = businessRulesService?.BeforeObsolete(entity) ?? entity;
            entity = persistenceService.Obsolete(entity, TransactionMode.Commit, AuthenticationContext.Current.Principal);
            entity = businessRulesService?.AfterObsolete(entity) ?? entity;

            this.Inserted?.Invoke(this, new RepositoryEventArgs<TEntity>(entity));

            return entity;
        }

        /// <summary>
        /// Get the specified key
        /// </summary>
        public virtual TEntity Get(Guid key)
        {
            return this.Get(key, Guid.Empty);
        }

        /// <summary>
        /// Get specified data from persistence
        /// </summary>
        public virtual TEntity Get(Guid key, Guid versionKey) 
        {
            // Demand permission
            this.DemandRead(key);
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TEntity>>();

            if (persistenceService == null)
            {
                throw new InvalidOperationException($"Unable to locate {nameof(IDataPersistenceService<TEntity>)}");
            }

            var businessRulesService = ApplicationServiceContext.Current.GetBusinessRulesService<TEntity>();

            var result = persistenceService.Get(key, versionKey,  true, AuthenticationContext.Current.Principal);

            var retVal = businessRulesService?.AfterRetrieve(result) ?? result;
            this.Retrieved?.Invoke(this, new RepositoryEventArgs<TEntity>(retVal));
            return retVal;
        }

        /// <summary>
        /// Save the specified entity (insert or update)
        /// </summary>
        public virtual TEntity Save(TEntity data)
        {
            // Demand permission
            this.DemandAlter(data);

            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TEntity>>();

            if (persistenceService == null)
            {
                throw new InvalidOperationException($"Unable to locate {nameof(IDataPersistenceService<TEntity>)}");
            }

            data=this.Validate(data);

            var businessRulesService = ApplicationServiceContext.Current.GetBusinessRulesService<TEntity>();

            try
            {
                data = businessRulesService?.BeforeUpdate(data) ?? data;
                data = persistenceService.Update(data, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                businessRulesService?.AfterUpdate(data);
                this.Saved?.Invoke(this, new RepositoryEventArgs<TEntity>(data));
                return data;
            }
            catch (KeyNotFoundException)
            {
                data = businessRulesService?.BeforeInsert(data) ?? data;
                data = persistenceService.Insert(data, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                businessRulesService?.AfterInsert(data);
                this.Saved?.Invoke(this, new RepositoryEventArgs<TEntity>(data));
                return data;
            }
        }

        /// <summary>
        /// Validate a patient before saving
        /// </summary>
        public virtual TEntity Validate(TEntity p) 
        {
            p = (TEntity)p.Clean(); // clean up messy data

            var businessRulesService = ApplicationServiceContext.Current.GetBusinessRulesService<TEntity>();

            var details = businessRulesService?.Validate(p) ?? new List<DetectedIssue>();

            if (details.Any(d => d.Priority == DetectedIssuePriorityType.Error))
            {
                throw new DetectedIssueException(details);
            }

            // Bundles cascade
            var bundle = p as Bundle;
            if (bundle != null)
            {
                for (int i = 0; i < bundle.Item.Count; i++)
                {
                    var itm = bundle.Item[i];
                    var vrst = typeof(IValidatingRepositoryService<>).MakeGenericType(itm.GetType());
                    var vrsi = ApplicationServiceContext.Current.GetService(vrst);
                    
                    if (vrsi != null)
                        bundle.Item[i] = vrsi.GetType().GetMethod(nameof(Validate)).Invoke(vrsi, new object[] { itm }) as IdentifiedData;
                }
            }
            return p;
        }

        /// <summary>
        /// Perform a faster version of the query for an object
        /// </summary>
        public virtual IEnumerable<TEntity> FindFast(Expression<Func<TEntity, bool>> query, int offset, int? count, out int totalResults, Guid queryId) 
        {
            // Demand permission
            this.DemandQuery();

            var persistenceService = ApplicationServiceContext.Current.GetService<IFastQueryDataPersistenceService<TEntity>>();

            if (persistenceService == null)
            {
                return this.Find(query, offset, count, out totalResults, queryId);
            }

            var businessRulesService = ApplicationServiceContext.Current.GetBusinessRulesService<TEntity>();

            IEnumerable<TEntity> results = null;
            results = persistenceService.QueryFast(query, queryId, offset, count, out totalResults);

            results = businessRulesService != null ? businessRulesService.AfterQuery(results) : results;
            this.Queried?.Invoke(this, new RepositoryEventArgs<IEnumerable<TEntity>>(results));
            return results;
        }

        /// <summary>
        /// Perform a simple find
        /// </summary>
        public virtual IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> query)
        {
            int t = 0;
            return this.Find(query, 0, null, out t, Guid.Empty);
        }

        /// <summary>
        /// Perform a normal find
        /// </summary>
        public virtual IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> query, int offset, int? count, out int totalResults, params ModelSort<TEntity>[] orderBy)
        {
            return this.Find(query, offset, count, out totalResults, Guid.Empty, orderBy);
        }

        /// <summary>
        /// Demand write permission
        /// </summary>
        public virtual void DemandWrite(object data)
        {
            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, this.WritePolicy).Demand();
        }

        /// <summary>
        /// Demand read
        /// </summary>
        /// <param name="key"></param>
        public virtual void DemandRead(Guid key)
        {
            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, this.ReadPolicy).Demand();
        }

        /// <summary>
        /// Demand delete permission
        /// </summary>
        public virtual void DemandDelete(Guid key)
        {
            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, this.DeletePolicy).Demand();
        }

        /// <summary>
        /// Demand alter permission
        /// </summary>
        public virtual void DemandAlter(object data)
        {
            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, this.AlterPolicy).Demand();
        }

        /// <summary>
        /// Demand query 
        /// </summary>
        public virtual void DemandQuery()
        {
            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, this.QueryPolicy).Demand();
        }
    }
}