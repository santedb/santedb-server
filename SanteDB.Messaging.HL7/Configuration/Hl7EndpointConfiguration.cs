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
 * Date: 2019-1-22
 */
using Newtonsoft.Json;
using SanteDB.Messaging.HL7.Client;
using SanteDB.Messaging.HL7.TransportProtocol;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Messaging.HL7.Configuration
{
    /// <summary>
    /// Represents HL7 endpoint configuration data
    /// </summary>
    [XmlType(nameof(Hl7EndpointConfiguration), Namespace = "http://santedb.org/configuration")]
    public class Hl7EndpointConfiguration
    {

        /// <summary>
        /// Gets or sets the address of the service
        /// </summary>
        [XmlAttribute("address"), JsonProperty("address")]
        public String AddressXml { get; set; }

        /// <summary>
        /// Gets the listening address
        /// </summary>
        [XmlIgnore, JsonIgnore]
        public Uri Address => new Uri(this.AddressXml);

        /// <summary>
        /// Attributes
        /// </summary>
        [XmlElement("sllp", Type = typeof(SllpTransport.SllpConfigurationObject)), JsonProperty("sllpConfiguration")]
        public object Configuration { get; set; }

        /// <summary>
        /// Gets or sets the timeout
        /// </summary>
        [XmlAttribute("receiveTimeout"), JsonProperty("receiveTimeout")]
        public int ReceiveTimeout { get; set; }

        
    }

    /// <summary>
    /// Represents a remote endpoint
    /// </summary>
    [XmlType(nameof(Hl7RemoteEndpointConfiguration), Namespace = "http://santedb.org/configuration")]
    public class Hl7RemoteEndpointConfiguration : Hl7EndpointConfiguration
    {

        // Sender
        private MllpMessageSender m_sender;

        /// <summary>
        /// Gets the security token
        /// </summary>
        [XmlAttribute("securityToken"), JsonProperty("securityToken")]
        public String SecurityToken { get; set; }

        /// <summary>
        /// Gets the receiving facility
        /// </summary>
        [XmlAttribute("recievingFacility"), JsonProperty("recievingFacility")]
        public String ReceivingFacility { get; set; }

        /// <summary>
        /// Gets the receiving facility
        /// </summary>
        [XmlAttribute("recievingDevice"), JsonProperty("recievingDevice")]
        public String ReceivingDevice { get; set; }

        /// <summary>
        /// Get the message sender
        /// </summary>
        /// <returns></returns>
        public MllpMessageSender GetSender()
        {
            if(this.m_sender == null)
                this.m_sender = new MllpMessageSender(this.Address, (this.Configuration as SllpTransport.SllpConfigurationObject)?.ClientCaCertificate?.GetCertificate(), (this.Configuration as SllpTransport.SllpConfigurationObject)?.ServerCertificate?.GetCertificate());
            return this.m_sender;
        }

    }
}