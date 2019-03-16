using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using RestSrvr;
using RestSrvr.Attributes;
using RestSrvr.Exceptions;
using SanteDB.Core;
using SanteDB.Core.Http;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Interop;
using SanteDB.Messaging.Metadata.Composer;
using SanteDB.Messaging.Metadata.Model.Swagger;
using SanteDB.Rest.Common.Fault;

namespace SanteDB.Messaging.Metadata.Rest
{
    /// <summary>
    /// Represents the OpenApi Behavior
    /// </summary>
    [ServiceBehavior(Name = "META")]
    public class MetadataServiceBehavior : IMetadataServiceContract
    {

        /// <summary>
        /// Gets the trace source
        /// </summary>
        private TraceSource m_traceSource = new TraceSource(MetadataConstants.TraceSourceName);

        /// <summary>
        /// Get the swagger documentation
        /// </summary>
        public object GetMetadata(String serviceName, String composer)
        {
            try
            {
                // Get the services and see if we have a document already
                return MetadataComposerUtil.GetComposer(composer).ComposeDocumentation(serviceName);
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "Could not get documentation due to exception: {0}", e);
                throw e;
            }
        }

        /// <summary>
        /// Get the YAML documentation
        /// </summary>
        public Stream GetOpenApiDefinitions()
        {
            RestOperationContext.Current.OutgoingResponse.ContentType = "application/json";
            IEnumerable<ServiceEndpointOptions> services = ApplicationServiceContext.Current.GetService<MetadataMessageHandler>().Configuration.Services;
            if (services.Count() == 0)
                services = ApplicationServiceContext.Current.GetService<IServiceManager>().GetServices().OfType<IApiEndpointProvider>().Select(o => new ServiceEndpointOptions(o));

            // Output YAML
            var sw = new StringBuilder();
            sw.Append("{ urls: [");
            foreach (var api in services.Where(o=>o.Behavior != null))
            {
                var serviceName = typeof(ServiceEndpointType).GetField(api.ServiceType.ToString()).GetCustomAttribute<XmlEnumAttribute>()?.Name ?? api.ServiceType.ToString();
                sw.AppendFormat("{{ \"url\": \"{0}\", ", $"./{serviceName}/swagger.json");
                sw.AppendFormat("\"name\": \"{0}\" }} ,", MetadataComposerUtil.GetElementDocumentation(api.Behavior.Type) ?? api.ServiceType.ToString());
            }
            sw.Remove(sw.Length - 2, 2);
            sw.Append("] }");
            return new MemoryStream(Encoding.UTF8.GetBytes(sw.ToString()));

        }

        /// <summary>
        /// Get the specified embedded resource
        /// </summary>
        public Stream Index(String content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    RestOperationContext.Current.OutgoingResponse.Redirect("./index.html");
                    return null;
                }

                string filename = content.Contains("?")
                    ? content.Substring(0, content.IndexOf("?", StringComparison.Ordinal))
                    : content;


                var contentPath = $"SanteDB.Messaging.Metadata.Docs.{filename.Replace("/", ".")}";

                if (!typeof(MetadataServiceBehavior).Assembly.GetManifestResourceNames().Contains(contentPath))
                {
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 404;
                    return null;
                }
                else
                {

                    RestOperationContext.Current.OutgoingResponse.StatusCode = 200; /// HttpStatusCode.OK;
                    //RestOperationContext.Current.OutgoingResponse.ContentLength = new FileInfo(contentPath).Length;
                    RestOperationContext.Current.OutgoingResponse.ContentType = DefaultContentTypeMapper.GetContentType(contentPath);
                    return typeof(MetadataServiceBehavior).Assembly.GetManifestResourceStream(contentPath);
                }
            }
            catch (Exception e)
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = 500;

                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                return null;
            }
        }
    }
}
