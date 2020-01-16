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
using SanteDB.Core.Auditing;
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// The patient resource
    /// </summary>
    [XmlType("Patient", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("Patient", Namespace = "http://hl7.org/fhir")] 
    [ParticipantObjectMap(IdType = AuditableObjectIdType.PatientNumber, Role = AuditableObjectRole.Patient, Type = AuditableObjectType.Person, OidName = "CR_CID")]
    public class Patient : DomainResourceBase
    {
        /// <summary>
        /// Namespace Declarations
        /// </summary>
        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces Namespaces { get { return this.m_namespaces; } }

        /// <summary>
        /// Patient constructor
        /// </summary>
        public Patient()
        {
            this.Link = new List<PatientLink>();
            this.Identifier = new List<DataTypes.FhirIdentifier>();
            this.Active = new FhirBoolean(true);
            this.Name = new List<FhirHumanName>();
            this.Telecom = new List<FhirTelecom>();
            this.Address = new List<FhirAddress>();
            this.Communication = new List<Communication>();
            this.Photo = new List<Attachment>();
            this.Contact = new List<PatientContact>();
        }

        /// <summary>
        /// Gets or sets a list of identifiers
        /// </summary>
        [XmlElement("identifier")]
        [Description("An identifier for the person as this patient")]
        [FhirElement(MaxOccurs = -1)]
        public List<DataTypes.FhirIdentifier> Identifier { get; set; }

        /// <summary>
        /// True when the patient is active
        /// </summary>
        [XmlElement("active")]
        [Description("Whether this patient's record is in active use")]
        public FhirBoolean Active { get; set; }

        /// <summary>
        /// The name of the individual
        /// </summary>
        [XmlElement("name")]
        [Description("A name associated with the individual")]
        public List<FhirHumanName> Name { get; set; }

        /// <summary>
        /// The telecommunications addresses for the individual
        /// </summary>
        [XmlElement("telecom")]
        [Description("A contact detail for the individual")]
        public List<FhirTelecom> Telecom { get; set; }

        /// <summary>
        /// The gender of the individual
        /// </summary>
        [XmlElement("gender")]
        [Description("Gender for administrative purposes")]
        [FhirElement(RemoteBinding = "http://hl7.org/fhir/ValueSet/administrative-gender")]
        public FhirCode<String> Gender { get; set; }

        /// <summary>
        /// The birth date of the individual
        /// </summary>
        [XmlElement("birthDate")]
        [Description("The date and time of birth for the individual")]
        public FhirDateTime BirthDate { get; set; }

        /// <summary>
        /// True if the individual is deceased
        /// </summary>
        [XmlElement("deceasedDateTime", typeof(FhirDateTime))]
        [XmlElement("deceasedBoolean", typeof(FhirBoolean))]
        [Description("Indicates if the individual is deceased or not")]
        public Object Deceased { get; set; }

        /// <summary>
        /// Gets or sets the addresses of the user
        /// </summary>
        [XmlElement("address")]
        [Description("Addresses for the individual")]
        public List<FhirAddress> Address { get; set; }

        /// <summary>
        /// Gets or sets the marital status of the user
        /// </summary>
        [XmlElement("maritalStatus")]
        [Description("Marital (civil) status of a person")]
        [FhirElement(RemoteBinding = "http://hl7.org/fhir/vs/marital-status")]
        public FhirCodeableConcept MaritalStatus { get; set; }

        /// <summary>
        /// The multiple birth indicator
        /// </summary>
        [XmlElement("multipleBirthInteger", typeof(FhirInt))]
        [XmlElement("multipleBirthBoolean", typeof(FhirBoolean))]
        [Description("Whether patient is part of a multiple birth")]
        public FhirElement MultipleBirth { get; set; }

        /// <summary>
        /// Gets or sets the photograph of the user
        /// </summary>
        [XmlElement("photo")]
        [Description("Image of the person")]
        public List<Attachment> Photo { get; set; }

        /// <summary>
        /// Contact details
        /// </summary>
        [Description("A contact party (e.g. guardian, partner, friend) for the patient")]
        [XmlElement("contact")]
        [FhirElement(MaxOccurs = -1)]
        public List<PatientContact> Contact { get; set; }

        /// <summary>
        /// Animal reference
        /// </summary>
        [XmlElement("animal")]
        [Description("If this patient is a non-human")]
        public Animal Animal { get; set; }

        /// <summary>
        /// Gets or sets the language of the user
        /// </summary>
        [Description("Person's proficiancy level of a language")]
        [XmlElement("communication")]
        public List<Communication> Communication { get; set; }

        /// <summary>
        /// Provider of the patient resource
        /// </summary>
        [XmlElement("generalPractitioner")]
        [Description("Provider managing this patient")]
        public Reference Provider { get; set; }

        /// <summary>
        /// Provider of the patient resource
        /// </summary>
        [XmlElement("managingOrganization")]
        [Description("Organization managing this patient")]
        public Reference<Organization> ManagingOrganization { get; set; }

        /// <summary>
        /// Link between this patient and others
        /// </summary>
        [XmlElement("link")]
        [Description("Other patient resources linked to this patient resource")]
        public List<PatientLink> Link { get; set; }
      
        /// <summary>
        /// Generate the narrative
        /// </summary>
        internal override void WriteText(XmlWriter xw)
        {
            xw.WriteStartElement("div");
            xw.WriteAttributeString("class", "h1");
            xw.WriteString(String.Format("Patient {0}", this.Id));
            xw.WriteEndElement(); // div

            // Now output
            xw.WriteStartElement("table", NS_XHTML);
            xw.WriteAttributeString("border", "1");
            xw.WriteElementString("caption", NS_XHTML, "Identifiers");
            xw.WriteStartElement("tbody", NS_XHTML);
            base.WriteTableRows(xw, "Identifiers", this.Identifier.ToArray());
            xw.WriteEndElement(); // tbody
            xw.WriteEndElement(); // table

            // Now output demographics
            xw.WriteStartElement("table", NS_XHTML);
            xw.WriteAttributeString("border", "1");
            xw.WriteElementString("caption", NS_XHTML, "Demographic Information");
            xw.WriteStartElement("tbody", NS_XHTML);
            base.WriteTableRows(xw, "Name", this.Name.ToArray());
            base.WriteTableRows(xw, "DOB", this.BirthDate);
            base.WriteTableRows(xw, "Gender", this.Gender);
            base.WriteTableRows(xw, "Address", this.Address.ToArray());
            base.WriteTableRows(xw, "Telecom", this.Telecom.ToArray());
            // Contacts
            if (this.Contact != null)
                base.WriteTableRows(xw, "Contacts", this.Contact.ToArray());
            // Extended Attributes
            base.WriteTableRows(xw, "Extended Attributes", this.Extension.ToArray());

            if (this.Contained != null)
                base.WriteTableRows(xw, "Contained Resources", this.Contained.ToArray());

            xw.WriteEndElement(); // tbody
            xw.WriteEndElement(); // table

            
        }

        /// <summary>
        /// Represent as a string
        /// </summary>
        public override string ToString()
        {
            return String.Format("[Patient] {0}", this.Name.FirstOrDefault());
        }
    }
}
