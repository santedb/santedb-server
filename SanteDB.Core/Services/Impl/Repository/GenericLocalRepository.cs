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

using SanteDB.Core;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Event;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using SanteDB.Server.Core;
using SanteDB.Server.Core.Security.Attribute;
using SanteDB.Core.Security.Services;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Represents a base class for entity repository services
    /// </summary>
    [ServiceProvider("Local Repository Service", Dependencies = new Type[] { typeof(IDataPersistenceService) })]
    public class GenericLocalRepository<TEntity> :
        IRepositoryService,
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

        // Privacy service
        private IPrivacyEnforcementService m_privacyService;

        // Localization service
        protected readonly ILocalizationService m_localizationService;

        // Policy enforcement
        protected IPolicyEnforcementService m_policyService;

        /// <summary>
        /// Creates a new generic local repository with specified privacy service
        /// </summary>
        public GenericLocalRepository(IPrivacyEnforcementService privacyService, IPolicyEnforcementService policyService, ILocalizationService localizationService)
        {
            this.m_privacyService = privacyService;
            this.m_policyService = policyService;
            this.m_localizationService = localizationService;
        }

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
                throw new InvalidOperationException(this.m_localizationService.FormatString("error.server.core.servicePersistence", new
                    {
                        param = typeof(IDataPersistenceService<TEntity>).FullName
                    }));
            }
            var businessRulesService = ApplicationServiceContext.Current.GetBusinessRulesService<TEntity>();

            // Notify query
            var preQueryEventArgs = new QueryRequestEventArgs<TEntity>(query, offset, count, queryId, AuthenticationContext.Current.Principal, orderBy);
            this.Querying?.Invoke(this, preQueryEventArgs);
            IEnumerable<TEntity> results = null;
            if (preQueryEventArgs.Cancel) /// Cancel the request
            {
                totalResults = preQueryEventArgs.TotalResults;
                results = preQueryEventArgs.Results;
            }
            else
            {
                if (queryId != Guid.Empty && persistenceService is IStoredQueryDataPersistenceService<TEntity>)
                    results = (persistenceService as IStoredQueryDataPersistenceService<TEntity>).Query(preQueryEventArgs.Query, preQueryEventArgs.QueryId.GetValueOrDefault(), preQueryEventArgs.Offset, preQueryEventArgs.Count, out totalResults, AuthenticationContext.Current.Principal, orderBy);
                else
                    results = persistenceService.Query(preQueryEventArgs.Query, preQueryEventArgs.Offset, preQueryEventArgs.Count, out totalResults, AuthenticationContext.Current.Principal, orderBy);
            }

            // 1. Let the BRE run
            var retVal = businessRulesService != null ? businessRulesService.AfterQuery(results) : results;

            // 2. Broadcast query performed
            var postEvt = new QueryResultEventArgs<TEntity>(query, retVal, offset, count, totalResults, queryId, AuthenticationContext.AnonymousPrincipal);
            this.Queried?.Invoke(this, postEvt);
            totalResults = postEvt.TotalResults;

            // 2. Apply Filters if needed
            retVal = this.m_privacyService?.Apply(retVal, AuthenticationContext.Current.Principal) ?? retVal;

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

            // Call privacy hook
            if (this.m_privacyService?.ValidateWrite(data, AuthenticationContext.Current.Principal) == false)
                this.ThrowPrivacyValidationException(data);

            // Fire pre-persistence triggers
            var prePersistence = new DataPersistingEventArgs<TEntity>(data, TransactionMode.Commit, AuthenticationContext.Current.Principal);
            this.Inserting?.Invoke(this, prePersistence);
            if (prePersistence.Cancel)
            {
                this.m_traceSource.TraceInfo("Pre-persistence event signal cancel: {0}", data);

                // Fired inserted trigger
                if (prePersistence.Success)
                {
                    this.Inserted?.Invoke(this, new DataPersistedEventArgs<TEntity>(prePersistence.Data, TransactionMode.Commit, AuthenticationContext.Current.Principal));
                }
                return this.m_privacyService?.Apply(prePersistence.Data, AuthenticationContext.Current.Principal) ?? prePersistence.Data;
            }

            // Did the pre-persistence service change the type to a batch
            var businessRulesService = ApplicationServiceContext.Current.GetBusinessRulesService<TEntity>();

            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TEntity>>();
            data = businessRulesService?.BeforeInsert(data) ?? prePersistence.Data;
            data = persistenceService.Insert(data, TransactionMode.Commit, AuthenticationContext.Current.Principal);
            businessRulesService?.AfterInsert(data);
            this.Inserted?.Invoke(this, new DataPersistedEventArgs<TEntity>(data, TransactionMode.Commit, AuthenticationContext.Current.Principal));
            return this.m_privacyService?.Apply(data, AuthenticationContext.Current.Principal) ?? data;
        }

        /// <summary>
        /// Throw a privacy validation exception
        /// </summary>
        private void ThrowPrivacyValidationException(TEntity data)
        {
            throw new DetectedIssueException(
                new DetectedIssue(DetectedIssuePriorityType.Error, "privacy", this.m_localizationService.FormatString("error.server.core.validationFail", new
                {
                    param = "Privacy"
                }), DetectedIssueKeys.AlreadyDoneIssue)
            );
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
                throw new InvalidOperationException(this.m_localizationService.FormatString("error.server.core.servicePersistence", new
                    {
                        param = nameof(IDataPersistenceService<TEntity>)

                    }));
            }

            var entity = persistenceService.Get(key, null, true, AuthenticationContext.Current.Principal);

            if (entity == null)
                throw new KeyNotFoundException(this.m_localizationService.FormatString("error.type.KeyNotFoundException.notFound",new
                {
                    param = $"Entity {key}"
                }));

            // Fire pre-persistence triggers
            var prePersistence = new DataPersistingEventArgs<TEntity>(entity, TransactionMode.Commit, AuthenticationContext.Current.Principal);
            this.Obsoleting?.Invoke(this, prePersistence);
            if (prePersistence.Cancel)
            {
                this.m_traceSource.TraceInfo("Pre-persistence event signal cancel obsolete: {0}", key);
                // Fired inserted trigger
                if (prePersistence.Success)
                {
                    this.Obsoleted?.Invoke(this, new DataPersistedEventArgs<TEntity>(prePersistence.Data, TransactionMode.Commit, AuthenticationContext.Current.Principal));
                }
                return this.m_privacyService?.Apply(prePersistence.Data, AuthenticationContext.Current.Principal) ?? prePersistence.Data;
            }

            var businessRulesService = ApplicationServiceContext.Current.GetBusinessRulesService<TEntity>();

            entity = businessRulesService?.BeforeObsolete(entity) ?? entity;
            entity = persistenceService.Obsolete(entity, TransactionMode.Commit, AuthenticationContext.Current.Principal);
            entity = businessRulesService?.AfterObsolete(entity) ?? entity;

            this.Obsoleted?.Invoke(this, new DataPersistedEventArgs<TEntity>(entity, TransactionMode.Commit, AuthenticationContext.Current.Principal));

            return this.m_privacyService?.Apply(entity, AuthenticationContext.Current.Principal) ?? entity;
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
                throw new InvalidOperationException(this.m_localizationService.FormatString("error.server.core.servicePersistence", new
                    {
                        param = nameof(IDataPersistenceService<TEntity>)
                    }));
            }

            var businessRulesService = ApplicationServiceContext.Current.GetBusinessRulesService<TEntity>();

            var preRetrieve = new DataRetrievingEventArgs<TEntity>(key, versionKey, AuthenticationContext.Current.Principal);

            this.Retrieving?.Invoke(this, preRetrieve);
            if (preRetrieve.Cancel)
            {
                this.m_traceSource.TraceInfo("Pre-retrieve trigger signals cancel: {0}", key);
                return this.m_privacyService?.Apply(preRetrieve.Result, AuthenticationContext.Current.Principal) ?? preRetrieve.Result;
            }

            var result = persistenceService.Get(key, versionKey, true, AuthenticationContext.Current.Principal);
            var retVal = businessRulesService?.AfterRetrieve(result) ?? result;
            var postEvt = new DataRetrievedEventArgs<TEntity>(retVal, AuthenticationContext.Current.Principal);
            this.Retrieved?.Invoke(this, postEvt);

            return this.m_privacyService?.Apply(postEvt.Data, AuthenticationContext.Current.Principal) ?? postEvt.Data;
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
                throw new InvalidOperationException(this.m_localizationService.FormatString("error.server.core.servicePersistence", new
                    {
                        param = nameof(IDataPersistenceService<TEntity>)
                    }));
            }

            data = this.Validate(data);

            var businessRulesService = ApplicationServiceContext.Current.GetBusinessRulesService<TEntity>();

            try
            {
                if (this.m_privacyService?.ValidateWrite(data, AuthenticationContext.Current.Principal) == false)
                    this.ThrowPrivacyValidationException(data);

                var preSave = new DataPersistingEventArgs<TEntity>(data, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                this.Saving?.Invoke(this, preSave);
                if (preSave.Cancel)
                {
                    this.m_traceSource.TraceInfo("Persistence layer indicates pre-save cancel: {0}", data);
                    // Fired inserted trigger
                    if (preSave.Success)
                    {
                        this.Saved?.Invoke(this, new DataPersistedEventArgs<TEntity>(preSave.Data, TransactionMode.Commit, AuthenticationContext.Current.Principal));
                    }
                    return this.m_privacyService?.Apply(preSave.Data, AuthenticationContext.Current.Principal) ?? preSave.Data;
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

                this.Saved?.Invoke(this, new DataPersistedEventArgs<TEntity>(data, TransactionMode.Commit, AuthenticationContext.Current.Principal));
                return this.m_privacyService?.Apply(data, AuthenticationContext.Current.Principal) ?? data;
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
            var preQueryEventArgs = new QueryRequestEventArgs<TEntity>(query, offset, count, queryId, AuthenticationContext.Current.Principal, new ModelSort<TEntity>[0]);
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
            return this.m_privacyService?.Apply(results, AuthenticationContext.Current.Principal) ?? results;
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
            this.m_policyService.Demand(this.WritePolicy);
        }

        /// <summary>
        /// Demand read
        /// </summary>
        /// <param name="key"></param>
        public virtual void DemandRead(Guid key)
        {
            this.m_policyService.Demand(this.ReadPolicy);
        }

        /// <summary>
        /// Demand delete permission
        /// </summary>
        public virtual void DemandDelete(Guid key)
        {
            this.m_policyService.Demand(this.DeletePolicy);
        }

        /// <summary>
        /// Demand alter permission
        /// </summary>
        public virtual void DemandAlter(object data)
        {
            this.m_policyService.Demand(this.AlterPolicy);
        }

        /// <summary>
        /// Demand query
        /// </summary>
        public virtual void DemandQuery()
        {
            this.m_policyService.Demand(this.QueryPolicy);
        }

        /// <summary>
        /// Get the specified data
        /// </summary>
        IdentifiedData IRepositoryService.Get(Guid key)
        {
            return this.Get(key);
        }

        /// <summary>
        /// Find specified data
        /// </summary>
        IEnumerable<IdentifiedData> IRepositoryService.Find(Expression query)
        {
            return this.Find((Expression<Func<TEntity, bool>>)query).OfType<IdentifiedData>();
        }

        /// <summary>
        /// Find specified data
        /// </summary>
        IEnumerable<IdentifiedData> IRepositoryService.Find(Expression query, int offset, int? count, out int totalResults)
        {
            return this.Find((Expression<Func<TEntity, bool>>)query, offset, count, out totalResults).OfType<IdentifiedData>();
        }

        /// <summary>
        /// Insert the specified data
        /// </summary>
        IdentifiedData IRepositoryService.Insert(object data)
        {
            return this.Insert((TEntity)data);
        }

        /// <summary>
        /// Save specified data
        /// </summary>
        IdentifiedData IRepositoryService.Save(object data)
        {
            return this.Save((TEntity)data);
        }

        /// <summary>
        /// Obsolete
        /// </summary>
        IdentifiedData IRepositoryService.Obsolete(Guid key)
        {
            return this.Obsolete(key);
        }
    }
}