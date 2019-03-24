using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Caching.Memory
{
    /// <summary>
    /// Memory cache constants
    /// </summary>
    internal static class MemoryCacheConstants
    {

        /// <summary>
        /// Trace source name
        /// </summary>
        public const string TraceSourceName = "SanteDB.Caching.Memory";

        /// <summary>
        /// Memory query persistence
        /// </summary>
        public const string QueryTraceSourceName = TraceSourceName + ".Query";
    }
}
