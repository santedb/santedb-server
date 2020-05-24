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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Represents a related person
    /// </summary>
    [XmlType("RelatedPerson", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("RelatedPerson", Namespace = "http://hl7.org/fhir")]
    public class RelatedPerson : DomainResourceBase
    {
        /// <summary>
        /// Related person
        /// </summary>
        public RelatedPerson()
        {
            this.Identifier = new List<FhirIdentifier>();
            this.Telecom = new List<FhirTelecom>();
            this.Photo = new List<Attachment>();
        }

        /// <summary>
        /// Gets or sets the identifier for the relationship
        /// </summary>
        [Description("An identifier for the person as a relationship")]
        [FhirElement(MaxOccurs = -1)]
        [XmlElement("identifier")]
        public List<FhirIdentifier> Identifier { get; set; }

        /// <summary>
        /// Gets or sets the patient to which this person is related
        /// </summary>
        [Description("The person to which this person is related")]
        [FhirElement(MaxOccurs = 1, MinOccurs = 1)]
        [XmlElement("patient")]
        public Reference<Patient> Patient { get; set; }

        /// <summary>
        /// Gets or sets the relationship type
        /// </summary>
        [Description("The relationship this person has with the patient")]
        [FhirElement(MaxOccurs = 0, MinOccurs = 1, RemoteBinding = "http://hl7.org/fhir/vs/relatedperson-relationshiptype")]
        [XmlElement("relationship")]
        public FhirCodeableConcept Relationship { get; set; }

        /// <summary>
        /// Gets or sets the person's name
        /// </summary>
        [Description("The name of the related person")]
        [FhirElement(MaxOccurs = 1)]
        [XmlElement("name")]
        public FhirHumanName Name { get; set; }

        /// <summary>
        /// Gets or sets the person's telecom addresses
        /// </summary>
        [Description("Telecommunications addresses")]
        [FhirElement(MaxOccurs = -1)]
        [XmlElement("telecom")]
        public List<FhirTelecom> Telecom { get; set; }

        /// <summary>
        /// Gets or sets the gender of the patient
        /// </summary>
        [XmlElement("gender")]
        [Description("Gender for administrative purposes")]
        [FhirElement(RemoteBinding = "http://hl7.org/fhir/vs/administrative-gender")]
        public FhirCodeableConcept Gender { get; set; }

        /// <summary>
        /// Gets or sets the address of the related person
        /// </summary>
        [XmlElement("address")]
        [Description("Address of the related person")]
        [FhirElement(MaxOccurs = 1)]
        public FhirAddress Address { get; set; }

        /// <summary>
        /// Gets or sets the photograph of the person
        /// </summary>
        [XmlElement("photo")]
        [Description("Photograph of the related person")]
        [FhirElement(MaxOccurs = -1)]
        public List<Attachment> Photo { get; set; }

        /// <summary>
        /// Write textual content
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter xw)
        {
            xw.WriteStartElement("div");
            xw.WriteAttributeString("class", "h1");
            xw.WriteString(String.Format("Related Person {0}", this.Id));
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
            base.WriteTableRows(xw, "Name", this.Name);
            base.WriteTableRows(xw, "Gender", this.Gender);
            base.WriteTableRows(xw, "Address", this.Address);
            base.WriteTableRows(xw, "Telecom", this.Telecom.ToArray());
            // Related to
            base.WriteTableRows(xw, "Relation", this.Relationship);
            base.WriteTableRows(xw, "To Patient", this.Patient);
            // Extended Attributes
            base.WriteTableRows(xw, "Extended Attributes", this.Extension.ToArray());
            xw.WriteEndElement(); // tbody
            xw.WriteEndElement(); // table
        }

        /// <summary>
        /// Represent as a string
        /// </summary>
        public override string ToString()
        {
            return String.Format("Patient: {0}", this.Name);
        }

    }
}
