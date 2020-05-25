/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
 * Date: 2019-11-27
 */
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
        /// If no security method is being used, the principal of the anonymous user
        /// </summary>
        [XmlAttribute("anonUser"), JsonProperty("anonUser")]
        public String AnonymousUser { get; set; }

        /// <summary>
        /// The address to which to bind
        /// </summary>
        /// <remarks>A full Uri is required and must be tcp:// or mllp://</remarks>
        [XmlArray("services"), XmlArrayItem("add"), JsonProperty("services")]
        public List<Hl7ServiceDefinition> Services { get; set; }

        /// <summary>
        /// Gets or sets the facilit
        /// </summary>
        [XmlElement("facility"), JsonProperty("facility")]
        public Guid LocalFacility { get; set; }

        /// <summary>
        /// Gets or sets the notifications
        /// </summary>
        [XmlArray("interceptors"), XmlArrayItem("add"), JsonProperty("interceptors")]
        public List<Hl7InterceptorConfigurationElement> Interceptors { get; set; }

        /// <summary>
        /// Gets or sets the authority for SSN
        /// </summary>
        [XmlElement("ssnAuthority"), JsonProperty("ssnAuthority")]
        public AssigningAuthority SsnAuthority { get; set; }

        /// <summary>
        /// Birthplace class keys
        /// </summary>
        [XmlArray("birthplaceClasses"), XmlArrayItem("add"), JsonProperty("birthplaceClasses")]
        public List<Guid> BirthplaceClassKeys { get; set; }
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
    [XmlType(nameof(Hl7ServiceDefinition), Namespace = "http://santedb.org/configuration")]
    public class Hl7ServiceDefinition : Hl7EndpointConfiguration
    {
        /// <summary>
        /// Service defn ctor
        /// </summary>
        public Hl7ServiceDefinition()
        {
            this.Handlers = new List<HandlerDefinition>();
        }


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

    }

}
