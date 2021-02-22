using SanteDB.Caching.Memory.Configuration;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Caching.Memory
{
    /// <summary>
    /// REDIS ad-hoc cache
    /// </summary>
    public class MemoryAdhocCacheService : IAdhocCacheService
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Memory Ad-Hoc Caching Service";

        //  trace source
        private Tracer m_tracer = new Tracer(MemoryCacheConstants.TraceSourceName);
        private MemoryCacheConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<MemoryCacheConfigurationSection>();
        private MemoryCache m_cache;

        /// <summary>
        /// Adhoc cache init
        /// </summary>
        public MemoryAdhocCacheService()
        {
            var config = new NameValueCollection();
            config.Add("cacheMemoryLimitMegabytes", this.m_configuration.MaxCacheSize.ToString());
            config.Add("pollingInterval", "00:05:00");

            this.m_cache = new MemoryCache("santedb.adhoc", config);
        }
       

        /// <summary>
        /// Add the specified data to the cache
        /// </summary>
        public void Add<T>(string key, T value, TimeSpan? timeout = null)
        {
            try
            {
                if (Object.Equals(value, default(T))) return;
                this.m_cache.Set(key, value, DateTimeOffset.Now.AddSeconds(timeout?.TotalSeconds ?? this.m_configuration.MaxCacheAge));
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
                var data = this.m_cache.Get(key);
                if(data == null)
                    return default(T);
                return (T)data;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error fetch {0} from cache", key);
                //throw new Exception($"Error fetching {key} ({typeof(T).FullName}) from cache", e);
                return default(T);
            }
        }

        /// <summary>
        /// Remove the specified key
        /// </summary>
        public bool Remove(string key)
        {
            return this.m_cache.Remove(key) != null;
        }
    }
}
