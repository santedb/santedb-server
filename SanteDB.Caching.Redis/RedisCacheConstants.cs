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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Caching.Redis
{
    /// <summary>
    /// REDIS constants
    /// </summary>
    internal static class RedisCacheConstants
    {
        /// <summary>
        /// Trace source name
        /// </summary>
        public const string TraceSourceName = "SanteDB.Caching.Redis";

        /// <summary>
        /// Database ID for cache
        /// </summary>
        public const int CacheDatabaseId = 0;

        /// <summary>
        /// Database identifier
        /// </summary>
        public const int QueryDatabaseId = 1;

        /// <summary>
        /// Adhoc cache database id
        /// </summary>
        public const int AdhocCacheDatabaseId = 2;
    }
}
