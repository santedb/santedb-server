/*
 * Copyright 2010-2018 Mohawk College of Applied Arts and Technology
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
 * Date: 1-9-2017
 */

using AtnaApi.Transport;
using SanteDB.Core.Configuration;
using System;
using System.Net;
using System.Xml.Serialization;

namespace SanteDB.Messaging.Atna.Configuration
{
    /// <summary>
    /// Audit configuration
    /// </summary>
    [XmlType(nameof(AtnaConfigurationSection), Namespace = "http://santedb.org/configuration/atna")]
    public class AtnaConfigurationSection : IConfigurationSection
    {
        
        /// <summary>
        /// Identifies the host that audits should be sent to
        /// </summary>
        [XmlAttribute("endpoint"), ConfigurationRequired]
        public String AuditTarget { get; set; }

        /// <summary>
        /// Gets or sets the message publisher to use for this audit
        /// </summary>
        [XmlIgnore]
        public Type MessagePublisher { get; set; }

        /// <summary>
        /// Gets or sets the publisher type
        /// </summary>
        [XmlAttribute("publisher"), ConfigurationRequired]
        public string PublisherType {
            get => this.MessagePublisher?.GetType().AssemblyQualifiedName;
            set => this.MessagePublisher = Type.GetType(value);
        }

        /// <summary>
        /// Enterprise site ID
        /// </summary>
        [XmlAttribute("EnterpriseSiteID"), ConfigurationRequired]
        public string EnterpriseSiteId { get; set; }

        /// <summary>
        /// Gets or sets the certificate thumbprint
        /// </summary>
        [XmlElement("certificateThumbprint")]
        public string CertificateThumprint { get; set; }

        /// <summary>
        /// Message format
        /// </summary>
        [XmlAttribute("format")]
        public MessageFormatType Format { get; set; }

    }
}
