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
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Event;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Model.Query;
using SanteDB.Core.PubSub;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.OrmLite.Migration;
using SanteDB.Persistence.PubSub.ADO.Configuration;
using SanteDB.Persistence.PubSub.ADO.Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security;

namespace SanteDB.Persistence.PubSub.ADO
{
    /// <summary>
    /// Represents a pub/sub manager which stores definitions in a database
    /// </summary>
    [ServiceProvider("ADO.NET Pub/Sub Subscription Manager")]
    public class AdoPubSubManager : IPubSubManagerService
    {
        /// <summary>
        /// Gets the service name for this service
        /// </summary>
        public string ServiceName => "ADO.NET PubSub Manager";

        // Load mapper
        private ModelMapper m_mapper = new ModelMapper(typeof(AdoPubSubManager).Assembly.GetManifestResourceStream("SanteDB.Persistence.PubSub.ADO.Data.Map.ModelMap.xml"), "PubSubModelMap");

        // Configuration section
        private AdoPubSubConfigurationSection m_configuration;

        // Security repository
        private ISecurityRepositoryService m_securityRepository;

        // Cache
        private IDataCachingService m_cache;

        // Service manager
        private IServiceManager m_serviceManager;

        // Broker
        private IPubSubBroker m_broker;

        // Policy enforcement
        private IPolicyEnforcementService m_policyEnforcementService;

        /// <summary>
        /// Creates a new instance of this pub-sub manager
        /// </summary>
        public AdoPubSubManager(IServiceManager serviceManager,
            IPolicyEnforcementService policyEnforcementService,
            IConfigurationManager configurationManager,
            ISecurityRepositoryService securityRepository,
            IDataCachingService cachingService,
            IPubSubBroker broker)
        {
            this.m_cache = cachingService;
            this.m_serviceManager = serviceManager;
            this.m_broker = broker;
            this.m_configuration = configurationManager.GetSection<AdoPubSubConfigurationSection>();
            this.m_policyEnforcementService = policyEnforcementService;
            this.m_securityRepository = securityRepository;
            this.m_configuration.Provider.UpgradeSchema("SanteDB.Persistence.PubSub.ADO");
        }

        // Ado Pub Sub Manager Tracer source
        private Tracer m_tracer = Tracer.GetTracer(typeof(AdoPubSubManager));

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
        /// Find the specified channel
        /// </summary>
        public IEnumerable<PubSubChannelDefinition> FindChannel(Expression<Func<PubSubChannelDefinition, bool>> filter)
        {
            return this.FindChannel(filter, 0, 100, out int _);
        }

        /// <summary>
        /// Find all subscriptions
        /// </summary>
        public IEnumerable<PubSubSubscriptionDefinition> FindSubscription(Expression<Func<PubSubSubscriptionDefinition, bool>> filter)
        {
            return this.FindSubscription(filter, 0, 100, out int _);
        }

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
            retVal.DispatcherFactoryTypeXml = domainInstance.DispatchFactoryType; // TODO: Refactor this mapping to a fn
            retVal.Endpoint = domainInstance.Endpoint;
            retVal.Settings = context.Query<DbChannelSetting>(r => r.ChannelKey == retVal.Key).ToList().Select(r => new PubSubChannelSetting() { Name = r.Name, Value = r.Value }).ToList();
            return retVal;
        }

