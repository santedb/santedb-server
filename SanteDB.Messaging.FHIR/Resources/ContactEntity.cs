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
using SanteDB.Messaging.FHIR.DataTypes;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Identifies a contact entity
    /// </summary>
    [XmlType("ContactEntity", Namespace = "http://hl7.org/fhir")]
    public class ContactEntity : FhirElement
    {
        /// <summary>
        /// Gets or sets the type of contact entity
        /// </summary>
        [XmlElement("purpose")]
        [Description("The type of contact")]
        public FhirCodeableConcept Type { get; set; }
        /// <summary>
        /// Gets or sets the name of the contact entity
        /// </summary>
        [XmlElement("name")]
        [Description("A name associated with the contact entity")]
        public FhirHumanName Name { get; set; }
        /// <summary>
        /// Gets or sets the telecommunications address of the entity
        /// </summary>
        [XmlElement("telecom")]
        [Description("Contact details (telephone, email, etc) for a contact")]
        public List<FhirTelecom> Telecom { get; set; }
        /// <summary>
        /// Gets or sets the address of the entity
        /// </summary>
        [XmlElement("address")]
        [Description("Visiting or postal address for the contact")]
        public FhirAddress Address { get; set; }


    }
}
