/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace SanteDB.Core.Diagnostics
{
    /// <summary>
    /// Represents a trace writer which writes to the android log
    /// </summary>
    public class LogTraceWriter : TraceWriter
    {

        // PCL trace-source
        private TraceSource m_traceSource = new TraceSource("SanteDB.Core.Api");

        /// <summary>
        /// Initialize the trace writer
        /// </summary>
        public LogTraceWriter(EventLevel filter, String initializationData) : base(filter, null)
        {
        }


        #region implemented abstract members of TraceWriter
        /// <summary>
        /// Write trace
        /// </summary>
        protected override void WriteTrace(EventLevel level, string source, string format, params object[] args)
        {
           
            switch (level)
            {
                case EventLevel.Error:
                    this.m_traceSource?.TraceEvent(TraceEventType.Error, 0, $"[{source}] {format}", args);
                    break;
                case EventLevel.Informational:
                    this.m_traceSource?.TraceEvent(TraceEventType.Information, 0, $"[{source}] {format}", args);
                    break;
                case EventLevel.Critical:
                    this.m_traceSource?.TraceEvent(TraceEventType.Critical, 0, $"[{source}] {format}", args);
                    break;
                case EventLevel.Verbose:
                    this.m_traceSource?.TraceEvent(TraceEventType.Verbose, 0, $"[{source}] {format}", args);
                    break;
                case EventLevel.Warning:
                    this.m_traceSource?.TraceEvent(TraceEventType.Warning, 0, $"[{source}] {format}", args);
                    break;

            }
        }
        #endregion
    }
}

