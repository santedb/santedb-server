/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2021-8-27
 */

using SanteDB.Core.Configuration;
using SanteDB.Core.Http;
using SanteDB.Core.Http.Description;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Persistence.Diagnostics.Jira.Configuration
{
    /// <summary>
    /// JIRA Service configuration
    /// </summary>
    [XmlType(nameof(JiraServiceConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class JiraServiceConfigurationSection : IConfigurationSection, IRestClientDescription
    {
        /// <summary>
        /// Creates a new jira service configuration
        /// </summary>
        public JiraServiceConfigurationSection()
        {
        }

        /// <summary>
        /// Accept
        /// </summary>
        public string Accept => "application/json";

        /// <summary>
        /// Gets or sets the username
        /// </summary>
        [XmlAttribute("userName"), ConfigurationRequired]
        public String UserName { get; set; }

        /// <summary>
        /// Gets or sets the password
        /// </summary>
        [XmlAttribute("password"), ConfigurationRequired]
        public String Password { get; set; }

        /// <summary>
        /// Gets or sets the project
        /// </summary>
        [XmlAttribute("project"), ConfigurationRequired]
        public String Project { get; set; }

        /// <summary>
        /// Gets whether a tracing is enabled.
        /// </summary>
        [XmlAttribute("trace")]
        public bool Trace { get; }

        /// <summary>
        /// Gets or sets the endpoint information
        /// </summary>
        [XmlAttribute("url"), ConfigurationRequired]
        public String Endpoint { get; set; }

        /// <summary>
        /// Get the endpoint
        /// </summary>
        [XmlIgnore]
        List<IRestClientEndpointDescription> IRestClientDescription.Endpoint => new List<IRestClientEndpointDescription>()
        {
            new ServiceClientEndpointDescription(this.Endpoint)
        };

        /// <summary>
        /// Rest binding
        /// </summary>
        [XmlIgnore]
        IRestClientBindingDescription IRestClientDescription.Binding => new JiraRestClientBindingDescription();
    }
}