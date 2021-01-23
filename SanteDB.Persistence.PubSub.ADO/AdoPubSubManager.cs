using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Event;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Model.Query;
using SanteDB.Core.PubSub;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Persistence.PubSub.ADO.Configuration;
using SanteDB.Persistence.PubSub.ADO.Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Persistence.PubSub.ADO
{
    /// <summary>
    /// Represents a pub/sub manager which stores definitions in a database
    /// </summary>
    public class AdoPubSubManager : IPubSubManagerService,
        IRepositoryService<PubSubChannelDefinition>,
        IRepositoryService<PubSubSubscriptionDefinition>
    {
        /// <summary>
        /// Gets the service name for this service
        /// </summary>
        public string ServiceName => "ADO.NET PubSub Manager";

        // Load mapper
        private ModelMapper m_mapper = new ModelMapper(typeof(AdoPubSubManager).Assembly.GetManifestResourceStream("SanteDB.Persistence.PubSub.ADO.Data.Map.ModelMap.xml"));

        // Configuration section
        private AdoPubSubConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AdoPubSubConfigurationSection>();

        // Security repository
        private ISecurityRepositoryService m_securityRepository = ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>();

        // Cache
        private IDataCachingService m_cache = ApplicationServiceContext.Current.GetService<IDataCachingService>();

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
        /// Find the specified channel
        /// </summary>
        public IEnumerable<PubSubChannelDefinition> FindChannel(Expression<Func<PubSubChannelDefinition, bool>> filter)
        {
            return this.Find(filter);
        }

        /// <summary>
        /// Find all subscriptions
        /// </summary>
        public IEnumerable<PubSubSubscriptionDefinition> FindSubscription(Expression<Func<PubSubSubscriptionDefinition, bool>> filter)
        {
            return this.Find(filter);
        }

        /// <summary>
        /// Retrieve the specified channel by ID
        /// </summary>
        public PubSubChannelDefinition GetChannel(Guid id)
        {
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
                    var retVal = this.m_mapper.MapDomainInstance<DbChannel, PubSubChannelDefinition>(domainInstance, true);
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
        /// Registers a new channel in the manager (database)
        /// </summary>
        public PubSubChannelDefinition RegisterChannel(string name, Type dispatcherFactory, Uri endpoint, IDictionary<string, string> settings)
        {
            return this.Insert(new PubSubChannelDefinition()
            {
                Name = name,
                DispatcherFactoryTypeXml = dispatcherFactory.AssemblyQualifiedName,
                Endpoint = endpoint,
                IsActive = true,
                Settings = settings.Select(o => new PubSubChannelSetting() { Name = o.Key, Value = o.Value }).ToList()
            });
        }

        /// <summary>
        /// Registers a new subscription
        /// </summary>
        public PubSubSubscriptionDefinition RegisterSubscription<TModel>(string name, PubSubEventType events, Expression<Func<TModel, bool>> filter, Guid channelId)
        {
            return this.Insert(new PubSubSubscriptionDefinition()
            {
                ChannelKey = channelId,
                Event = events,
                Filter = new List<string>() { new NameValueCollection(QueryExpressionBuilder.BuildQuery(filter).ToArray()).ToString() },
                IsActive = true,
                Name = name,
                ResourceTypeXml = typeof(TModel).GetSerializationName()
            });
        }

        /// <summary>
        /// Remove the channel
        /// </summary>
        public PubSubChannelDefinition RemoveChannel(Guid id)
        {
            return (this as IRepositoryService<PubSubChannelDefinition>).Obsolete(id);
        }

        /// <summary>
        /// Remove the subscription
        /// </summary>
        public PubSubSubscriptionDefinition RemoveSubscription(Guid id)
        {
            return (this as IRepositoryService<PubSubSubscriptionDefinition>).Obsolete(id);
        }

        /// <summary>
        /// Get subscription channel
        /// </summary>
        public PubSubChannelDefinition Get(Guid key)
        {
            return this.GetChannel(key);
        }

        /// <summary>
        /// Get specified version of channel
        /// </summary>
        /// <param name="key"></param>
        /// <param name="versionKey"></param>
        /// <returns></returns>
        public PubSubChannelDefinition Get(Guid key, Guid versionKey) => (this as IRepositoryService<PubSubChannelDefinition>).Get(key);

        /// <summary>
        /// Find the specified object
        /// </summary>
        public IEnumerable<PubSubChannelDefinition> Find(Expression<Func<PubSubChannelDefinition, bool>> query)
        {
            return this.Find(query, 0, 100, out int _);
        }

        /// <summary>
        /// Find specified channel definitions
        /// </summary>
        public IEnumerable<PubSubChannelDefinition> Find(Expression<Func<PubSubChannelDefinition, bool>> query, int offset, int? count, out int totalResults, params ModelSort<PubSubChannelDefinition>[] orderBy)
        {
            using (var conn = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    conn.Open();
                    var domainFilter = this.m_mapper.MapModelExpression<PubSubChannelDefinition, DbChannel, bool>(query, true);
                    var retVal = conn.Query(domainFilter);
                    totalResults = retVal.Count();
                    return retVal.Skip(offset).Take(count ?? 100).ToList().Select(o =>
                    {
                        var cv = this.m_cache?.GetCacheItem<PubSubChannelDefinition>(o.Key);
                        if (cv != null)
                            return cv;
                        else
                        {
                            var rv = this.m_mapper.MapDomainInstance<DbChannel, PubSubChannelDefinition>(o, true);
                            rv.Settings = conn.Query<DbChannelSetting>(r => r.ChannelKey == rv.Key).Select(r => new PubSubChannelSetting() { Name = r.Name, Value = r.Value }).ToList();
                            this.m_cache?.Add(rv);
                            return rv;
                        }
                    });
                }
                catch (Exception e)
                {
                    throw new Exception($"Error querying for channels {query}", e);
                }
            }
        }

        /// <summary>
        /// Insert pub-sub channel definition
        /// </summary>
        public PubSubChannelDefinition Insert(PubSubChannelDefinition data)
        {
            if (!typeof(IPubSubDispatcherFactory).IsAssignableFrom(data.DispatcherFactoryType))
                throw new InvalidOperationException("Dispatcher factory is of invalid type");

            using (var conn = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        var dbChannel = this.m_mapper.MapModelInstance<PubSubChannelDefinition, DbChannel>(data);
                        data.CreatedByKey = dbChannel.CreatedBy = this.m_securityRepository.GetUser(AuthenticationContext.Current.Principal.Identity).Key.Value;
                        data.CreationTime = dbChannel.CreationTime = DateTimeOffset.Now;
                        dbChannel = conn.Insert(dbChannel);

                        // Insert settings
                        foreach (var itm in data.Settings)
                            conn.Insert(new DbChannelSetting()
                            {
                                ChannelKey = dbChannel.Key,
                                Name = itm.Name,
                                Value = itm.Value
                            });

                        tx.Commit();
                        data.Key = dbChannel.Key;

                        this.m_cache?.Add(data);
                        return data;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Error creating channel {data}", e);
                }
            }
        }

        /// <summary>
        /// Update Pub-Sub Channel information
        /// </summary>
        public PubSubChannelDefinition Save(PubSubChannelDefinition data)
        {
            if (!typeof(IPubSubDispatcherFactory).IsAssignableFrom(data.DispatcherFactoryType))
                throw new InvalidOperationException("Dispatcher factory is of invalid type");

            using (var conn = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        var dbExisting = conn.FirstOrDefault<DbChannel>(o => o.Key == data.Key);
                        if (dbExisting == null)
                            throw new KeyNotFoundException($"Channel {data.Key} not found");
                        data.UpdatedByKey = dbExisting.UpdatedBy = this.m_securityRepository.GetUser(AuthenticationContext.Current.Principal.Identity).Key.Value;
                        data.UpdatedTime = dbExisting.UpdatedTime = DateTimeOffset.Now;
                        data.CreatedByKey = dbExisting.CreatedBy;
                        data.CreationTime = dbExisting.CreationTime;
                        dbExisting.ObsoletedBy = null;
                        dbExisting.ObsoletionTime = null;
                        dbExisting.ObsoletedBySpecified = dbExisting.ObsoletionTimeSpecified = true;
                        dbExisting.Name = data.Name;
                        dbExisting.IsActive = data.IsActive;
                        conn.Update(dbExisting);
                        conn.Delete<DbChannelSetting>(o => o.ChannelKey == data.Key);
                        // Insert settings
                        foreach (var itm in data.Settings)
                            conn.Insert(new DbChannelSetting()
                            {
                                ChannelKey = dbExisting.Key,
                                Name = itm.Name,
                                Value = itm.Value
                            });

                        this.m_cache?.Add(data);

                        tx.Commit();
                        return data;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Error updating channel {data}", e);
                }
            }
        }

        /// <summary>
        /// Delete the specified channel
        /// </summary>
        public PubSubChannelDefinition Obsolete(Guid key)
        {
            using (var conn = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    conn.Open();
                    var dbExisting = conn.FirstOrDefault<DbChannel>(o => o.Key == key);
                    if (dbExisting == null)
                        throw new KeyNotFoundException($"Channel {key} not found");
                    dbExisting.ObsoletedBy = this.m_securityRepository.GetUser(AuthenticationContext.Current.Principal.Identity).Key.Value;
                    dbExisting.ObsoletionTime = DateTimeOffset.Now;
                    conn.Update(dbExisting);
                    this.m_cache?.Remove(key);

                    return this.m_mapper.MapDomainInstance<DbChannel, PubSubChannelDefinition>(dbExisting, false);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error obsoleting channel {key}", e);
                }
            }
        }

        /// <summary>
        /// Get the specified channel object
        /// </summary>
        PubSubSubscriptionDefinition IRepositoryService<PubSubSubscriptionDefinition>.Get(Guid key) => (this as IRepositoryService<PubSubSubscriptionDefinition>).Get(key, Guid.Empty);

        /// <summary>
        /// Get a specific verison
        /// </summary>
        PubSubSubscriptionDefinition IRepositoryService<PubSubSubscriptionDefinition>.Get(Guid key, Guid versionKey)
        {
            var cache = this.m_cache?.GetCacheItem<PubSubSubscriptionDefinition>(key);
            if (cache != null)
                return cache;
            else 
                return (this as IRepositoryService<PubSubSubscriptionDefinition>).Find(o => o.Key == key, 0, 1, out int _).FirstOrDefault();
        }

        /// <summary>
        /// Find the specified subscription definitions
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public IEnumerable<PubSubSubscriptionDefinition> Find(Expression<Func<PubSubSubscriptionDefinition, bool>> query)
        {
            return this.Find(query, 0, 100, out int _);
        }

        /// <summary>
        /// Find specified subscription definitions
        /// </summary>
        public IEnumerable<PubSubSubscriptionDefinition> Find(Expression<Func<PubSubSubscriptionDefinition, bool>> query, int offset, int? count, out int totalResults, params ModelSort<PubSubSubscriptionDefinition>[] orderBy)
        {
            using (var conn = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    conn.Open();
                    var domainFilter = this.m_mapper.MapModelExpression<PubSubSubscriptionDefinition, DbSubscription, bool>(query, true);
                    var retVal = conn.Query(domainFilter);

                    totalResults = retVal.Count();

                    return retVal.Skip(offset).Take(count ?? 100).ToList().Select(o =>
                    {
                        var rv = this.m_cache?.GetCacheItem<PubSubSubscriptionDefinition>(o.Key);
                        if (rv != null)
                            return rv;
                        else
                        {
                            rv = this.m_mapper.MapDomainInstance<DbSubscription, PubSubSubscriptionDefinition>(o, true);
                            rv.Filter = conn.Query<DbSubscriptionFilter>(r => r.SubscriptionKey == rv.Key).Select(r => r.Filter).ToList();
                            this.m_cache?.Add(rv);
                            return rv;
                        }
                    });
                }
                catch (Exception e)
                {
                    throw new Exception($"Error querying for subscriptions {query}", e);
                }
            }
        }

        /// <summary>
        /// Insert a subscription definition
        /// </summary>
        public PubSubSubscriptionDefinition Insert(PubSubSubscriptionDefinition data)
        {
            var preEvent = new DataPersistingEventArgs<PubSubSubscriptionDefinition>(data, AuthenticationContext.Current.Principal);
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
                    using(var tx = conn.BeginTransaction())
                    {

                        // First construct db instance 
                        var dbSubscription = this.m_mapper.MapModelInstance<PubSubSubscriptionDefinition, DbSubscription>(data);
                        data.CreatedByKey = dbSubscription.CreatedBy = this.m_securityRepository.GetUser(AuthenticationContext.Current.Principal.Identity).Key.Value;
                        data.CreationTime = dbSubscription.CreationTime = DateTimeOffset.Now;
                        dbSubscription = conn.Insert(dbSubscription);

                        // Insert settings
                        foreach (var itm in data.Filter)
                            conn.Insert(new DbSubscriptionFilter()
                            {
                                SubscriptionKey = dbSubscription.Key,
                                Filter= itm
                            });

                        tx.Commit();
                        data.Key = dbSubscription.Key;

                        this.Subscribed?.Invoke(this, new DataPersistedEventArgs<PubSubSubscriptionDefinition>(data, AuthenticationContext.Current.Principal));
                        this.m_cache?.Add(data);
                        return data;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Error inserting subscription {data}", e);
                }
            }
        }

        /// <summary>
        /// Update a subscription
        /// </summary>
        public PubSubSubscriptionDefinition Save(PubSubSubscriptionDefinition data)
        {
            using (var conn = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        var dbExisting = conn.FirstOrDefault<DbSubscription>(o => o.Key == data.Key);
                        if (dbExisting == null)
                            throw new KeyNotFoundException($"Subscription {data.Key} not found");
                        data.UpdatedByKey = dbExisting.UpdatedBy = this.m_securityRepository.GetUser(AuthenticationContext.Current.Principal.Identity).Key.Value;
                        data.UpdatedTime = dbExisting.UpdatedTime = DateTimeOffset.Now;
                        data.CreatedByKey = dbExisting.CreatedBy;
                        data.CreationTime = dbExisting.CreationTime;
                        dbExisting.ObsoletedBy = null;
                        dbExisting.ObsoletionTime = null;
                        dbExisting.ObsoletedBySpecified = dbExisting.ObsoletionTimeSpecified = true;
                        dbExisting.Name = data.Name;
                        dbExisting.IsActive = data.IsActive;
                        dbExisting.NotAfter = data.NotAfter;
                        dbExisting.NotBefore = data.NotBefore;
                        dbExisting.ChannelKey = data.ChannelKey;

                        conn.Update(dbExisting);
                        conn.Delete<DbSubscriptionFilter>(o => o.SubscriptionKey == data.Key);
                        // Insert settings
                        foreach (var itm in data.Filter)
                            conn.Insert(new DbSubscriptionFilter()
                            {
                                SubscriptionKey = dbExisting.Key,
                                Filter = itm
                            });

                        this.m_cache?.Add(data);
                        tx.Commit();
                        return data;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Error updating channel {data}", e);
                }
            }
        }

        /// <summary>
        /// Obsolete the specified key
        /// </summary>
        PubSubSubscriptionDefinition IRepositoryService<PubSubSubscriptionDefinition>.Obsolete(Guid key)
        {
            var data = (this as IRepositoryService<PubSubSubscriptionDefinition>).Get(key);
            if (data == null)
                throw new KeyNotFoundException($"Subscription {key} not found");

            var preEvt = new DataPersistingEventArgs<PubSubSubscriptionDefinition>(data, AuthenticationContext.Current.Principal);
            this.UnSubscribing?.Invoke(this, preEvt);
            if(preEvt.Cancel)
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
                    data.ObsoletedByKey = dbExisting.ObsoletedBy = this.m_securityRepository.GetUser(AuthenticationContext.Current.Principal.Identity).Key.Value;
                    data.ObsoletionTime = dbExisting.ObsoletionTime = DateTimeOffset.Now;
                    conn.Update(dbExisting);
                    this.m_cache.Remove(key);
                    return this.m_mapper.MapDomainInstance<DbSubscription, PubSubSubscriptionDefinition>(dbExisting, false);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error obsoleting subscription {key}", e);
                }
            }
        }
    }
}
