using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Diagnostics
{
    /// <summary>
    /// Represents a trace writer to Tracer
    /// </summary>
    public class SystemDiagnosticsTraceWriter : TraceWriter
    {

        /// <summary>
        /// Creates a new diagnostics trace writer
        /// </summary>
        public SystemDiagnosticsTraceWriter() : base (EventLevel.LogAlways, null)
        {
        }

        /// <summary>
        /// Write the specified trace
        /// </summary>
        protected override void WriteTrace(EventLevel level, string source, string format, params object[] args)
        {
            TraceEventType eventLvl = TraceEventType.Information;
            switch (level)
            {
                case EventLevel.Critical:
                    eventLvl = TraceEventType.Critical;
                    break;
                case EventLevel.Error:
                    eventLvl = TraceEventType.Error;
                    break;
                case EventLevel.Informational:
                    eventLvl = TraceEventType.Information;
                    break;
                case EventLevel.Verbose:
                    eventLvl = TraceEventType.Verbose;
                    break;
                case EventLevel.Warning:
                    eventLvl = TraceEventType.Warning;
                    break;
            }

            new TraceSource(source).TraceEvent(eventLvl, 0, format, args);
        }
    }
}
