/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Event;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Model.Query;
using SanteDB.Core.PubSub;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.OrmLite.MappedResultSets;
using SanteDB.OrmLite.Migration;
using SanteDB.OrmLite.Providers;
using SanteDB.Persistence.PubSub.ADO.Configuration;
using SanteDB.Persistence.PubSub.ADO.Data.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Security;

namespace SanteDB.Persistence.PubSub.ADO
{
    /// <summary>
    /// Represents a pub/sub manager which stores definitions in a database
    /// </summary>
    [ServiceProvider("ADO.NET Pub/Sub Subscription Manager")]
    public class AdoPubSubManager : IPubSubManagerService, IMappedQueryProvider<PubSubChannelDefinition>, IMappedQueryProvider<PubSubSubscriptionDefinition>
    {
        /// <summary>
        /// Gets the service name for this service
        /// </summary>
        public string ServiceName => "ADO.NET PubSub Manager";

        /// <summary>
        /// Gets the provider for this
        /// </summary>
        public IDbProvider Provider => this.m_configuration.Provider;

        /// <summary>
        /// Gets the query persistence service
        /// </summary>
        public IQueryPersistenceService QueryPersistence => this.m_queryPersistence;

        // Load mapper
        private ModelMapper m_mapper = new ModelMapper(typeof(AdoPubSubManager).Assembly.GetManifestResourceStream("SanteDB.Persistence.PubSub.ADO.Data.Map.ModelMap.xml"), "PubSubModelMap");

        // Configuration section
        private AdoPubSubConfigurationSection m_configuration;

        // Service manager
        private IServiceManager m_serviceManager;

        // Security repository
        private ISecurityRepositoryService m_securityRepository;

        // Cache
        private IDataCachingService m_cache;

        // Policy enforcement
        private IPolicyEnforcementService m_policyEnforcementService;

        // Query persistence service
        private IQueryPersistenceService m_queryPersistence;
        
        /// <summary>
        /// Creates a new instance of this pub-sub manager
        /// </summary>
        public AdoPubSubManager(IServiceManager serviceManager,
            IPolicyEnforcementService policyEnforcementService,
            IConfigurationManager configurationManager,
            ISecurityRepositoryService securityRepository,
            IDataCachingService cachingService,
            IQueryPersistenceService queryPersistence)
        {
            this.m_cache = cachingService;
            this.m_serviceManager = serviceManager;
            this.m_configuration = configurationManager.GetSection<AdoPubSubConfigurationSection>();
            this.m_policyEnforcementService = policyEnforcementService;
            this.m_securityRepository = securityRepository;
            this.m_serviceManager = serviceManager;
            this.m_configuration.Provider.UpgradeSchema("SanteDB.Persistence.PubSub.ADO");
            this.m_queryPersistence = queryPersistence;
        }

        // Ado Pub Sub Manager Tracer source
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(AdoPubSubManager));

        /// <summary>
        /// Fired when subscribing is about to occur
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<PubSubSubscriptionDefinition>> Subscribing;

        /// <summary>
        /// Fired after subscription has occurred
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<PubSubSubscriptionDefinition>> Subscribed;

        /// <summary>
        /// Fired when subscription is removing
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<PubSubSubscriptionDefinition>> UnSubscribing;

        /// <summary>
        /// Fired after subscription has been removed
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<PubSubSubscriptionDefinition>> UnSubscribed;

        /// <summary>
        /// Fired before activating
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<PubSubSubscriptionDefinition>> Activating;

        /// <summary>
        /// Fired before de-activating
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<PubSubSubscriptionDefinition>> DeActivating;

        /// <summary>
        /// Fired after activation
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<PubSubSubscriptionDefinition>> Activated;

        /// <summary>
        /// Fired after deactivation
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<PubSubSubscriptionDefinition>> DeActivated;

        /// <summary>
        /// Retrieve the specified channel by ID
        /// </summary>
        public PubSubChannelDefinition GetChannel(Guid id)
        {
            this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.ReadPubSubSubscription);

