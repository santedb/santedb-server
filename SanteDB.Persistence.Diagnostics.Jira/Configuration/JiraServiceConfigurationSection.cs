/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using Newtonsoft.Json;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Http;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace SanteDB.Persistence.Diagnostics.Jira.Configuration
{
    /// <summary>
    /// JIRA Service configuration
    /// </summary>
    [XmlType(nameof(JiraServiceConfigurationSection), Namespace = "http://santedb.org/configuration")]
    [ExcludeFromCodeCoverage]
    public class JiraServiceConfigurationSection : IConfigurationSection
    {
        /// <summary>
        /// Creates a new jira service configuration
        /// </summary>
        public JiraServiceConfigurationSection()
        {
        }

        /// <summary>
        /// Gets the API configuration
        /// </summary>
        [XmlElement("jiraEndpoint"), JsonProperty("jiraEndpoint")]
        public RestClientDescriptionConfiguration ApiConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the project
        /// </summary>
        [XmlAttribute("project"), ConfigurationRequired]
        public String Project { get; set; }

    }
}