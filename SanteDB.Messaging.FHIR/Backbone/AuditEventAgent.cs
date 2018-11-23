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
 * Date: 2018-11-23
 */
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{

    /// <summary>
    /// Represents ttype of network address 
    /// </summary>
    [XmlType("AuditEventAgentNetworkType", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/network-type")]
    public enum AuditEventAgentNetworkType
    {
        [XmlEnum("1")]
        MachineName,
        [XmlEnum("2")]
        IpAddress,
        [XmlEnum("3")]
        TelephoneNumber,
        [XmlEnum("4")]
        EmailAddress,
        [XmlEnum("5")]
        Uri
    }
    /// <summary>
    /// Network information related to the audit agent
    /// </summary>
    [XmlType("AuditAgentNetwork", Namespace = "http://hl7.org/fhir")]
    public class AuditAgentNetwork : BackboneElement
    {
        /// <summary>
        /// Gets or sets the network address 
        /// </summary>
        [XmlElement("address")]
        [Description("The address or host name on the network")]
        public FhirString Address { get; set; }

        /// <summary>
        /// Gets or sets the address type
        /// </summary>
        [XmlElement("type")]
        [Description("Identifies the address type")]
        public FhirCode<AuditEventAgentNetworkType> AddressType { get; set; }

    }

    /// <summary>
    /// Represents an audit event agent (actor in the audit)
    /// </summary>
    [XmlType("AuditEventAgent", Namespace = "http://hl7.org/fhir")]
    public class AuditEventAgent : BackboneElement
    {
        /// <summary>
        /// Creates a new instance of the audit event agent
        /// </summary>
        public AuditEventAgent()
        {
            this.Role = new List<FhirCodeableConcept>();
            this.Policy = new List<FhirUri>();
        }

        /// <summary>
        /// Gets or sets the role that the user played
        /// </summary>
        [XmlElement("role")]
        [Description("Identifies the roles that an actor plays")]
        public List<FhirCodeableConcept> Role { get; set; }

        /// <summary>
        /// Gets or sets a reference to an actor in the audit
        /// </summary>
        [XmlElement("reference")]
        [Description("Reference to the resource representing this actor")]
        public Reference Reference { get; set; }

        /// <summary>
        /// Gets or sets the user id for the audit
        /// </summary>
        [XmlElement("userId")]
        [Description("The ID of the user involved in the event")]
        public FhirIdentifier UserId { get; set; }

        /// <summary>
        /// Gets or sets the alternate identifier for the actor
        /// </summary>
        [XmlElement("altId")]
        [Description("The alternate identifier for the actor")]
        public FhirString AltId { get; set; }

        /// <summary>
        /// Gets or sets the name of the actor
        /// </summary>
        [XmlElement("name")]
        [Description("A human meaningful name for the actor")]
        public FhirString Name { get; set; }

        /// <summary>
        /// Gets or sets whether the actor is the requestor
        /// </summary>
        [XmlElement("requestor")]
        [Description("Whether the actor is the intiator of the action")]
        [FhirElement(MinOccurs = 1)]
        public FhirBoolean IsRequestor { get; set; }

        /// <summary>
        /// Gets or sets a reference to the location where the actor acts
        /// </summary>
        [XmlElement("location")]
        [Description("The location where the actor performed the action")]
        public Reference<Location> Location { get; set; }

        /// <summary>
        /// Gets or sets the list of policies which access was granted under
        /// </summary>
        [XmlElement("policy")]
        [Description("The policy under which access or action was done")]
        public List<FhirUri> Policy { get; set; }

        /// <summary>
        /// Gets or sets the media type
        /// </summary>
        [XmlElement("media")]
        [Description("The type of media")]
        public FhirCoding Media { get; set; }

        /// <summary>
        /// Gets or sets the network information for the agent
        /// </summary>
        [XmlElement("network")]
        [Description("Network access information for the actor")]
        public AuditAgentNetwork Network { get; set; }

        /// <summary>
        /// Gets or sets the purpose of use 
        /// </summary>
        [XmlElement("purposeOfUse")]
        [Description("The reason given for access")]
        public List<FhirCodeableConcept> PurposeOfUse { get; set; }
    }
}
