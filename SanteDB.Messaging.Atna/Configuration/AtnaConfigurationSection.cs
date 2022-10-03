/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using AtnaApi.Transport;
using SanteDB.Core.Configuration;
using SanteDB.Core.Security.Configuration;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace SanteDB.Messaging.Atna.Configuration
{
    /// <summary>
    /// Classifies the audit transport
    /// </summary>
    [XmlType(nameof(AtnaTransportType), Namespace = "http://santedb.org/configuration")]
    public enum AtnaTransportType
    {
        /// <summary>
        /// Send audits to the central repository using UDP
        /// </summary>
        [XmlEnum("udp")]
        Udp,
        /// <summary>
        /// Send audits to the central repository using TCP
        /// </summary>
        [XmlEnum("tcp")]
        Tcp,
        /// <summary>
        /// Send audits to the central repository using TLS + TCP
        /// </summary>
        [XmlEnum("stcp")]
        Stcp,
        /// <summary>
        /// Send audits to the central repository via a file share
        /// </summary>
        [XmlEnum("file")]
        File
    }

    /// <summary>
    /// Audit configuration
    /// </summary>
    [XmlType(nameof(AtnaConfigurationSection), Namespace = "http://santedb.org/configuration")]
    [ExcludeFromCodeCoverage]
    public class AtnaConfigurationSection : IConfigurationSection
    {

        // Transport type
        private AtnaTransportType m_transportType;

        /// <summary>
        /// Identifies the host that audits should be sent to
        /// </summary>
        [XmlAttribute("endpoint"), ConfigurationRequired]
        [DisplayName("Endpoint"), Description("The endpoint in HOST:PORT format where the remote")]
        public String AuditTarget { get; set; }

        /// <summary>
        /// Gets or sets the publisher type
        /// </summary>
        [XmlAttribute("transport"), ConfigurationRequired]
        [DisplayName("Transport To Use"), Description("The transport to use to send audits (UDP or STCP are recommended)")]
        public AtnaTransportType Transport
        {
            get => this.m_transportType;
            set
            {
                this.m_transportType = value;
                if (this.m_transportType == AtnaTransportType.Stcp)
                {
                    this.ClientCertificate = this.ClientCertificate ?? new X509ConfigurationElement();
                    this.ServerCertificate = this.ServerCertificate ?? new X509ConfigurationElement();
                }
            }
        }

        /// <summary>
        /// Enterprise site ID
        /// </summary>
        [XmlAttribute("enterpriseSiteID"), ConfigurationRequired]
        [DisplayName("Enterprise Site"), Description("The enterprise site to affix to audits")]
        public string EnterpriseSiteId { get; set; }

        /// <summary>
        /// Gets or sets the certificate thumbprint
        /// </summary>
        [XmlElement("clientCertificate")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [DisplayName("Client Certificate"), Description("If using a secure connection (STCP) the certificate to use to authenticate this node to the audit repository")]
        public X509ConfigurationElement ClientCertificate { get; set; }

        /// <summary>
        /// Gets or sets the certificate thumbprint
        /// </summary>
        [XmlElement("serverCertificate")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [DisplayName("Server Certificate"), Description("If using a secure connection (STCP) the certificate this node expects the certificate to use")]
        public X509ConfigurationElement ServerCertificate { get; set; }

        /// <summary>
        /// Message format
        /// </summary>
        [XmlAttribute("format")]
        [DisplayName("Format"), Description("The format of the message either RFC-3881 or DICOM")]
        public MessageFormatType Format { get; set; }

    }
}
