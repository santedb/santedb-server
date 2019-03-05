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
using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Configuration
{
    /// <summary>
    /// FHIR service configuration
    /// </summary>
    [XmlType(nameof(FhirServiceConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class FhirServiceConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Creates a new instance of the WcfEndpoint
        /// </summary>
        public FhirServiceConfigurationSection()
        {
        }

        /// <summary>
        /// Gets the WCF endpoint name that the FHIR service listens on
        /// </summary>
        [XmlAttribute("restEndpoint")]
        public string WcfEndpoint { get; set; }

        /// <summary>
        /// The landing page file
        /// </summary>
        [XmlAttribute("index")]
        public string LandingPage { get; set; }

        /// <summary>
        /// XML for resource handlers
        /// </summary>
        [XmlArray("resourceHandlers"), XmlArrayItem("add")]
        public List<TypeReferenceConfiguration> ResourceHandlers
        {
            get;set;
        }

        /// <summary>
        /// When set, describes the base uri for all resources on this FHIR service.
        /// </summary>
        [XmlElement("base")]
        public String ResourceBaseUri { get; set; }
    }

    /// <summary>
    /// FHIR CORS configuration
    /// </summary>
    [XmlType(nameof(FhirCorsConfiguration), Namespace = "http://santedb.org/configuration")]
    public class FhirCorsConfiguration
    {

        /// <summary>
        /// Gets or sets the domain from which CORS is allowed
        /// </summary>
        [XmlAttribute("domain")]
        public String Domain { get; set; }

        /// <summary>
        /// Gets or sets the rsource
        /// </summary>
        [XmlAttribute("resource")]
        public String Resource { get; set; }

        /// <summary>
        /// Gets or sets the allowed operations
        /// </summary>
        [XmlArray("actions"), XmlArrayItem("add")]
        public List<String> Actions { get; set; }

        /// <summary>
        /// Gets or sets the allowed headers
        /// </summary>
        [XmlArray("headers"), XmlArrayItem("add")]
        public List<String> Headers { get; set; }
    }
}
