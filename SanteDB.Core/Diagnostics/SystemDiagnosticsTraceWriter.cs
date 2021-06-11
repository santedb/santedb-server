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
using SanteDB.Core.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Server.Core.Diagnostics
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

        private TraceEventType Classify(EventLevel level)
        {
            switch (level)
            {
                case EventLevel.Critical:
                    return TraceEventType.Critical;
                case EventLevel.Error:
                    return TraceEventType.Error;
                case EventLevel.Informational:
                    return TraceEventType.Information;
                case EventLevel.Verbose:
                    return TraceEventType.Verbose;
                case EventLevel.Warning:
                    return TraceEventType.Warning;
                default:
                    return TraceEventType.Information;
            }
        }
        /// <summary>
        /// Write the specified trace
        /// </summary>
        protected override void WriteTrace(EventLevel level, string source, string format, params object[] args)
        {
            this.m_traceSource.TraceEvent(this.Classify(level), 0, format, args);
        }

        /// <summary>
        /// Trace event data 
        /// </summary>
        public override void TraceEventWithData(EventLevel level, string source, string message, object[] data)
        {
            this.m_traceSource.TraceData(this.Classify(level), 0, data);
        }
    }
}
