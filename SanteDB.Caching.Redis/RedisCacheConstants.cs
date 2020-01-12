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
