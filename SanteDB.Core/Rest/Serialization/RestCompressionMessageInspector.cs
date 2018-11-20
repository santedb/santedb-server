using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core.Rest.Compression;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SanteDB.Core.Rest.Serialization
{
    /// <summary>
    /// Represents an HDSI message inspector which can inspect messages and perform tertiary functions
    /// not included in WCF (such as compression)
    /// </summary>
    public class RestCompressionMessageInspector : IMessageInspector
    {
        // Trace source
        private TraceSource m_traceSource = new TraceSource(SanteDBConstants.WcfTraceSourceName);
        
        /// <summary>
        /// After request is received
        /// </summary>
        public void AfterReceiveRequest(RestRequestMessage request)
        {
            try
            {

                // Handle compressed requests
                var compressionScheme = CompressionUtil.GetCompressionScheme(RestOperationContext.Current.IncomingRequest.Headers["Content-Encoding"]);
                if (compressionScheme != null)
                    request.Body = compressionScheme.CreateDecompressionStream(request.Body);
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
            }
        }
        
        /// <summary>
        /// Before sending the response
        /// </summary>
        public void BeforeSendResponse(RestResponseMessage response)
        {
            try
            {

                string encodings = RestOperationContext.Current.IncomingRequest.Headers.Get("Accept-Encoding");
                string compressionScheme = String.Empty;

                if (!string.IsNullOrEmpty(encodings))
                {
                    encodings = encodings.ToLowerInvariant();

                    if (encodings.Contains("lzma"))
                        compressionScheme = "lzma";
                    else if (encodings.Contains("bzip2"))
                        compressionScheme = "bzip2";
                    else if (encodings.Contains("gzip"))
                        compressionScheme = "gzip";
                    else if (encodings.Contains("deflate"))
                        compressionScheme = "deflate";
                    else
                        response.Headers.Add("X-CompressResponseStream", "no-known-accept");
                }
                
                // No reply = no compress :)
                if (response.Body == null)
                    return;

                // Finally compress
                // Compress
                if (!String.IsNullOrEmpty(compressionScheme))
                {
                    try
                    {
                        response.Headers.Add("Content-Encoding", compressionScheme);
                        response.Headers.Add("X-CompressResponseStream", compressionScheme);

                        // Read binary contents of the message
                        var memoryStream = new MemoryStream();
                        using (var compressor = CompressionUtil.GetCompressionScheme(compressionScheme).CreateCompressionStream(memoryStream))
                            response.Body.CopyTo(compressor);
                        response.Body.Dispose();
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        response.Body = memoryStream;

                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
            }
        }
    }
}
