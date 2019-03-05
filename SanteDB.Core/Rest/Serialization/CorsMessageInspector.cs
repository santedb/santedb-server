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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Core.Rest.Serialization
{
    /// <summary>
    /// CORS settings
    /// </summary>
    [XmlRoot(nameof(CorsEndpointBehaviorConfiguration), Namespace = "http://santedb.org/configuration")]
    [XmlType(nameof(CorsEndpointBehaviorConfiguration), Namespace = "http://santedb.org/configuration")]
    public class CorsEndpointBehaviorConfiguration
    {
        /// <summary>
        /// Resources
        /// </summary>
        public CorsEndpointBehaviorConfiguration()
        {
            this.Resource = new List<CorsResourceSetting>();
        }

        /// <summary>
        /// Gets the resource settings
        /// </summary>
        [XmlElement("resource")]
        public List<CorsResourceSetting> Resource { get; private set; }
    }

    /// <summary>
    /// Represents a setting for one resource
    /// </summary>
    [XmlType(nameof(CorsResourceSetting), Namespace = "http://santedb.org/configuration")]
    public class CorsResourceSetting
    {

        public CorsResourceSetting()
        {

        }

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
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets the domain
        /// </summary>
        [XmlAttribute("domain")]
        public String Domain { get; set; }

        /// <summary>
        /// Gets the verbs allowed
        /// </summary>
        [XmlArray("verbs"), XmlArrayItem("add")]
        public List<String> Verbs { get; set; }

        /// <summary>
        /// Gets the headers allowed
        /// </summary>
        [XmlArray("headers"), XmlArrayItem("add")]
        public List<String> Headers { get; set; }
    }

    /// <summary>
    /// Represents a message inspector that adds CORS headers
    /// </summary>
    public class CorsMessageInspector : IMessageInspector
    {

        // Settings for CORs
        private CorsEndpointBehaviorConfiguration m_settings;

        /// <summary>
        /// Create a new message inspector
        /// </summary>
        public CorsMessageInspector(CorsEndpointBehaviorConfiguration settings)
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
