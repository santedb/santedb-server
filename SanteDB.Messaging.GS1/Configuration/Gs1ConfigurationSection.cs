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
 * Date: 2020-1-8
 */
using SanteDB.Core.Configuration;
using System;
using System.ComponentModel;
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
        /// Creates a new GS1 configuration
        /// </summary>
        public Gs1ConfigurationSection()
        {
            this.Gs1BrokerAddress = new As2ServiceElement();
        }

        /// <summary>
        /// Auto create materials
        /// </summary>
        [XmlAttribute("autoCreateMaterials")]
        [DisplayName("Auto Create Materials"), Description("When a GS1 BMS message is received with a material unknown to SanteDB, automatically register the material type in the database")]
        public bool AutoCreateMaterials {
            get;set;
        }

        /// <summary>
        /// Default content owner assigning authority
        /// </summary>
        [XmlAttribute("defaultAuthority")]
        [DisplayName("Content Owner AA"), Description("The assinging authority to append to owner information when not provided in the GS1 message")]
        public String DefaultContentOwnerAssigningAuthority {
            get;set;
        }

        /// <summary>
        /// Gets the queue on which to place messages
        /// </summary>
        [XmlAttribute("queueName"), ConfigurationRequired]
        [DisplayName("Queue Name"), Description("The name of the queue from the queue service which stores GS1 requests until they can be successfully sent")]
        public String Gs1QueueName {
            get;set;
        }

        /// <summary>
        /// Gets or sets the gs1 broker address
        /// </summary>
        [XmlElement("broker"), ConfigurationRequired]
        [TypeConverter(typeof(ExpandableObjectConverter)), DisplayName("GS1 Broker"), Description("Configuration for the broker to use for GS1 messages")]
        public As2ServiceElement Gs1BrokerAddress {
            get;set;
        }

        /// <summary>
        /// Gets or set sthe sender information
        /// </summary>
        [XmlAttribute("partnerAuthority"), ConfigurationRequired]
        [DisplayName("Partner AA"), Description("The default assigning authority to use for trading partner identification")]
        public string PartnerIdentificationAuthority { get; set; }


        /// <summary>
        /// Gets or sets the partner identification
        /// </summary>
        [XmlAttribute("partnerIdentification"), ConfigurationRequired]
        [DisplayName("Partner ID"), Description("The name of this trading partner")]
        public string PartnerIdentification { get; set; }

        /// <summary>
        /// Identifies the sender contact email
        /// </summary>
        [XmlElement("senderContactEmail")]
        [DisplayName("Contact Email"), Description("The e-mail address this service should affix to all outbound GS1 messages")]
        public string SenderContactEmail { get; set; }
    }
}
