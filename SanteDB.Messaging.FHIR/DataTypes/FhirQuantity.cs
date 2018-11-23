using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// A measured amount of data
    /// </summary>
    [XmlType("Quantity", Namespace = "http://hl7.org/fhir")]
    public class FhirQuantity : FhirElement
    {
        /// <summary>
        /// Gets or sets the primitive value of the quantity
        /// </summary>
        [XmlElement("value")]
        public FhirDecimal Value { get; set; }

        /// <summary>
        /// Gets or sets the relationship of the stated value and the real value
        /// </summary>
        [XmlElement("comparator")]
        public FhirCode<String> Comparator { get; set; }

        /// <summary>
        /// Gets or sets the units of measure
        /// </summary>
        [XmlElement("unit")]
        public FhirString Units { get; set; }

        /// <summary>
        /// Gets or sets the system of the units of measure
        /// </summary>
        [XmlElement("system")]
        public FhirUri System { get; set; }

        /// <summary>
        /// Gets or sets the code
        /// </summary>
        [XmlElement("code")]
        public FhirCode<String> Code { get; set; }

    }
}
