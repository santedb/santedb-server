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
 * Date: 2019-11-27
 */
using SanteDB.Caching.Memory.Configuration;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Jobs;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Timers;

namespace SanteDB.Caching.Memory
{


    /// <summary>
    /// Represents a simple query persistence service that uses local memory for query continuation
    /// </summary>
    [ServiceProvider("Memory-Based Query Persistence Service")]
    public class MemoryQueryPersistenceService : SanteDB.Core.Services.IQueryPersistenceService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Memory-Based Query Persistence / Continuation Service";

        /// <summary>
        /// Memory based query information
        /// </summary>
        public class MemoryQueryInfo
        {
            /// <summary>
            /// Query info ctor
            /// </summary>
            public MemoryQueryInfo()
            {
                this.CreationTime = DateTime.Now;
            }

            /// <summary>
            /// Total results
            /// </summary>
            public int TotalResults { get; set; }

            /// <summary>
            /// Results in the result set
            /// </summary>
            public List<Guid> Results { get; set; }

            /// <summary>
            /// The query tag
            /// </summary>
            public object QueryTag { get; set; }

            /// <summary>
            /// Get or sets the creation time
            /// </summary>
            public DateTime CreationTime { get; private set; }

            /// <summary>
            /// Get or sets the key
            /// </summary>
            public Guid Key { get; set; }
        }

        //  trace source
        private Tracer m_tracer = new Tracer(MemoryCacheConstants.TraceSourceName);
        private MemoryCacheConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<MemoryCacheConfigurationSection>();
        private MemoryCache m_cache;

        /// <summary>
        /// Create new persistence
        /// </summary>
        public MemoryQueryPersistenceService()
        {
            var config = new NameValueCollection();
            config.Add("cacheMemoryLimitMegabytes", this.m_configuration.MaxCacheSize.ToString());
            config.Add("pollingInterval", "00:05:00");
            this.m_cache = new MemoryCache("santedb.query", config);
        }

        /// <summary>
        /// Clear
        /// </summary>
        public void Clear()
        {
            this.m_cache.Trim(100);
        }

        /// <summary>
        /// Add results to id set
        /// </summary>
        public void AddResults(Guid queryId, IEnumerable<Guid> results, int totalResults)
        {
            var cacheResult = this.m_cache.GetCacheItem($"qry.{queryId}");
            if (cacheResult == null)
                return; // no item
            else if (cacheResult.Value is MemoryQueryInfo retVal)
            {
                this.m_tracer.TraceVerbose("Updating query {0} ({1} results)", queryId, results.Count());
                lock (retVal.Results) 
                    retVal.Results.AddRange(results.Where(o => !retVal.Results.Contains(o)).Select(o => o));
                retVal.TotalResults = totalResults;
                this.m_cache.Set(cacheResult.Key, cacheResult.Value, DateTimeOffset.Now.AddSeconds(this.m_configuration.MaxQueryAge));
                //retVal.TotalResults = retVal.Results.Count();
            }
        }

        /// <summary>
        /// Get query results
        /// </summary>
        public IEnumerable<Guid> GetQueryResults(Guid queryId, int startRecord, int nRecords)
        {
            var cacheResult = this.m_cache.Get($"qry.{queryId}");
            if (cacheResult is MemoryQueryInfo retVal)
                lock (retVal.Results)
                    return retVal.Results.ToArray().Distinct().Skip(startRecord).Take(nRecords).OfType<Guid>().ToArray();
            return null;
        }

        /// <summary>
        /// Get query tag
        /// </summary>
        public object GetQueryTag(Guid queryId)
        {
            var cacheResult = this.m_cache.Get($"qry.{queryId}");
            if (cacheResult is MemoryQueryInfo retVal)
                return retVal.QueryTag;
            return null;
        }

        /// <summary>
        /// True if registered
        /// </summary>
        public bool IsRegistered(Guid queryId)
        {
            return this.m_cache.Contains($"qry.{queryId}");
        }

        /// <summary>
        /// Get total results
        /// </summary>
        public long QueryResultTotalQuantity(Guid queryId)
        {
            var cacheResult = this.m_cache.Get($"qry.{queryId}");
            if (cacheResult is MemoryQueryInfo retVal)
                return retVal.TotalResults;
            return 0;
        }

        /// <summary>
        /// Register a query
        /// </summary>
        public bool RegisterQuerySet(Guid queryId, IEnumerable<Guid> results, object tag, int totalResults)
        {

            this.m_cache.Set($"qry.{queryId}", new MemoryQueryInfo()
            {
                QueryTag = tag,
                Results = results.Select(o => o).ToList(),
                TotalResults = totalResults,
                Key = queryId
            }, DateTimeOffset.Now.AddSeconds(this.m_configuration.MaxQueryAge));
            return true;

        }

        /// <summary>
        /// Find the query ID by the tagged value of that query
        /// </summary>
        public Guid FindQueryId(object queryTag)
        {
            return this.m_cache.Select(o=>o.Value).OfType<MemoryQueryInfo>().FirstOrDefault(o => o.QueryTag.Equals(queryTag))?.Key ?? Guid.Empty;
        }

        /// <summary>
        /// Set the query tag
        /// </summary>
        public void SetQueryTag(Guid queryId, object tagValue)
        {
            var cacheResult = this.m_cache.Get($"qry.{queryId}");
            if (cacheResult is MemoryQueryInfo retVal)
                retVal.QueryTag = tagValue;
        }

      
    }
}