        /// <summary>
        /// Registers a new channel in the manager (database)
        /// </summary>
        public PubSubChannelDefinition RegisterChannel(string name, Type dispatcherFactory, Uri endpoint, IDictionary<string, string> settings)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            else if (dispatcherFactory == null)
            {
                throw new ArgumentNullException(nameof(dispatcherFactory));
            }
            else if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            // Validate state
            this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.CreatePubSubSubscription);
            if (!typeof(IPubSubDispatcherFactory).IsAssignableFrom(dispatcherFactory))
                throw new InvalidOperationException("Dispatcher factory is of invalid type");

            var channel = new PubSubChannelDefinition()
            {
                Name = name,
                DispatcherFactoryTypeXml = dispatcherFactory.AssemblyQualifiedName,
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
        /// Registers a new subscription
        /// </summary>
        public PubSubSubscriptionDefinition RegisterSubscription<TModel>(string name, string description, PubSubEventType events, Expression<Func<TModel, bool>> filter, Guid channelId, String supportAddress = null, DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null)
        {
            var hdsiFilter = new NameValueCollection(QueryExpressionBuilder.BuildQuery(filter, true).ToArray()).ToString();
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
                    conn.Update(dbExisting);
                    this.m_cache.Remove(key);
                    return this.MapInstance(conn, dbExisting);
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
            retVal.ResourceTypeXml = domainInstance.ResourceType;
            retVal.Filter = context.Query<DbSubscriptionFilter>(r => r.SubscriptionKey == domainInstance.Key).Select(r => r.Filter).ToList();

            return retVal;
        }

        /// <summary>
        /// Register the specified channel
        /// </summary>
        public PubSubChannelDefinition RegisterChannel(string name, Uri endpoint, IDictionary<string, string> settings)
        {
            return this.RegisterChannel(name, this.m_broker.FindDispatcherFactory(endpoint).GetType(), endpoint, settings);
        }

        /// <summary>
        /// Find the channels matching <paramref name="filter"/>
        /// </summary>
        public IEnumerable<PubSubChannelDefinition> FindChannel(Expression<Func<PubSubChannelDefinition, bool>> filter, int offset, int count, out int totalResults)
        {
            this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.ReadPubSubSubscription);

            using (var conn = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    conn.Open();
                    var domainFilter = this.m_mapper.MapModelExpression<PubSubChannelDefinition, DbChannel, bool>(filter, true);
                    var retVal = conn.Query(domainFilter);
                    totalResults = retVal.Count();
                    return retVal.Skip(offset).Take(count).ToList().Select(o =>
                    {
                        var cv = this.m_cache?.GetCacheItem<PubSubChannelDefinition>(o.Key.Value);
                        if (cv != null)
                            return cv;
                        else
                        {
                            var rv = this.MapInstance(conn, o);
                            this.m_cache?.Add(rv);
                            return rv;
                        }
                    });
                }
                catch (Exception e)
                {
                    throw new Exception($"Error querying for channels {filter}", e);
                }
            }
        }

        /// <summary>
        /// Find subscription based on the specified filter
        /// </summary>
        public IEnumerable<PubSubSubscriptionDefinition> FindSubscription(Expression<Func<PubSubSubscriptionDefinition, bool>> filter, int offset, int count, out int totalResults)
        {
            this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.ReadPubSubSubscription);

            using (var conn = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    conn.Open();
                    var domainFilter = this.m_mapper.MapModelExpression<PubSubSubscriptionDefinition, DbSubscription, bool>(filter, true);
                    var retVal = conn.Query(domainFilter);

                    totalResults = retVal.Count();

                    return retVal.Skip(offset).Take(count).ToList().Select(o =>
                    {
                        var rv = this.m_cache?.GetCacheItem<PubSubSubscriptionDefinition>(o.Key.Value);
                        if (rv != null)
                            return rv;
                        else
                        {
                            rv = this.MapInstance(conn, o);
                            this.m_cache?.Add(rv);
                            return rv;
                        }
                    }).ToList();
                }
                catch (Exception e)
                {
                    throw new Exception($"Error querying for subscriptions {filter}", e);
                }
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
                        conn.Delete<DbChannelSetting>(o => o.ChannelKey == key);

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
                Filter = String.IsNullOrEmpty(hdsiFilter) ? null : new List<string>() { hdsiFilter },
                IsActive = false,
                Name = name,
                Description = description,
                SupportContact = supportAddress,
                NotBefore = notBefore,
                NotAfter = notAfter,
                ResourceTypeXml = modelType.GetSerializationName()
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

                        var retVal = new PubSubSubscriptionDefinition();
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
                        dbExisting.Name = retVal.Name = name;
                        dbExisting.IsActive = false; // we disable the subscription to allow for review
                        dbExisting.NotAfter = retVal.NotAfter = notAfter;
                        dbExisting.NotBefore = retVal.NotBefore = notBefore;
                        dbExisting.Description = description;
                        dbExisting.SupportContact = supportAddress;
                        dbExisting.Event = (int)events;

                        conn.Update(dbExisting);
                        conn.Delete<DbSubscriptionFilter>(o => o.SubscriptionKey == retVal.Key);
                        // Insert settings
                        conn.Insert(new DbSubscriptionFilter()
                        {
                            SubscriptionKey = dbExisting.Key.Value,
                            Filter = hdsiFilter
                        });

                        this.m_cache?.Add(retVal);
                        tx.Commit();
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
                return this.FindSubscription(o => o.Key == key, 0, 1, out int _).FirstOrDefault();
        }

        /// <summary>
        /// Get subscription by its name
        /// </summary>
        public PubSubSubscriptionDefinition GetSubscriptionByName(string name)
        {
            return this.FindSubscription(o => o.Name == name && o.ObsoletionTime == null, 0, 1, out int _).FirstOrDefault();
        }
    }
}