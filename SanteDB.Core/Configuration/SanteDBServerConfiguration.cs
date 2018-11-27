/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: justin
 * Date: 2018-6-22
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
{
    /// <summary>
    /// SanteDB server configuration 
    /// </summary>
    [XmlType(nameof(SanteDBServerConfiguration), Namespace = "http://santedb.org/configuration")]
    public class SanteDBServerConfiguration : IConfigurationSection
    {

        // Service providers
        private IEnumerable<Type> m_serviceProviders;

        /// <summary>
        /// Create new santedb configuration object
        /// </summary>
        public SanteDBServerConfiguration()
        {
            this.ServiceProviders = new List<TypeReferenceConfiguration>();
        }

        /// <summary>
        /// Thread pool size
        /// </summary>
        [XmlAttribute("threadPoolSize")]
        public int ThreadPoolSize { get; set; }

        /// <summary>
        /// Gets the service providers from XML
        /// </summary>
        [XmlArray("serviceProviders"), XmlArrayItem("add")]
        public List<TypeReferenceConfiguration> ServiceProviders { get; set; }

    }
}
