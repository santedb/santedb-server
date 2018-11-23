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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MARC.HI.EHRS.SVC.Core.Data;
using System.Diagnostics;
using SanteDB.Core.Diagnostics;
using MARC.HI.EHRS.SVC.Core.Timer;
using System.Timers;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;

namespace SanteDB.Core.Query
{


    /// <summary>
    /// Represents a simple query persistence service that uses local memory for query continuation
    /// </summary>
    public class MemoryQueryPersistenceService : SanteDB.Core.Services.IQueryPersistenceService, ITimerJob, IDaemonService
    {

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
        }

        // Tracer
        private TraceSource m_tracer = new TraceSource("SanteDB.Core.Query.MemoryQueryPersistence");

        // Memory cache of queries
        private Dictionary<Guid, MemoryQueryInfo> m_queryCache = new Dictionary<Guid, MemoryQueryInfo>(10);

        // Sync object
        private Object m_syncObject = new object();

        /// <summary>
        /// The daemon is starting
        /// </summary>
        public event EventHandler Starting;
        /// <summary>
        /// The daemon is stopping
        /// </summary>
        public event EventHandler Stopping;
        /// <summary>
        /// The damon has started
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// The daemon has stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Gets whether the daemon is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Clear
        /// </summary>
        public void Clear()
        {
            this.m_queryCache.Clear();
        }


        /// <summary>
        /// Add results to id set
        /// </summary>
        public void AddResults(Guid queryId, IEnumerable<Guid> results)
        {
            MemoryQueryInfo retVal = null;
            if (this.m_queryCache.TryGetValue(queryId, out retVal))
            {
                this.m_tracer.TraceVerbose("Updating query {0} ({1} results)", queryId, results.Count());
                lock (retVal.Results)
                    retVal.Results.AddRange(results.Where(o => !retVal.Results.Contains(o)).Select(o => o));

                //retVal.TotalResults = retVal.Results.Count();
            }
        }

        /// <summary>
        /// Get query results
        /// </summary>
        public IEnumerable<Guid> GetQueryResults(Guid queryId, int startRecord, int nRecords)
        {
            MemoryQueryInfo retVal = null;
            if (this.m_queryCache.TryGetValue(queryId, out retVal))
                lock(retVal.Results)
                    return retVal.Results.ToArray().Distinct().Skip(startRecord).Take(nRecords).OfType<Guid>().ToArray();
            return null;
        }

        /// <summary>
        /// Get query tag
        /// </summary>
        public object GetQueryTag(Guid queryId)
        {
            MemoryQueryInfo retVal = null;
            if (this.m_queryCache.TryGetValue(queryId, out retVal))
                return retVal.QueryTag;
            return null;
        }

        /// <summary>
        /// True if registered
        /// </summary>
        public bool IsRegistered(Guid queryId)
        {
            return this.m_queryCache.ContainsKey(queryId);
        }

        /// <summary>
        /// Get total results
        /// </summary>
        public long QueryResultTotalQuantity(Guid queryId)
        {
            MemoryQueryInfo retVal = null;
            if (this.m_queryCache.TryGetValue(queryId, out retVal))
                return retVal.TotalResults;
            return 0;
        }

        /// <summary>
        /// Register a query
        /// </summary>
        public bool RegisterQuerySet(Guid queryId, IEnumerable<Guid> results, object tag, int totalResults)
        {
            lock (this.m_syncObject)
            {
                MemoryQueryInfo retVal = null;
                if (this.m_queryCache.TryGetValue(queryId, out retVal))
                {
                    this.m_tracer.TraceVerbose("Updating query {0} ({1} results)", queryId, results.Count());
                    retVal.Results = results.Select(o => o).ToList();
                    retVal.QueryTag = tag;
                    retVal.TotalResults = totalResults;
                }
                else
                {
                    this.m_tracer.TraceVerbose("Registering query {0} ({1} results)", queryId, results.Count());

                    this.m_queryCache.Add(queryId, new MemoryQueryInfo()
                    {
                        QueryTag = tag,
                        Results = results.Select(o => o).ToList(),
                        TotalResults = totalResults
                    });
                }
            }
            return true;
        }

        /// <summary>
        /// Start the service
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            this.Starting?.Invoke(this, EventArgs.Empty);

            ApplicationContext.Current.Started += (o, e) =>
            {
                var timerService = ApplicationContext.Current.GetService<ITimerService>();
                if (timerService?.IsJobRegistered(typeof(MemoryQueryPersistenceService)) == false)
                    timerService.AddJob(this, new TimeSpan(4, 0, 0));
            };

            this.Started?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Stop the service
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);
            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Timer has elapsed
        /// </summary>
        public void Elapsed(object sender, ElapsedEventArgs e)
        {
#if DEBUG
            this.m_tracer.TraceInformation("Cleaning stale queries from memory...");
#endif

            try
            {
                lock (this.m_syncObject)
                {
                    DateTime now = DateTime.Now;
                    var garbageBin = this.m_queryCache.Where(o => now.Subtract(o.Value.CreationTime).TotalMinutes == 30).Select(o => o.Key);
                    foreach (var itm in garbageBin)
                        this.m_queryCache.Remove(itm);// todo configuration
                }
            }
            catch (Exception ex)
            {
                this.m_tracer.TraceError(ex.ToString());
            }
        }

        /// <summary>
        /// Find the query ID by the tagged value of that query
        /// </summary>
        public Guid FindQueryId(object queryTag)
        {
            return this.m_queryCache.FirstOrDefault(o => o.Value.QueryTag.Equals(queryTag)).Key;
        }

        /// <summary>
        /// Set the query tag
        /// </summary>
        public void SetQueryTag(Guid queryId, object tagValue)
        {
            MemoryQueryInfo queryInfo = null;
            if (this.m_queryCache.TryGetValue(queryId, out queryInfo))
                queryInfo.QueryTag = tagValue;
        }
    }
}
