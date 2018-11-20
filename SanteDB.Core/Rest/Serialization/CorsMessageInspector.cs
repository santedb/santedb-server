using RestSrvr;
using RestSrvr.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Rest.Serialization
{
    /// <summary>
    /// CORS settings
    /// </summary>
    public class CorsSettings
    {
        /// <summary>
        /// Resources
        /// </summary>
        public CorsSettings()
        {
            this.Resource = new List<CorsResourceSetting>();
        }

        /// <summary>
        /// Gets the resource settings
        /// </summary>
        public List<CorsResourceSetting> Resource { get; private set; }
    }

    /// <summary>
    /// Represents a setting for one resource
    /// </summary>
    public class CorsResourceSetting
    {

        /// <summary>
        /// Creates a new resource setting
        /// </summary>
        /// <param name="name">The name of the resource or *</param>
        /// <param name="domain">The domain or *</param>
        public CorsResourceSetting(String name, String domain, IEnumerable<String> verbs, IEnumerable<String> headers)
        {
            this.Name = name;
            this.Domain = domain;
            this.Verbs = new List<string>(verbs);
            this.Headers = new List<string>(headers);
        }

        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the domain
        /// </summary>
        public String Domain { get; private set; }

        /// <summary>
        /// Gets the verbs allowed
        /// </summary>
        public List<String> Verbs { get; private set; }

        /// <summary>
        /// Gets the headers allowed
        /// </summary>
        public List<String> Headers { get; private set; }
    }

    /// <summary>
    /// Represents a message inspector that adds CORS headers
    /// </summary>
    public class CorsMessageInspector : IMessageInspector
    {

        // Settings for CORs
        private CorsSettings m_settings;

        /// <summary>
        /// Create a new message inspector
        /// </summary>
        public CorsMessageInspector(CorsSettings settings)
        {
            this.m_settings = settings;
        }

        /// <summary>
        /// After received request
        /// </summary>
        public void AfterReceiveRequest(RestRequestMessage request)
        {
        }

        /// <summary>
        /// Before send response
        /// </summary>
        public void BeforeSendResponse(RestResponseMessage response)
        {
            // TODO: Add a configuration option to disable this
            var resourcePath = RestOperationContext.Current.IncomingRequest.Url.AbsolutePath.Substring(RestOperationContext.Current.ServiceEndpoint.Description.ListenUri.AbsolutePath.Length);
            var settings = this.m_settings.Resource.FirstOrDefault(o => o.Name == "*" || o.Name == resourcePath);
            if (settings != null)
            {
                Dictionary<String, String> requiredHeaders = new Dictionary<string, string>() {
                    {"Access-Control-Allow-Origin", settings.Domain},
                    {"Access-Control-Allow-Methods", String.Join(",", settings.Verbs)},
                    {"Access-Control-Allow-Headers", String.Join(",", settings.Headers)}
                };
                foreach (var kv in requiredHeaders)
                    if (!RestOperationContext.Current.OutgoingResponse.Headers.AllKeys.Contains(kv.Key))
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(kv.Key, kv.Value);
            }
        }
    }
}
