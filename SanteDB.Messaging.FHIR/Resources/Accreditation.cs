using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Attributes;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Represents an accreditation
    /// </summary>
    [XmlType("Accreditation", Namespace = "http://hl7.org/fhir")]
    public class Accreditation : FhirElement
    {
        /// <summary>
        /// Gets or sets the identifier for the accreditation
        /// </summary>
        [XmlElement("identifier")]
        public FhirIdentifier Identifier { get; set; }
        /// <summary>
        /// Gets or sets the code (type) of the accreditation
        /// </summary>
        [XmlElement("code")]
        public FhirCodeableConcept Code { get; set; }
        /// <summary>
        /// Gets or sets the issuing organization of the accreditation
        /// </summary>
        [XmlElement("issuer")]
        public Reference<Organization> Issuer { get; set; }
        /// <summary>
        /// Gets or sets the period of the accreditation
        /// </summary>
        [XmlElement("period")]
        public FhirPeriod Period { get; set; }

    }
}
