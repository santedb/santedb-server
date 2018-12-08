using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Messaging.HL7.Configuration
{
    /// <summary>
    /// HL7 Notifications Configuration Element
    /// </summary>
    [XmlType(nameof(Hl7InterceptorConfigurationElement), Namespace = "http://santedb.org/configuration")]
    public class Hl7InterceptorConfigurationElement
    {

        /// <summary>
        /// Gets the XML type name of the notification
        /// </summary>
        [XmlAttribute("type"), JsonProperty("type")]
        public string InterceptorClassXml { get; set; }

        /// <summary>
        /// Gets or sets the notifier
        /// </summary>
        [XmlIgnore, JsonIgnore]
        public Type InterceptorClass { get => Type.GetType(this.InterceptorClassXml); set => this.InterceptorClassXml = value?.GetType().AssemblyQualifiedName; }

        /// <summary>
        /// Guards to filter the incoming data
        /// </summary>
        [XmlArray("guards"), XmlArrayItem("add"), JsonProperty("guards")]
        public List<String> Guards { get; set; }

        /// <summary>
        /// Represents endpoints
        /// </summary>
        [XmlArray("endpoints"), XmlArrayItem("add"), JsonProperty("endpoints")]
        public List<Hl7RemoteEndpointConfiguration> Endpoints { get; set; }

        /// <summary>
        /// Gets or sets the identity domains to notify the remote target of
        /// </summary>
        [XmlArray("domains"), XmlArrayItem("add"), JsonProperty("domains")]
        public List<String> ExportDomains { get; set; }

        /// <summary>
        /// Sets the version
        /// </summary>
        [XmlAttribute("hl7version"), JsonProperty("hl7version")]
        public string Version { get; set; }
    }
}