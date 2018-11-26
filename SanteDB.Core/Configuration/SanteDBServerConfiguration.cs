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
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
{
    /// <summary>
    /// SanteDB server configuration 
    /// </summary>
    [XmlType(nameof(SanteDBServerConfiguration), Namespace = "http://santedb.org/configuration/server")]
    public class SanteDBServerConfiguration : IConfigurationSection
    {
        /// <summary>
        /// Create new santedb configuration object
        /// </summary>
        public SanteDBServerConfiguration()
        {
        }

        /// <summary>
        /// Thread pool size
        /// </summary>
        [XmlAttribute("threadPoolSize")]
        public int ThreadPoolSize { get; set; }
    
        /// <summary>
        /// Service providers
        /// </summary>
        [XmlIgnore]
        public List<Type> ServiceProviders { get; internal set; }
    }
}
