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
 * Date: 2020-1-12
 */
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
                var db = RedisConnectionManager.Current.Connection.GetDatabase(RedisCacheConstants.AdhocCacheDatabaseId);
                db.StringSet(key, JsonConvert.SerializeObject(value), expiry: timeout ?? this.m_configuration.TTL, flags: CommandFlags.FireAndForget);
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
                var db = RedisConnectionManager.Current.Connection?.GetDatabase(RedisCacheConstants.AdhocCacheDatabaseId);
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
                this.m_tracer.TraceInfo("Using shared REDIS cache {0}", RedisConnectionManager.Current.Connection);

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
            RedisConnectionManager.Current.Dispose();
            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Remove the object from the cache
        /// </summary>
        public bool Remove(string key)
        {
            try
            {
                var db = RedisConnectionManager.Current.Connection?.GetDatabase(RedisCacheConstants.AdhocCacheDatabaseId);
                db?.KeyDelete(key);
                return true;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error removing entity from ad-hoc cache : {0}", e);
                return false;
            }
        }
    }
}
