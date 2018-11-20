using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;
using System.ComponentModel;
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.Resources;

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
