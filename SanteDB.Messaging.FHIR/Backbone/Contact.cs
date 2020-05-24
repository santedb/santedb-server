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
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Contact information
    /// </summary>
    [XmlType("Patient.ContactInfo", Namespace = "http://hl7.org/fhir")]
    public class PatientContact : BackboneElement
    {

        /// <summary>
        /// Creates a new patient contact
        /// </summary>
        public PatientContact()
        {
            this.Relationship = new List<FhirCodeableConcept>();
            this.Telecom = new List<FhirTelecom>();
        }

        /// <summary>
        /// Gets or sets the relationships between the container
        /// </summary>
        [XmlElement("relationship")]
        [Description("The kind of relationship")]
        public List<FhirCodeableConcept> Relationship { get; set; }

        // The name of the individual
        /// </summary>
        [XmlElement("name")]
        [Description("A name associated with the individual")]
        public FhirHumanName Name { get; set; }

        /// <summary>
        /// The telecommunications addresses for the individual
        /// </summary>
        [XmlElement("telecom")]
        [Description("A contact detail for the individual")]
        public List<FhirTelecom> Telecom { get; set; }

        /// <summary>
        /// Gets or sets the addresses of the user
        /// </summary>
        [XmlElement("address")]
        [Description("Address for the individual")]
        public FhirAddress Address { get; set; }
        
        /// <summary>
        /// The gender of the individual
        /// </summary>
        [XmlElement("gender")]
        [Description("Gender for administrative purposes")]
        [FhirElement(RemoteBinding = "http://hl7.org/fhir/ValueSet/administrative-gender")]
        public FhirCode<String> Gender { get; set; }

        /// <summary>
        /// Gets or sets the organization
        /// </summary>
        [XmlElement("organization")]
        [Description("Organization that is associated with the contact")]
        public Reference<Organization> Organization { get; set; }

        /// <summary>
        /// Gets or sets a period describing when the contact is valid
        /// </summary>
        [XmlElement("period")]
        [Description("The period during which this contact person is valid")]
        public FhirPeriod Period { get; set; }

        /// <summary>
        /// Write the contact information
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter xw)
        {
            if (this.Name != null)
                this.Name.WriteText(xw);

            xw.WriteString(" - ");
            foreach (var rel in this.Relationship)
            {
                rel.WriteText(xw);
                xw.WriteRaw(", ");
            }
        }
    }
}
