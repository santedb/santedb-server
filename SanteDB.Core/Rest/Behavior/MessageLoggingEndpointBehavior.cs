using RestSrvr;
using RestSrvr.Message;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Rest.Behavior
{
    /// <summary>
    /// Represents an endpoint behavior that logs messages
    /// </summary>
    public class MessageLoggingEndpointBehavior : IEndpointBehavior, IMessageInspector
    {

        // Trace source name
        private TraceSource m_traceSource = new TraceSource(SanteDBConstants.WcfTraceSourceName);

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

            this.m_traceSource.TraceEvent(TraceEventType.Verbose, 0, "HTTP RQO {0} : {1} {2} ({3}) - {4} (CPU {5}%)",
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

            this.m_traceSource.TraceEvent(TraceEventType.Verbose, 0, "HTTP RSP {0} : {1} ({2} ms - CPU {3}%)",
                httpCorrelation.Key,
                response.StatusCode,
                processingTime.TotalMilliseconds,
                usage);
        }
    }
}
