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
using Newtonsoft.Json;
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
        [XmlAttribute("restEndpoint"), JsonProperty("restEndpoint")]
        public string WcfEndpoint { get; set; }

        /// <summary>
        /// The landing page file
        /// </summary>
        [XmlAttribute("index"), JsonProperty("index")]
        public string LandingPage { get; set; }

        /// <summary>
        /// XML for resource handlers
        /// </summary>
        [XmlArray("resourceHandlers"), XmlArrayItem("add"), JsonProperty("resourceHandlers")]
        public List<TypeReferenceConfiguration> ResourceHandlers
        {
            get;set;
        }

        /// <summary>
        /// When set, describes the base uri for all resources on this FHIR service.
        /// </summary>
        [XmlElement("base"), JsonProperty("base")]
        public String ResourceBaseUri { get; set; }
    }
    
}
