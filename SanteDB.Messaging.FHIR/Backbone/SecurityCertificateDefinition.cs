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
    /// Security certificate definition
    /// </summary>
    [XmlType("SecurityCertificateDefinition", Namespace = "http://hl7.org/fhir")]
    public class SecurityCertificateDefinition : BackboneElement
    {

        /// <summary>
        /// Gets or sets the mime type of the certificate
        /// </summary>
        [XmlElement("type")]
        [Description("Mime type of certificate")]
        public FhirCode<String> Type { get; set; }

        /// <summary>
        /// Gets or sets the blob of the certificate
        /// </summary>
        [XmlElement("blob")]
        [Description("Actual certificate")]
        public FhirBase64Binary Blob { get; set; }

    }
}
