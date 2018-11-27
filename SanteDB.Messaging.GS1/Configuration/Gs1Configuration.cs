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
using SanteDB.Core.Configuration;
using System;
using System.Configuration;
using System.Xml.Serialization;

namespace SanteDB.Messaging.GS1.Configuration
{
    /// <summary>
    /// GS1 configuration 
    /// </summary>
    [XmlType(nameof(Gs1ConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class Gs1ConfigurationSection  : IConfigurationSection
    {
        /// <summary>
        /// Auto create materials
        /// </summary>
        [XmlAttribute("autoCreateMaterials")]
        public bool AutoCreateMaterials {
            get;set;
        }

        /// <summary>
        /// Default content owner assigning authority
        /// </summary>
        [XmlAttribute("defaultAuthority")]
        public String DefaultContentOwnerAssigningAuthority {
            get;set;
        }

        /// <summary>
        /// Gets the queue on which to place messages
        /// </summary>
        [XmlAttribute("queueName"), ConfigurationRequired]
        public String Gs1QueueName {
            get;set;
        }

        /// <summary>
        /// Gets or sets the gs1 broker address
        /// </summary>
        [XmlElement("broker"), ConfigurationRequired]
        public As2ServiceElement Gs1BrokerAddress {
            get;set;
        }

        /// <summary>
        /// Gets or set sthe sender information
        /// </summary>
        [XmlAttribute("partnerAuthority"), ConfigurationRequired]
        public string PartnerIdentificationAuthority { get; set; }


        /// <summary>
        /// Gets or sets the partner identification
        /// </summary>
        [XmlAttribute("partnerIdentification"), ConfigurationRequired]
        public string PartnerIdentification { get; set; }

        /// <summary>
        /// Identifies the sender contact email
        /// </summary>
        [XmlElement("senderContactEmail")]
        public string SenderContactEmail { get; set; }
    }
}
