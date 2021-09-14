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
using System;

namespace SanteDB.Server.Core
{
    /// <summary>
    /// SanteDB constants
    /// </summary>
    public static class SanteDBConstants
    {


        // Configuration name
        internal const string SanteDBConfigurationName = "santedb.core";

        // Security trace source
        internal const string SecurityTraceSourceName = "SanteDB.Core.Security";

        // Map trace source
        internal const string MapTraceSourceName= "SanteDB.Core.Map";

        /// <summary>
        /// SanteDB dataset installation source name
        /// </summary>
        internal const string DatasetInstallSourceName = "SanteDB.Core.DataSet";


        // WCF trace source
        internal const string WcfTraceSourceName = "SanteDB.Core.HttpRest";

        // Panic error code
        internal const string GeneralPanicErrorCode = "01189998819991197253";
        // General panic error text
        internal const string GeneralPanicErrorText = "0118 999 881 999 119 7253 - FATAL ERROR: {0}";

        /// <summary>
        /// Service trace source name
        /// </summary>
        public const string ServiceTraceSourceName = "SanteDB.Core";
        /// <summary>
        /// Data source name
        /// </summary>
        public const string DataTraceSourceName = ServiceTraceSourceName + ".Data";

        /// <summary>
        /// Trace source name for queue
        /// </summary>
        public const string QueueTraceSourceName = ServiceTraceSourceName + ".Queue";

        /// <summary>
        /// Gets the thread pooling performance counter
        /// </summary>
        public static readonly Guid ThreadPoolPerformanceCounter = new Guid("9E77D692-1F71-4442-BDA1-056D3DB1A480");

        /// <summary>
        /// Gets the thread pooling performance counter
        /// </summary>
        public static readonly Guid ThreadPoolConcurrencyCounter = new Guid("9E77D692-1F71-4442-BDA1-056D3DB1A481");

        /// <summary>
        /// Gets the thread pooling performance counter
        /// </summary>
        public static readonly Guid ThreadPoolWorkerCounter = new Guid("9E77D692-1F71-4442-BDA1-056D3DB1A482");

        /// <summary>
        /// Gets the thread pooling performance counter
        /// </summary>
        public static readonly Guid ThreadPoolNonQueuedWorkerCounter = new Guid("9E77D692-1F71-4442-BDA1-056D3DB1A483");

        /// <summary>
        /// Gets the thread pooling performance counter
        /// </summary>
        public static readonly Guid ThreadPoolErrorWorkerCounter = new Guid("9E77D692-1F71-4442-BDA1-056D3DB1A484");

        /// <summary>
        /// Gets the thread pooling performance counter
        /// </summary>
        public static readonly Guid MachinePerformanceCounter = new Guid("9E77D692-1F71-4442-BDA1-056D3DB1A485");

        /// <summary>
        /// Gets the thread pooling performance counter
        /// </summary>
        public static readonly Guid ProcessorUseCounter = new Guid("9E77D692-1F71-4442-BDA1-056D3DB1A486");

        /// <summary>
        /// Gets the thread pooling performance counter
        /// </summary>
        public static readonly Guid MemoryUseCounter = new Guid("9E77D692-1F71-4442-BDA1-056D3DB1A487");


    }
}
