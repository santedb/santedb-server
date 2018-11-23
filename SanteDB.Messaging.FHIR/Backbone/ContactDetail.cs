using SanteDB.Messaging.FHIR.DataTypes;
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
    /// Represents a publisher of a conformance resource
    /// </summary>
    [XmlType("ContactDetail", Namespace = "http://hl7.org/fhir")]
    public class ContactDetail : BackboneElement
    {

        /// <summary>
        /// Creates a new conformance publisher
        /// </summary>
        public ContactDetail()
        {
            this.Telecom = new List<FhirTelecom>();
        }

        /// <summary>
        /// Gets or sets the name of the contact
        /// </summary>
        [XmlElement("name")]
        [Description("Name of an individual to contact")]
        public FhirString Name { get; set; }

        /// <summary>
        /// Gets or sets the telecom addresses
        /// </summary>
        [XmlElement("telecom")]
        [Description("Contact details for individual or publisher")]
        public List<FhirTelecom> Telecom { get; set; }
    }
}
