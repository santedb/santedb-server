using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents a range of values
    /// </summary>
    [XmlType("Range", Namespace = "http://hl7.org/fhir")]
    public class FhirRange : FhirElement
    {

        /// <summary>
        /// Lower bound of the range
        /// </summary>
        [XmlElement("low")]
        public FhirQuantity Low { get; set; }

        /// <summary>
        /// Upper bound of the range
        /// </summary>
        [XmlElement("high")]
        public FhirQuantity High { get; set; }

    }
}
