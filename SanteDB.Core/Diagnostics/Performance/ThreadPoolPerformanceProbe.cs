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
 * Date: 2019-12-24
 */
using SanteDB.Core.Services;
using SanteDB.Core.Services.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Diagnostics.Performance
{

    /// <summary>
    /// Represents a thread pool performance counter
    /// </summary>
    public class ThreadPoolPerformanceProbe : ICompositeDiagnosticsProbe
    {


        // Performance counters
        private IDiagnosticsProbe[] m_performanceCounters =
        {
            new NonPooledWorkersProbe(),
            new PoolConcurrencyProbe(),
            new ErroredWorkersProbe(),
            new PooledWorkersProbe()
        };

        /// <summary>
        /// Generic performance counter
        /// </summary>
        private class NonPooledWorkersProbe : DiagnosticsProbeBase<int>
        {
            /// <summary>
            /// Non pooled workers counter
            /// </summary>
            public NonPooledWorkersProbe() : base("ThreadPool: Non-Pooled Workers", "Shows the number of active threads which are not in the thread pool")
            {

            }

            /// <summary>
            /// Gets the identifier for the pool
            /// </summary>
            public override Guid Uuid => SanteDBConstants.ThreadPoolNonQueuedWorkerCounter;

            /// <summary>
            /// Gets the value
            /// </summary>
            public override int Value => ApplicationContext.Current.GetService<ThreadPoolService>().NonPooledWorkers;

        }

        /// <summary>
        /// Generic performance counter
        /// </summary>
        private class PooledWorkersProbe : DiagnosticsProbeBase<int>
        {
            /// <summary>
            /// Non pooled workers counter
            /// </summary>
            public PooledWorkersProbe() : base("ThreadPool: Pooled Workers", "Shows the number of active threads in the thread pool")
            {

            }

            /// <summary>
            /// Gets the identifier for the pool
            /// </summary>
            public override Guid Uuid => SanteDBConstants.ThreadPoolWorkerCounter;

            /// <summary>
            /// Gets the value
            /// </summary>
            public override int Value => ApplicationContext.Current.GetService<ThreadPoolService>().PooledWorkers;

        }

        /// <summary>
        /// Generic performance counter
        /// </summary>
        private class PoolConcurrencyProbe : DiagnosticsProbeBase<int>
        {
            /// <summary>
            /// Non pooled workers counter
            /// </summary>
            public PoolConcurrencyProbe() : base("ThreadPool: Thread pool size", "Shows the total number of threads which are allocated to the thread pool")
            {

            }

            /// <summary>
            /// Gets the identifier for the pool
            /// </summary>
            public override Guid Uuid => SanteDBConstants.ThreadPoolConcurrencyCounter;


            /// <summary>
            /// Gets the value
            /// </summary>
            public override int Value => ApplicationContext.Current.GetService<ThreadPoolService>().Concurrency;

        }

        /// <summary>
        /// Generic performance counter
        /// </summary>
        private class ErroredWorkersProbe : DiagnosticsProbeBase<int>
        {
            /// <summary>
            /// Non pooled workers counter
            /// </summary>
            public ErroredWorkersProbe() : base("ThreadPool: Worker Errors", "Shows the total number of workers that didn't successfully complete due to an uncaught exception")
            {

            }

            /// <summary>
            /// Gets the identifier for the pool
            /// </summary>
            public override Guid Uuid => SanteDBConstants.ThreadPoolErrorWorkerCounter;

            /// <summary>
            /// Gets the value
            /// </summary>
            public override int Value => ApplicationContext.Current.GetService<ThreadPoolService>().ErroredWorkers;

        }

        /// <summary>
        /// Get the UUID of the thread pool
        /// </summary>
        public Guid Uuid => SanteDBConstants.ThreadPoolPerformanceCounter;

        /// <summary>
        /// Gets the value of the 
        /// </summary>
        public IEnumerable<IDiagnosticsProbe> Value => this.m_performanceCounters;

        /// <summary>
        /// Gets thename of hte composite
        /// </summary>
        public string Name => "Thread Pool";

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description => "The primary SanteDB thread pool performance monitor";

        /// <summary>
        /// Gets the type of the performance counter
        /// </summary>
        public Type Type => typeof(Array);

        /// <summary>
        /// Gets the value
        /// </summary>
        object IDiagnosticsProbe.Value => this.Value;
    }
}
