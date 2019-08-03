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

        // Trace source
        private TraceSource m_traceSource = new TraceSource("SanteDB");

        /// <summary>
        /// CTOR for diagnostics
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="fileName"></param>
        public SystemDiagnosticsTraceWriter(EventLevel filter, string fileName) : base(filter, fileName)
        {
        }

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

            this.m_traceSource.TraceEvent(eventLvl, 0, format, args);
        }
    }
}
