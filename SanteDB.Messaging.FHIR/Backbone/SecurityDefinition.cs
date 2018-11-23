using SanteDB.Messaging.FHIR.Attributes;
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
    /// Identifies a security definition
    /// </summary>
    [XmlType("SecurityDefinition", Namespace = "http://hl7.org/fhir")]
    public class SecurityDefinition : BackboneElement
    {

        /// <summary>
        /// Creates a new instance of security definition
        /// </summary>
        public SecurityDefinition()
        {
            this.Service = new List<FhirCodeableConcept>();
            this.Certificate = new List<SecurityCertificateDefinition>();
        }

        /// <summary>
        /// Gets or sets whether CORS headers are enabled
        /// </summary>
        [XmlElement("cors")]
        [Description("Adds CORS headers")]
        public FhirBoolean Cors { get; set; }

        /// <summary>
        /// Gets or sets authentication schemes supported
        /// </summary>
        [XmlElement("service")]
        [Description("Identifies authentication services supported")]
        [FhirElement(RemoteBinding = "http://hl7.org/fhir/ValueSet/restful-security-service")]
        public List<FhirCodeableConcept> Service { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        [XmlElement("description")]
        [Description("General description of how security works")]
        public FhirString Description { get; set; }

        /// <summary>
        /// Gets or sets the certificates used by this service
        /// </summary>
        [XmlElement("certificate")]
        [Description("Certificates associated with security profiles")]
        public List<SecurityCertificateDefinition> Certificate { get; set; }

    }
}
