/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2021-8-5
 */
using SanteDB.Core.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    [DisplayName("System/Debugger Trace Writer")]
    public class SystemDiagnosticsTraceWriter : TraceWriter
    {

        // Trace source
        private TraceSource m_traceSource = new TraceSource("SanteDB");

        /// <summary>
        /// CTOR for diagnostics
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="fileName"></param>
        public SystemDiagnosticsTraceWriter(EventLevel filter, string fileName, IDictionary<String, EventLevel> sources) : base(filter, fileName, sources)
        {
        }

        /// <summary>
        /// Creates a new diagnostics trace writer
        /// </summary>
        public SystemDiagnosticsTraceWriter() : base (EventLevel.LogAlways, null, new Dictionary<String, EventLevel>())
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
