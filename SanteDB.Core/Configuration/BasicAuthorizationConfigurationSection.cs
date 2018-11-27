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
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
{
    /// <summary>
    /// Basic authorization configuration
    /// </summary>
    [XmlType(nameof(BasicAuthorizationConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class BasicAuthorizationConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Require client authentication.
        /// </summary>
        [XmlAttribute("requireClientAuth")]
        public bool RequireClientAuth { get; set; }

        /// <summary>
        /// Allowed claims
        /// </summary>
        [XmlArray("claims"), XmlArrayItem("add")]
        public ObservableCollection<string> AllowedClientClaims { get; set; }

        /// <summary>
        /// Realm of basic auth
        /// </summary>
        [XmlAttribute("realm")]
        public string Realm { get; set; }
    }
}