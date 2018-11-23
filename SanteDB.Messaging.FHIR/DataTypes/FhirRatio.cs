using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents a ratio of two quantities
    /// </summary>
    [XmlType("Ratio", Namespace = "http://hl7.org/fhir")]
    public class FhirRatio : FhirElement
    {

        /// <summary>
        /// Numerator
        /// </summary>
        [XmlElement("numerator")]
        public FhirQuantity Numerator { get; set; }

        /// <summary>
        /// Denominator
        /// </summary>
        [XmlElement("denominator")]
        public FhirQuantity Denominator { get; set; }

    }
}
