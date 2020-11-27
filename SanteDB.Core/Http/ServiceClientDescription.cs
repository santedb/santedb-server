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
using SanteDB.Core.Http.Description;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Core.Http
{
    /// <summary>
    /// Represents a rest client description
    /// </summary>
    [XmlType(nameof(ServiceClientDescription), Namespace = "http://santedb.org/configuration")]
    public class ServiceClientDescription : IRestClientDescription
    {

        /// <summary>
        /// Gets or sets the service binding
        /// </summary>
        [XmlElement("binding")]
        public ServiceClientBindingDescription Binding {
            get; set;
        }

        /// <summary>
        /// Gets or sets the binding description
        /// </summary>
        [XmlIgnore]
        IRestClientBindingDescription IRestClientDescription.Binding
        {
            get
            {
                return this.Binding;
            }
        }

        /// <summary>
        /// Gets whether a tracing is enabled.
        /// </summary>
        [XmlElement("enableTracing")]
        public bool Trace { get; set; }

        /// <summary>
        /// Gets or sets the endpoints
        /// </summary>
        [XmlIgnore]
        public List<IRestClientEndpointDescription> Endpoint
        {
            get
            {
                return this.EndpointCollection.OfType<IRestClientEndpointDescription>().ToList();
            }
        }

        /// <summary>
        /// Endpoint collection for configuration
        /// </summary>
        [XmlArray("endpoint"), XmlArrayItem("add")]
        public List<ServiceClientEndpointDescription> EndpointCollection { get;set;}
    }



    /// <summary>
    /// Represents a service client description
    /// </summary>
    [XmlType(nameof(ServiceClientEndpointDescription), Namespace = "http://santedb.org/configuration")]
    public class ServiceClientEndpointDescription : IRestClientEndpointDescription
    {

        /// <summary>
        /// Default ctor
        /// </summary>
        public ServiceClientEndpointDescription()
        {

        }

        /// <summary>
        /// Service client endpoint description
        /// </summary>
        public ServiceClientEndpointDescription(String address)
        {
            this.Address = address;
            this.Timeout = 10000;
        }

        /// <summary>
        /// Gets or sets the address
        /// </summary>
        [XmlAttribute("address")]
        public string Address {
            get;set;
        }

        /// <summary>
        /// Gets or sets the timeout
        /// </summary>
        [XmlAttribute("timeout")]
        public int Timeout
        {
            get;set;
        }
    }

    /// <summary>
    /// REST client binding description
    /// </summary>
    [XmlType(nameof(ServiceClientBindingDescription), Namespace = "http://santedb.org/configuration")]
    public class ServiceClientBindingDescription :  IRestClientBindingDescription
    {
        /// <summary>
        /// Gets the content type mapper
        /// </summary>
        [XmlIgnore]
        public IContentTypeMapper ContentTypeMapper { get { return new DefaultContentTypeMapper(); } }

        /// <summary>
        /// Gets or sets the optimization flag
        /// </summary>
        [XmlAttribute("optimize")]
        public bool Optimize {
            get;set;
        }

        /// <summary>
        /// Gets or sets the security description
        /// </summary>
        [XmlIgnore]
        public IRestClientSecurityDescription Security { get; set; }
    }
}
