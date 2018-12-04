using Newtonsoft.Json;
using SanteDB.Core.Configuration;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Messaging.HL7.TransportProtocol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.HL7.Configuration
{
    /// <summary>
    /// Represents the HL7 configuration
    /// </summary>
    [XmlType(nameof(Hl7ConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class Hl7ConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Represents the local domain
        /// </summary>
        [XmlElement("localAuthority"), JsonProperty("localAuthority")]
        public AssigningAuthority LocalAuthority { get; set; }


        /// <summary>
        /// Security method
        /// </summary>
        [XmlAttribute("security"), JsonProperty("security")]
        public SecurityMethod Security { get; set; }


        /// <summary>
        /// The address to which to bind
        /// </summary>
        /// <remarks>A full Uri is required and must be tcp:// or mllp://</remarks>
        [XmlArray("services"), XmlArrayItem("add"), JsonProperty("services")]
        public List<ServiceDefinition> Services { get; set; }

        /// <summary>
        /// Gets or sets the facilit
        /// </summary>
        [XmlElement("facility"), JsonProperty("facility")]
        public Guid LocalFacility { get; set; }
    }

    /// <summary>
	/// Handler definition
	/// </summary>
    [XmlType(nameof(HandlerDefinition), Namespace = "http://santedb.org/configuration")]
    public class HandlerDefinition
    {
        /// <summary>
        /// The handler
        /// </summary>
        private IHL7MessageHandler m_handler;

        /// <summary>
        /// Handler defn ctor
        /// </summary>
        public HandlerDefinition()
        {
            this.Types = new List<MessageDefinition>();
        }

        /// <summary>
        /// Gets or sets the handler
        /// </summary>
        [XmlIgnore, JsonIgnore]
        public IHL7MessageHandler Handler {
            get
            {
                if (this.m_handler == null)
                {
                    var hdt = Type.GetType(this.HandlerType);
                    if (hdt == null)
                        throw new InvalidOperationException($"{this.Handler} can't be found");
                    this.m_handler = Activator.CreateInstance(hdt) as IHL7MessageHandler;
                }
                return this.m_handler;
            }
            set
            {
                this.HandlerType = value?.GetType().AssemblyQualifiedName;
            }
        }

        /// <summary>
        /// Type name of the handler
        /// </summary>
        [XmlAttribute("type"), JsonProperty("type")]
        public string HandlerType { get; set; }

        /// <summary>
        /// Message types that trigger this (MSH-9)
        /// </summary>
        [XmlElement("message"), JsonProperty("message")]
        public List<MessageDefinition> Types { get; set; }

        /// <summary>
        /// Get the string representation
        /// </summary>
        public override string ToString()
        {
            var descAtts = this.Handler.GetType().GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (descAtts.Length > 0)
                return (descAtts[0] as DescriptionAttribute).Description;
            return this.Handler.GetType().Name;
        }
    }

    /// <summary>
    /// Security methods
    /// </summary>
    [XmlType(nameof(SecurityMethod), Namespace = "http://santedb.org/configuration")]
    public enum SecurityMethod
    {
        /// <summary>
        /// No security
        /// </summary>
        None,
        /// <summary>
        /// Use MSH-8 for authentication
        /// </summary>
        Msh8,
        /// <summary>
        /// Use SFT-4 for authentication
        /// </summary>
        Sft4
    }

    /// <summary>
    /// Message definition
    /// </summary>
    [XmlType(nameof(MessageDefinition), Namespace = "http://santedb.org/configuration")]
    public class MessageDefinition
    {
        /// <summary>
        /// Gets or sets a value identifying whether this is a query
        /// </summary>
        [XmlAttribute("isQuery"), JsonProperty("isQuery")]
        public bool IsQuery { get; set; }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [XmlAttribute("name"), JsonProperty("name")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Service definition
    /// </summary>
    [XmlType(nameof(ServiceDefinition), Namespace = "http://santedb.org/configuration")]
    public class ServiceDefinition
    {
        /// <summary>
        /// Service defn ctor
        /// </summary>
        public ServiceDefinition()
        {
            this.Handlers = new List<HandlerDefinition>();
        }

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
        /// Gets or sets the handlers
        /// </summary>
        [XmlArray("handler"), XmlArrayItem("add"), JsonProperty("handlers")]
        public List<HandlerDefinition> Handlers { get; set; }

        /// <summary>
        /// Gets or sets the name of the defintiion
        /// </summary>
        [XmlAttribute("name"), JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the timeout
        /// </summary>
        [XmlAttribute("receiveTimeout"), JsonProperty("receiveTimeout")]
        public int ReceiveTimeout { get; set; }
    }

}
