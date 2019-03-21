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
using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace SanteDB.Core.Rest.Behavior
{
    /// <summary>
    /// Represents an endpoint behavior that logs messages
    /// </summary>
    public class MessageLoggingEndpointBehavior : IEndpointBehavior, IMessageInspector
    {

        // Trace source name
        private Tracer m_traceSource = new Tracer(SanteDBConstants.WcfTraceSourceName);

        // Correlation id
        [ThreadStatic]
        private static KeyValuePair<Guid, DateTime> httpCorrelation;

        /// <summary>
        /// After receiving the request
        /// </summary>
        /// <param name="request"></param>
        public void AfterReceiveRequest(RestRequestMessage request)
        {
            Guid httpCorrelator = Guid.NewGuid();

            // Windows we get CPU usage
            float usage = 0.0f;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                using (PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true))
                    usage = cpuCounter.NextValue();
            }

            this.m_traceSource.TraceEvent(EventLevel.Verbose, "HTTP RQO {0} : {1} {2} ({3}) - {4} (CPU {5}%)",
                RestOperationContext.Current.IncomingRequest.RemoteEndPoint,
                request.Method,
                request.Url,
                RestOperationContext.Current.IncomingRequest.UserAgent,
                httpCorrelator,
                usage);

            httpCorrelation = new KeyValuePair<Guid, DateTime>(httpCorrelator, DateTime.Now);
        }

        /// <summary>
        /// Apply the endpoint behavior
        /// </summary>
        public void ApplyEndpointBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            dispatcher.MessageInspectors.Add(this);
        }

        /// <summary>
        /// Before sending the response
        /// </summary>
        public void BeforeSendResponse(RestResponseMessage response)
        {
            var processingTime = DateTime.Now.Subtract(httpCorrelation.Value);

            float usage = 0.0f;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                using (PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true))
                    usage = cpuCounter.NextValue();
            }

            this.m_traceSource.TraceEvent(EventLevel.Verbose, "HTTP RSP {0} : {1} ({2} ms - CPU {3}%)",
                httpCorrelation.Key,
                response.StatusCode,
                processingTime.TotalMilliseconds,
                usage);
        }
    }
}
