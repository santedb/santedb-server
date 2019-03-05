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
 * Date: 2019-3-5
 */
using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Persistence.Diagnostics.OpenProject.Configuration
{
    /// <summary>
    /// Represents a configuration section for open project configuration
    /// </summary>
    [XmlType(nameof(OpenProjectConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class OpenProjectConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Gets or sets the API key
        /// </summary>
        [XmlElement("apiKey")]
        public String ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the API endpoint
        /// </summary>
        [XmlElement("apiEndpoint")]
        public String ApiEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the project on which to create the API objects
        /// </summary>
        [XmlElement("project")]
        public String ProjectKey { get; set; }

        /// <summary>
        /// Gets or sets the bug type to create
        /// </summary>
        [XmlElement("bugType")]
        public int BugType { get; set; }
    }
}
