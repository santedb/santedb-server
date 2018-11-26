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
using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{

    /// <summary>
    /// Identifies an organization
    /// </summary>
    [XmlRoot("Organization",Namespace = "http://hl7.org/fhir")] 
    [XmlType("Organization", Namespace = "http://hl7.org/fhir")]
    public class Organization : DomainResourceBase
    {

        /// <summary>
        /// Gets or sets the unique identifiers for the organization
        /// </summary>
        [XmlElement("identifier")]
        [Description("Identifier for the organization")]
        public List<FhirIdentifier> Identifier { get; set; }

        /// <summary>
        /// Gets or sets the name of the organization
        /// </summary>
        [XmlElement("name")]
        [Description("Name used for the organization")]
        public FhirString Name { get; set; }

        /// <summary>
        /// Gets or sets the type of organization
        /// </summary>
        [XmlElement("type")]
        [Description("Kind of organization")]
        public FhirCodeableConcept Type { get; set; }

        /// <summary>
        /// Gets or sets the telecommunications addresses
        /// </summary>
        [XmlElement("telecom")]
        [Description("A contact detail for the organization")]
        public List<FhirTelecom> Telecom { get; set; }

        /// <summary>
        /// Gets or sets the addresses of the 
        /// </summary>
        [XmlElement("address")]
        [Description("An address for the organization")]
        public List<FhirAddress> Address { get; set; }

        /// <summary>
        /// Part of
        /// </summary>
        [XmlElement("partOf")]
        [Description("The organization of which this organization forms a part")]
        public Reference<Organization> PartOf { get; set; }

        /// <summary>
        /// Gets or sets the contact entities
        /// </summary>
        [XmlElement("contact")]
        [Description("Contact information for the organization")]
        public List<ContactEntity> ContactEntity { get; set; }

        /// <summary>
        /// Gets or sets the active flag for the item
        /// </summary>
        [XmlElement("active")]
        [Description("Whether the organization's record is still in active use")]
        public FhirBoolean Active { get; set; }

        /// <summary>
        /// Represent as a string
        /// </summary>
        public override string ToString()
        {
            return String.Format("[Organization] {0}", this.Name);
        }
    }
}
