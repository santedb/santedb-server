using Newtonsoft.Json;
using SanteDB.Caching.Redis.Configuration;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Services;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Caching.Redis
{
    /// <summary>
    /// REDIS ad-hoc cache
    /// </summary>
    public class RedisAdhocCache : IAdhocCacheService, IDaemonService
    {
       
        /// <summary>
        /// True if service is running
        /// </summary>
        public bool IsRunning => this.m_configuration != null;

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "REDIS Ad-Hoc Caching Service";

        // Redis trace source
        private Tracer m_tracer = new Tracer(RedisCacheConstants.TraceSourceName);

        // Connection
        private ConnectionMultiplexer m_connection;

        // Configuration
        private RedisConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<RedisConfigurationSection>();

        /// <summary>
        /// Application daemon is starting
        /// </summary>
        public event EventHandler Starting;
        /// <summary>
        /// Application daemon has started
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Application is stopping
        /// </summary>
        public event EventHandler Stopping;
        /// <summary>
        /// Application has stopped
        /// </summary>
        public event EventHandler Stopped;
        

        /// <summary>
        /// Add the specified data to the cache
        /// </summary>
        public void Add<T>(string key, T value, TimeSpan? timeout = null)
        {
            try
            {
                var db = this.m_connection.GetDatabase(RedisCacheConstants.AdhocCacheDatabaseId);
                db.StringSet(key, JsonConvert.SerializeObject(value), expiry: timeout);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error adding {0} to cache", value);
                //throw new Exception($"Error adding {value} to cache", e);
            }
        }

        /// <summary>
        /// Gets the specified value from the cache
        /// </summary>
        public T Get<T>(string key)
        {
            try
            {
                var db = this.m_connection?.GetDatabase(RedisCacheConstants.AdhocCacheDatabaseId);
                var str = db?.StringGet(key);
                if (!String.IsNullOrEmpty(str))
                    return JsonConvert.DeserializeObject<T>(str);
                else
                    return default(T);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error fetch {0} from cache", key);
                //throw new Exception($"Error fetching {key} ({typeof(T).FullName}) from cache", e);
                return default(T);
            }
        }

        /// <summary>
        /// Start the specified service
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            try
            {
                this.Starting?.Invoke(this, EventArgs.Empty);

                this.m_tracer.TraceInfo("Starting REDIS ad-cache service to hosts {0}...", String.Join(";", this.m_configuration.Servers));

                var configuration = new ConfigurationOptions()
                {
                    Password = this.m_configuration.Password
                };
                foreach (var itm in this.m_configuration.Servers)
                    configuration.EndPoints.Add(itm);

                this.m_connection = ConnectionMultiplexer.Connect(configuration);

                this.Started?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error starting REDIS query persistence, will switch to query persister : {0}", e);
                ApplicationServiceContext.Current.GetService<IServiceManager>().RemoveServiceProvider(typeof(RedisAdhocCache));
                ApplicationServiceContext.Current.GetService<IServiceManager>().RemoveServiceProvider(typeof(IDataCachingService));
                return false;
            }
        }

        /// <summary>
        /// Stops the connection broker
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);
            this.m_connection.Dispose();
            this.m_connection = null;
            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }
}
