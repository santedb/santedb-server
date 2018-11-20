using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.Attributes;
using System.ComponentModel;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Qualification
    /// </summary>
    [XmlType("Qualification", Namespace = "http://hl7.org/fhir")]
    public class Qualification : FhirElement
    {
        /// <summary>
        /// Qualification
        /// </summary>
        public Qualification()
        {
            this.Identifier = new List<FhirIdentifier>();
        }

        /// <summary>
        /// Identifier for this qualification
        /// </summary>
        [XmlElement("identifier")]
        [Description("An identifier for this qualification")]
        public List<FhirIdentifier> Identifier { get; set; }

        /// <summary>
        /// Gets or sets the code
        /// </summary>
        [XmlElement("code")]
        [FhirElement(MinOccurs = 1)]
        public FhirCodeableConcept Code { get; set; }

        /// <summary>
        /// Gets or sets the period of time
        /// </summary>
        [XmlElement("period")]
        public FhirPeriod Period { get; set; }

        /// <summary>
        /// Gets or sets the issuer organization
        /// </summary>
        [XmlElement("issuer")]
        public Reference<Organization> Issuer { get; set; }

    }
}