            var cached = this.m_cache?.GetCacheItem<PubSubChannelDefinition>(id);
            if (cached != null)
                return cached;

            using (var conn = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    conn.Open();
                    var domainInstance = conn.FirstOrDefault<DbChannel>(o => o.Key == id);
                    if (domainInstance == null)
                        throw new KeyNotFoundException($"Channel {id} not found");
                    var retVal = this.MapInstance(conn, domainInstance);
                    this.m_cache?.Add(retVal);
                    return retVal;
                }
                catch (Exception e)
                {
                    throw new Exception($"Error fetching channel {id}", e);
                }
            }
        }

        /// <summary>
        /// Map domain instance
        /// </summary>
        private PubSubChannelDefinition MapInstance(DataContext context, DbChannel domainInstance)
        {
            var retVal = this.m_mapper.MapDomainInstance<DbChannel, PubSubChannelDefinition>(domainInstance);
            retVal.DispatcherFactoryId = domainInstance.DispatchFactoryType; // TODO: Refactor this mapping to a fn
            retVal.Endpoint = domainInstance.Endpoint;
            retVal.Settings = context.Query<DbChannelSetting>(r => r.ChannelKey == retVal.Key).ToList().Select(r => new PubSubChannelSetting() { Name = r.Name, Value = r.Value }).ToList();
            return retVal;
        }

        /// <summary>
        /// Registers a new channel in the manager (database)
        /// </summary>
        public PubSubChannelDefinition RegisterChannel(string name, Type dispatcherFactory, Uri endpoint, IDictionary<string, string> settings)
        {
            var channelId = DispatcherFactoryUtil.FindDispatcherFactoryByType(dispatcherFactory);
            return this.RegisterChannel(name, channelId.Id, endpoint, settings);
        }

        /// <summary>
        /// Registers a new subscription
        /// </summary>
        public PubSubSubscriptionDefinition RegisterSubscription<TModel>(string name, string description, PubSubEventType events, Expression<Func<TModel, bool>> filter, Guid channelId, String supportAddress = null, DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null)
        {
            var hdsiFilter = QueryExpressionBuilder.BuildQuery(filter, true).ToHttpString();
            return this.RegisterSubscription(typeof(TModel), name, description, events, hdsiFilter, channelId, supportAddress, notBefore, notAfter);
        }

        /// <summary>
        /// Remove the channel
        /// </summary>
        public PubSubChannelDefinition RemoveChannel(Guid key)
        {
            this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.DeletePubSubSubscription);

            using (var conn = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    conn.Open();
                    var dbExisting = conn.FirstOrDefault<DbChannel>(o => o.Key == key);
                    if (dbExisting == null)
                        throw new KeyNotFoundException($"Channel {key} not found");

                    // Get the authorship
                    var se = this.m_securityRepository.GetSecurityEntity(AuthenticationContext.Current.Principal);
                    if (se == null)
                    {
                        throw new KeyNotFoundException($"Unable to determine structure data for {AuthenticationContext.Current.Principal.Identity.Name}");
                    }

                    dbExisting.ObsoletedByKey = se.Key.Value;
                    dbExisting.ObsoletionTime = DateTimeOffset.Now;
                    dbExisting.IsActive = false;
                    conn.Update(dbExisting);
                    this.m_cache?.Remove(key);
                    return this.MapInstance(conn, dbExisting);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error obsoleting channel {key}", e);
                }
            }
        }

        /// <summary>
        /// Remove the subscription
        /// </summary>
        public PubSubSubscriptionDefinition RemoveSubscription(Guid key)
        {
            this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.DeletePubSubSubscription);

            var subscription = this.GetSubscription(key);
            if (subscription == null)
                throw new KeyNotFoundException($"Subscription {key} not found");

            var preEvt = new DataPersistingEventArgs<PubSubSubscriptionDefinition>(subscription, TransactionMode.Commit, AuthenticationContext.Current.Principal);
            this.UnSubscribing?.Invoke(this, preEvt);
            if (preEvt.Cancel)
            {
                this.m_tracer.TraceWarning("Pre-Event Hook for UnSubscribing issued cancel");
                return preEvt.Data;
            }

            // Obsolete
            using (var conn = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    conn.Open();
                    var dbExisting = conn.FirstOrDefault<DbSubscription>(o => o.Key == key);
                    if (dbExisting == null)
                        throw new KeyNotFoundException($"Subscription {key} not found");

                    // Get the authorship
                    var se = this.m_securityRepository.GetSecurityEntity(AuthenticationContext.Current.Principal);
                    if (se == null)
                    {
                        throw new KeyNotFoundException($"Unable to determine structure data for {AuthenticationContext.Current.Principal.Identity.Name}");
                    }
                    subscription.ObsoletedByKey = dbExisting.ObsoletedByKey = se.Key.Value;
                    subscription.ObsoletionTime = dbExisting.ObsoletionTime = DateTimeOffset.Now;
                    subscription.IsActive = false;
                    conn.Update(dbExisting);
                    this.m_cache.Remove(key);

                    var retVal = this.MapInstance(conn, dbExisting);
                    this.UnSubscribed?.Invoke(this, new DataPersistedEventArgs<PubSubSubscriptionDefinition>(retVal, TransactionMode.Commit, AuthenticationContext.Current.Principal));
                    return retVal;
                }
                catch (Exception e)
                {
                    throw new Exception($"Error obsoleting subscription {key}", e);
                }
            }
        }

        /// <summary>
        /// Map domain instance
        /// </summary>
        private PubSubSubscriptionDefinition MapInstance(DataContext context, DbSubscription domainInstance)
        {
            var retVal = this.m_mapper.MapDomainInstance<DbSubscription, PubSubSubscriptionDefinition>(domainInstance);
            retVal.ResourceTypeName = domainInstance.ResourceType;
            retVal.Filter = context.Query<DbSubscriptionFilter>(r => r.SubscriptionKey == domainInstance.Key).Select(r => r.Filter).ToList();

            return retVal;
        }

        /// <summary>
        /// Register the specified channel
        /// </summary>
        public PubSubChannelDefinition RegisterChannel(string name, string dispatcherFactoryId, Uri endpoint, IDictionary<string, string> settings)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            else if (String.IsNullOrEmpty(dispatcherFactoryId))
            {
                var dispatchFactory = DispatcherFactoryUtil.FindDispatcherFactoryByUri(endpoint);
                if (dispatchFactory == null)
                {
                    throw new InvalidOperationException("Cannot find dispatcher factory for scheme!");
                }
                dispatcherFactoryId = dispatchFactory.Id;
            }
            else if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            // Validate state
            this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.CreatePubSubSubscription);

            var channel = new PubSubChannelDefinition()
            {
                Name = name,
                DispatcherFactoryId = dispatcherFactoryId,
                Endpoint = endpoint.ToString(),
                IsActive = false,
                Settings = settings.Select(o => new PubSubChannelSetting() { Name = o.Key, Value = o.Value }).ToList()
            };

            using (var conn = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        var dbChannel = this.m_mapper.MapModelInstance<PubSubChannelDefinition, DbChannel>(channel);
                        dbChannel.Endpoint = channel.Endpoint.ToString();

                        // Get the authorship
                        var se = this.m_securityRepository.GetSecurityEntity(AuthenticationContext.Current.Principal);
                        if (se == null)
                        {
                            throw new KeyNotFoundException($"Unable to determine structure data for {AuthenticationContext.Current.Principal.Identity.Name}");
                        }

                        dbChannel.CreatedByKey = se.Key.Value;
                        dbChannel.CreationTime = DateTimeOffset.Now;
                        dbChannel = conn.Insert(dbChannel);

                        // Insert settings
                        foreach (var itm in channel.Settings)
                            conn.Insert(new DbChannelSetting()
                            {
                                ChannelKey = dbChannel.Key.Value,
                                Name = itm.Name,
                                Value = itm.Value
                            });

                        tx.Commit();
                        channel = this.MapInstance(conn, dbChannel);

                        this.m_cache?.Add(channel);
                        return channel;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Error creating channel {channel}", e);
                }
            }
        }

        /// <summary>
        /// Find the channels matching <paramref name="filter"/>
        /// </summary>
        public IQueryResultSet<PubSubChannelDefinition> FindChannel(Expression<Func<PubSubChannelDefinition, bool>> filter)
        {
            this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.ReadPubSubSubscription);
            try
            {
                var domainFilter = this.m_mapper.MapModelExpression<PubSubChannelDefinition, DbChannel, bool>(filter, true);
                return new MappedQueryResultSet<PubSubChannelDefinition>(this);
            }
            catch (Exception e)
            {
                throw new Exception($"Error querying for channels {filter}", e);
            }
        }

        /// <summary>
        /// Find subscription based on the specified filter
        /// </summary>
        public IQueryResultSet<PubSubSubscriptionDefinition> FindSubscription(Expression<Func<PubSubSubscriptionDefinition, bool>> filter)
        {
            this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.ReadPubSubSubscription);

            try
            {
                return new MappedQueryResultSet<PubSubSubscriptionDefinition>(this).Where(filter);
            }
            catch (Exception e)
            {
                throw new Exception($"Error querying for subscriptions {filter}", e);
            }
        }

        /// <summary>
        /// Update the specified channel
        /// </summary>
        /// <remarks>
        /// Channel updates are restricted in that the scheme / dispatcher type cannot be changed , attempting to change the scheme
        /// (like from sms to http) will result in an error
        /// </remarks>
        public PubSubChannelDefinition UpdateChannel(Guid key, string name, Uri endpoint, IDictionary<string, string> settings)
        {
            this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.CreatePubSubSubscription);

            // Perform update
            using (var conn = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        var dbExisting = conn.FirstOrDefault<DbChannel>(o => o.Key == key);
                        if (dbExisting == null)
                            throw new KeyNotFoundException($"Channel {key} not found");

                        // Get the authorship
                        var se = this.m_securityRepository.GetSecurityEntity(AuthenticationContext.Current.Principal);
                        if (se == null)
                        {
                            throw new KeyNotFoundException($"Unable to determine structure data for {AuthenticationContext.Current.Principal.Identity.Name}");
                        }
                        dbExisting.UpdatedByKey = se.Key.Value;
                        dbExisting.UpdatedTime = DateTimeOffset.Now;

                        dbExisting.ObsoletedByKey = null;
                        dbExisting.ObsoletionTime = null;
                        dbExisting.ObsoletedBySpecified = dbExisting.ObsoletionTimeSpecified = true;

                        // Ensure that the scheme does not change
                        var oldUri = new Uri(dbExisting.Endpoint);
                        if (!endpoint.Scheme.Equals(oldUri.Scheme))
                            throw new InvalidOperationException($"Cannot change dispatcher scheme from {oldUri.Scheme} to {endpoint.Scheme} - please remove this subscription and re-create it with the new dispatcher factory");

                        dbExisting.Endpoint = endpoint.ToString();
                        dbExisting.Name = name;
                        dbExisting.IsActive = false; // we disable the channel to allow for review
                        conn.Update(dbExisting);
                        conn.DeleteAll<DbChannelSetting>(o => o.ChannelKey == key);

                        // Insert settings
                        foreach (var itm in settings)
                            conn.Insert(new DbChannelSetting()
                            {
                                ChannelKey = dbExisting.Key.Value,
                                Name = itm.Key,
                                Value = itm.Value
                            });

                        var retVal = this.MapInstance(conn, dbExisting);
                        this.m_cache?.Add(retVal);

                        tx.Commit();

                        return retVal;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Error updating channel {key}", e);
                }
            }
        }

        /// <summary>
        /// Register a subscription for <paramref name="modelType"/>
        /// </summary>
        public PubSubSubscriptionDefinition RegisterSubscription(Type modelType, string name, string description, PubSubEventType events, string hdsiFilter, Guid channelId, String supportAddress = null, DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null)
        {
            this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.CreatePubSubSubscription);

            var subscription = new PubSubSubscriptionDefinition()
            {
                ChannelKey = channelId,
                Event = events,
                Filter = String.IsNullOrEmpty(hdsiFilter) ? null : new List<string>(hdsiFilter.Split('&')),
                IsActive = false,
                Name = name,
                Description = description,
                SupportContact = supportAddress,
                NotBefore = notBefore?.DateTime,
                NotAfter = notAfter?.DateTime,
                ResourceTypeName = modelType.GetSerializationName()
            };

            var preEvent = new DataPersistingEventArgs<PubSubSubscriptionDefinition>(subscription, TransactionMode.Commit, AuthenticationContext.Current.Principal);
            this.Subscribing?.Invoke(this, preEvent);
            if (preEvent.Cancel)
            {
                this.m_tracer.TraceWarning("Pre-Event Hook Indicates Cancel");
                return preEvent.Data;
            }

            using (var conn = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        // First construct db instance
                        var dbSubscription = this.m_mapper.MapModelInstance<PubSubSubscriptionDefinition, DbSubscription>(subscription);

                        // Get the authorship
                        var se = this.m_securityRepository.GetSecurityEntity(AuthenticationContext.Current.Principal);
                        if (se == null)
                        {
                            throw new KeyNotFoundException($"Unable to determine structure data for {AuthenticationContext.Current.Principal.Identity.Name}");
                        }
                        dbSubscription.CreatedByKey = se.Key.Value;
                        dbSubscription.CreationTime = DateTimeOffset.Now;
                        dbSubscription = conn.Insert(dbSubscription);

                        // Insert settings
                        if (subscription.Filter != null)
                        {
                            foreach (var itm in subscription.Filter)
                                conn.Insert(new DbSubscriptionFilter()
                                {
                                    SubscriptionKey = dbSubscription.Key.Value,
                                    Filter = itm
                                });
                        }

                        tx.Commit();

                        subscription = this.MapInstance(conn, dbSubscription);
                        this.Subscribed?.Invoke(this, new DataPersistedEventArgs<PubSubSubscriptionDefinition>(subscription, TransactionMode.Commit, AuthenticationContext.Current.Principal));
                        this.m_cache?.Add(subscription);
                        return subscription;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Error inserting subscription {subscription}", e);
                }
            }
        }

        /// <summary>
        /// Update the subscription
        /// </summary>
        public PubSubSubscriptionDefinition UpdateSubscription(Guid key, string name, string description, PubSubEventType events, string hdsiFilter, String supportAddress = null, DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null)
        {
            this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.CreatePubSubSubscription);

            using (var conn = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        var dbExisting = conn.FirstOrDefault<DbSubscription>(o => o.Key == key);
                        if (dbExisting == null)
                            throw new KeyNotFoundException($"Subscription {key} not found");

                        // Get the authorship
                        var se = this.m_securityRepository.GetSecurityEntity(AuthenticationContext.Current.Principal);
                        if (se == null)
                        {
                            throw new KeyNotFoundException($"Unable to determine structure data for {AuthenticationContext.Current.Principal.Identity.Name}");
                        }

                        dbExisting.UpdatedByKey = se.Key.Value;
                        dbExisting.UpdatedTime = DateTimeOffset.Now;
                        dbExisting.ObsoletedByKey = null;
                        dbExisting.ObsoletionTime = null;
                        dbExisting.ObsoletedBySpecified = dbExisting.ObsoletionTimeSpecified = true;
                        dbExisting.Name = name;
                        dbExisting.IsActive = false; // we disable the subscription to allow for review
                        dbExisting.NotAfter = notAfter;
                        dbExisting.NotBefore = notBefore;
                        dbExisting.Description = description;
                        dbExisting.SupportContact = supportAddress;
                        dbExisting.Event = (int)events;

                        conn.Update(dbExisting);
                        conn.DeleteAll<DbSubscriptionFilter>(o => o.SubscriptionKey == dbExisting.Key);
                        // Insert settings
                        conn.InsertAll(hdsiFilter.Split('&').Select(s => new DbSubscriptionFilter()
                        {
                            SubscriptionKey = dbExisting.Key.Value,
                            Filter = s
                        }));

                        var retVal = this.MapInstance(conn, dbExisting);
                        this.m_cache?.Add(retVal);
                        tx.Commit();
                        this.Subscribed?.Invoke(this, new DataPersistedEventArgs<PubSubSubscriptionDefinition>(retVal, TransactionMode.Commit, AuthenticationContext.Current.Principal));

                        return this.MapInstance(conn, dbExisting);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Error updating subscription {key}", e);
                }
            }
        }

        /// <summary>
        /// Activates or de-activates <paramref name="key"/>
        /// </summary>
        public PubSubSubscriptionDefinition ActivateSubscription(Guid key, bool isActive)
        {
            this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.EnablePubSubSubscription);

            // perform activation
            using (var conn = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    conn.Open();
                    var dbExisting = conn.FirstOrDefault<DbSubscription>(o => o.Key == key);
                    if (dbExisting == null)
                        throw new KeyNotFoundException($"Subscription {key} not found");

                    var subscription = this.MapInstance(conn, dbExisting);
                    var preEvt = new DataPersistingEventArgs<PubSubSubscriptionDefinition>(subscription, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                    if (isActive)
                    {
                        this.Activating?.Invoke(this, preEvt);
                    }
                    else
                    {
                        this.DeActivating?.Invoke(this, preEvt);
                    }

                    if (preEvt.Cancel)
                    {
                        this.m_tracer.TraceWarning("Pre-Event Hook for Activation issued cancel");
                        return preEvt.Data;
                    }

                    var se = this.m_securityRepository.GetSecurityEntity(AuthenticationContext.Current.Principal);
                    if (se == null)
                    {
                        throw new SecurityException($"Cannot determine SID for {AuthenticationContext.Current.Principal.Identity.Name}");
                    }
                    dbExisting.IsActive = isActive;
                    subscription.IsActive = isActive;
                    subscription.UpdatedByKey = dbExisting.UpdatedByKey = se.Key.Value;
                    subscription.UpdatedTime = dbExisting.UpdatedTime = DateTimeOffset.Now;
                    dbExisting.ObsoletionTime = null;
                    dbExisting.ObsoletedByKey = null;
                    dbExisting.ObsoletedBySpecified = dbExisting.ObsoletionTimeSpecified = true;
                    conn.Update(dbExisting);
                    this.m_cache.Remove(key);

                    if (isActive)
                    {
                        this.Activated?.Invoke(this, new DataPersistedEventArgs<PubSubSubscriptionDefinition>(subscription, TransactionMode.Commit, AuthenticationContext.Current.Principal));
                    }
                    else
                    {
                        this.DeActivated?.Invoke(this, new DataPersistedEventArgs<PubSubSubscriptionDefinition>(subscription, TransactionMode.Commit, AuthenticationContext.Current.Principal));
                    }

                    return subscription;
                }
                catch (Exception e)
                {
                    throw new Exception($"Error obsoleting subscription {key}", e);
                }
            }
        }

        /// <summary>
        /// Get the specified subscription by identifier
        /// </summary>
        public PubSubSubscriptionDefinition GetSubscription(Guid key)
        {
            this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.ReadPubSubSubscription);
            var cache = this.m_cache?.GetCacheItem<PubSubSubscriptionDefinition>(key);
            if (cache != null)
                return cache;
            else
                return this.FindSubscription(o => o.Key == key).FirstOrDefault();
        }

        /// <summary>
        /// Get subscription by its name
        /// </summary>
        public PubSubSubscriptionDefinition GetSubscriptionByName(string name)
        {
            return this.FindSubscription(o => o.Name == name && o.ObsoletionTime == null).FirstOrDefault();
        }

        /// <summary>
        /// Execute a query query via the ORM for the PubSubSubscription definition
        /// </summary>
        public IOrmResultSet ExecuteQueryOrm(DataContext context, Expression<Func<PubSubSubscriptionDefinition, bool>> query)
        {
            var domainQuery = this.m_mapper.MapModelExpression<PubSubSubscriptionDefinition, DbSubscription, bool>(query, true);
            return context.Query<DbSubscription>(domainQuery);
        }

        /// <summary>
        /// Get the specified pub-sub subscription definition
        /// </summary>
        public PubSubSubscriptionDefinition Get(DataContext context, Guid key)
        {
            return this.MapInstance(context, context.FirstOrDefault<DbSubscription>(o => o.Key == key && o.ObsoletionTime == null));
        }

        /// <summary>
        /// Map this to model instance
        /// </summary>
        public PubSubSubscriptionDefinition ToModelInstance(DataContext context, object result)
        {
            if (result is DbSubscription dbs)
            {
                return this.MapInstance(context, dbs);
            }
            return null;
        }

        /// <summary>
        /// Map property expression
        /// </summary>
        public Expression MapExpression<TReturn>(Expression<Func<PubSubSubscriptionDefinition, TReturn>> sortExpression)
        {
            return this.m_mapper.MapModelExpression<PubSubSubscriptionDefinition, DbSubscription, TReturn>(sortExpression, true);
        }

        /// <summary>
        /// Execute a query query via the ORM for the PubSubSubscription definition
        /// </summary>
        public IOrmResultSet ExecuteQueryOrm(DataContext context, Expression<Func<PubSubChannelDefinition, bool>> query)
        {
            var domainQuery = this.m_mapper.MapModelExpression<PubSubChannelDefinition, DbChannel, bool>(query, true);
            return context.Query<DbChannel>(domainQuery);
        }

        /// <summary>
        /// Get the specified pub-sub subscription definition
        /// </summary>
        PubSubChannelDefinition IMappedQueryProvider<PubSubChannelDefinition>.Get(DataContext context, Guid key)
        {
            return this.MapInstance(context, context.FirstOrDefault<DbChannel>(o => o.Key == key && o.ObsoletionTime == null));
        }

        /// <summary>
        /// Map this to model instance
        /// </summary>
        PubSubChannelDefinition IMappedQueryProvider<PubSubChannelDefinition>.ToModelInstance(DataContext context, object result)
        {
            if (result is DbChannel dbs)
            {
                return this.MapInstance(context, dbs);
            }
            return null;
        }

        /// <summary>
        /// Map property expression
        /// </summary>
        public Expression MapExpression<TReturn>(Expression<Func<PubSubChannelDefinition, TReturn>> sortExpression)
        {
            return this.m_mapper.MapModelExpression<PubSubChannelDefinition, DbChannel, TReturn>(sortExpression, true);
        }

        /// <summary>
        /// Find channel - obsolete
        /// </summary>
        [Obsolete("Find(filter)", true)]
        public IEnumerable<PubSubChannelDefinition> FindChannel(Expression<Func<PubSubChannelDefinition, bool>> filter, int offset, int count, out int totalResults)
        {
            var results = this.FindChannel(filter);
            totalResults = results.Count();
            return results.Skip(offset).Take(count);
        }

        /// <summary>
        /// Find subscription obsolete
        /// </summary>
        [Obsolete("Find(filter)", true)]
        public IEnumerable<PubSubSubscriptionDefinition> FindSubscription(Expression<Func<PubSubSubscriptionDefinition, bool>> filter, int offset, int count, out int totalResults)
        {
            var results = this.FindSubscription(filter);
            totalResults = results.Count();
            return results.Skip(offset).Take(count);
        }

    }
}