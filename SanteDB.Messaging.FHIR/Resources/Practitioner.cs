using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Attributes;
using System.ComponentModel;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Identifies a practitioner
    /// </summary>
    [XmlType("Practitioner", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("Practitioner", Namespace = "http://hl7.org/fhir")]
    public class Practitioner : DomainResourceBase
    {

        /// <summary>
        /// Default constructor
        /// </summary>
        public Practitioner()
        {
            this.Identifier = new List<FhirIdentifier>();
            this.Name = new List<FhirHumanName>();
            this.Telecom = new List<FhirTelecom>();
            this.Address = new List<FhirAddress>();
            this.Photo = new List<Attachment>();
            this.Communication = new List<FhirCodeableConcept>();
        }

        /// <summary>
        /// Gets or sets identifier
        /// </summary>
        [XmlElement("identifier")]
        public List<FhirIdentifier> Identifier { get; set; }

        /// <summary>
        /// Gets or sets the active flag
        /// </summary>
        [XmlElement("active")]
        [Description("Indicates an active / inactive practitioner")]
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
        /// Gets or sets the addresses of the user
        /// </summary>
        [XmlElement("address")]
        [Description("Addresses for the individual")]
        public List<FhirAddress> Address { get; set; }

        /// <summary>
        /// The gender of the individual
        /// </summary>
        [XmlElement("gender")]
        [Description("Gender for administrative purposes")]
        [FhirElement(RemoteBinding = "http://hl7.org/fhir/vs/administrative-gender")]
        public FhirCodeableConcept Gender { get; set; }

        /// <summary>
        /// The birth date of the individual
        /// </summary>
        [XmlElement("birthDate")]
        [Description("The date and time of birth for the individual")]
        public FhirDateTime BirthDate { get; set; }

        /// <summary>
        /// Gets or sets the photograph of the user
        /// </summary>
        [XmlElement("photo")]
        [Description("Image of the person")]
        public List<Attachment> Photo { get; set; }
        
        /// <summary>
        /// Gets or sets the qualifications of the practicioner
        /// </summary>
        [XmlElement("qualification")]
        [Description("Qualifications relevant to the provided service")]
        public List<Qualification> Qualification { get; set; }

        /// <summary>
        /// Gets or sets communications the practitioner can use
        /// </summary>
        [XmlElement("communication")]
        [Description("A language the practitioner is able to use in patient communication")]
        [FhirElement(RemoteBinding = "http://tools.ietf.org/html/bcp47")]
        public List<FhirCodeableConcept> Communication { get; set; }


        /// <summary>
        /// Represent as a string
        /// </summary>
        public override string ToString()
        {
            return String.Format("[Practitioner] {0}", this.Name.FirstOrDefault());
        }

    }
}
