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
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Event;
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
    [ServiceProvider("Local Repository Service", Dependencies = new Type[] { typeof(IDataPersistenceService) })]
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
        protected Tracer m_traceSource = new Tracer(SanteDBConstants.DataTraceSourceName);

        /// <summary>
        /// Fired prior to inserting the record
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<TEntity>> Inserting;
        /// <summary>
        /// Fired after the record has been inserted
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<TEntity>> Inserted;
        /// <summary>
        /// Fired before the record is saved
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<TEntity>> Saving;
        /// <summary>
        /// Fired after the record has been persisted
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<TEntity>> Saved;
        /// <summary>
        /// Fired prior to the record being retrieved
        /// </summary>
        public event EventHandler<DataRetrievingEventArgs<TEntity>> Retrieving;
        /// <summary>
        /// Fired after the record has been retrieved
        /// </summary>
        public event EventHandler<DataRetrievedEventArgs<TEntity>> Retrieved;
        /// <summary>
        /// Fired before a query is executed
        /// </summary>
        public event EventHandler<QueryRequestEventArgs<TEntity>> Querying;
        /// <summary>
        /// Fired after query results have been executed
        /// </summary>
        public event EventHandler<QueryResultEventArgs<TEntity>> Queried;
        /// <summary>
        /// Data is obsoleting
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<TEntity>> Obsoleting;
        /// <summary>
        /// Data has obsoleted
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<TEntity>> Obsoleted;

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

            // Notify query 
            var preQueryEventArgs = new QueryRequestEventArgs<TEntity>(query, offset, count, queryId, AuthenticationContext.Current.Principal);
            this.Querying?.Invoke(this, preQueryEventArgs);
            if (preQueryEventArgs.Cancel) /// Cancel the request
            {
                totalResults = preQueryEventArgs.TotalResults;
                return preQueryEventArgs.Results;
            }

            IEnumerable<TEntity> results = null;
            if (queryId != Guid.Empty && persistenceService is IStoredQueryDataPersistenceService<TEntity>)
                results = (persistenceService as IStoredQueryDataPersistenceService<TEntity>).Query(preQueryEventArgs.Query, preQueryEventArgs.QueryId.GetValueOrDefault(), preQueryEventArgs.Offset, preQueryEventArgs.Count, out totalResults, AuthenticationContext.Current.Principal, orderBy);
            else
                results = persistenceService.Query(preQueryEventArgs.Query, preQueryEventArgs.Offset, preQueryEventArgs.Count, out totalResults, AuthenticationContext.Current.Principal, orderBy);

            var retVal = businessRulesService != null ? businessRulesService.AfterQuery(results) : results;
            this.Queried?.Invoke(this, new QueryResultEventArgs<TEntity>(query, retVal, offset, count, totalResults, queryId, AuthenticationContext.Current.Principal));
            return retVal;
        }

        /// <summary>
        /// Performs insert of object
        /// </summary>
        public virtual TEntity Insert(TEntity data)
        {
            // Demand permission
            this.DemandWrite(data);

            // Validate the resource
            data = this.Validate(data);

            // Fire pre-persistence triggers
            var prePersistence = new DataPersistingEventArgs<TEntity>(data, AuthenticationContext.Current.Principal);
            this.Inserting?.Invoke(this, prePersistence);
            if (prePersistence.Cancel)
            {
                this.m_traceSource.TraceWarning("Pre-persistence event signal cancel: {0}", data);
                return prePersistence.Data;
            }
            // Did the pre-persistence service change the type to a batch
            var businessRulesService = ApplicationServiceContext.Current.GetBusinessRulesService<TEntity>();
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TEntity>>();
            data = businessRulesService?.BeforeInsert(data) ?? prePersistence.Data;
            data = persistenceService.Insert(data, TransactionMode.Commit, AuthenticationContext.Current.Principal);
            businessRulesService?.AfterInsert(data);
            this.Inserted?.Invoke(this, new DataPersistedEventArgs<TEntity>(data, AuthenticationContext.Current.Principal));

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

            var entity = persistenceService.Get(key, null, true, AuthenticationContext.Current.Principal);

            if (entity == null)
                throw new KeyNotFoundException($"Entity {key} not found");

            // Fire pre-persistence triggers
            var prePersistence = new DataPersistingEventArgs<TEntity>(entity, AuthenticationContext.Current.Principal);
            this.Obsoleting?.Invoke(this, prePersistence);
            if (prePersistence.Cancel)
            {
                this.m_traceSource.TraceWarning("Pre-persistence event signal cancel obsolete: {0}", key);
                return prePersistence.Data;
            }

            var businessRulesService = ApplicationServiceContext.Current.GetBusinessRulesService<TEntity>();

            entity = businessRulesService?.BeforeObsolete(entity) ?? entity;
            entity = persistenceService.Obsolete(entity, TransactionMode.Commit, AuthenticationContext.Current.Principal);
            entity = businessRulesService?.AfterObsolete(entity) ?? entity;

            this.Obsoleted?.Invoke(this, new DataPersistedEventArgs<TEntity>(entity, AuthenticationContext.Current.Principal));

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

            var preRetrieve = new DataRetrievingEventArgs<TEntity>(key, versionKey, AuthenticationContext.Current.Principal);
            this.Retrieving?.Invoke(this, preRetrieve);
            if(preRetrieve.Cancel)
            {
                this.m_traceSource.TraceWarning("Pre-retrieve trigger signals cancel: {0}", key);
                return preRetrieve.Result;
            }
            
            var result = persistenceService.Get(key, versionKey, true, AuthenticationContext.Current.Principal);
            var retVal = businessRulesService?.AfterRetrieve(result) ?? result;
            this.Retrieved?.Invoke(this, new DataRetrievedEventArgs<TEntity>(retVal, AuthenticationContext.Current.Principal));
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

            data = this.Validate(data);

            var businessRulesService = ApplicationServiceContext.Current.GetBusinessRulesService<TEntity>();

            try
            {
                var preSave = new DataPersistingEventArgs<TEntity>(data, AuthenticationContext.Current.Principal);
                this.Saving?.Invoke(this, preSave);
                if (preSave.Cancel)
                {
                    this.m_traceSource.TraceWarning("Persistence layer indicates pre-save cancel: {0}", data);
                    return preSave.Data;
                }
                else
                    data = preSave.Data; // Data may have been updated

                if (data.Key.HasValue && persistenceService.Get(data.Key.Value, null, true, AuthenticationContext.Current.Principal) != null)
                {
                    
                    data = businessRulesService?.BeforeUpdate(data) ?? data;
                    data = persistenceService.Update(data, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                    businessRulesService?.AfterUpdate(data);
                }
                else
                {
                    data = businessRulesService?.BeforeInsert(data) ?? data;
                    data = persistenceService.Insert(data, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                    businessRulesService?.AfterInsert(data);
                }

                this.Saved?.Invoke(this, new DataPersistedEventArgs<TEntity>(data, AuthenticationContext.Current.Principal));
                return data;
            }
            catch (KeyNotFoundException)
            {
                return this.Insert(data);
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

            // Notify query 
            var preQueryEventArgs = new QueryRequestEventArgs<TEntity>(query, offset, count, queryId, AuthenticationContext.Current.Principal);
            this.Querying?.Invoke(this, preQueryEventArgs);
            if (preQueryEventArgs.Cancel) /// Cancel the request
            {
                totalResults = preQueryEventArgs.TotalResults;
                return preQueryEventArgs.Results;
            }

            IEnumerable<TEntity> results = null;
            results = persistenceService.QueryFast(query, queryId, offset, count, out totalResults);

            results = businessRulesService != null ? businessRulesService.AfterQuery(results) : results;
            this.Queried?.Invoke(this, new QueryResultEventArgs<TEntity>(query, results, offset, count, totalResults, queryId, AuthenticationContext.Current.Principal));
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