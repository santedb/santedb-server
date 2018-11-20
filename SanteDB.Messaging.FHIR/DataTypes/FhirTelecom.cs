using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents a telecommunications address
    /// </summary>
    [XmlType("Contact", Namespace="http://hl7.org/fhir")]
    public class FhirTelecom : FhirElement
    {
        /// <summary>
        /// Gets or sets the type of contact
        /// </summary>
        [XmlElement("system")]
        public FhirCode<String> System { get; set; }
        /// <summary>
        /// Gets or sets the value of the standard
        /// </summary>
        [XmlElement("value")]
        public FhirString Value { get; set; }
        /// <summary>
        /// Gets or sets the use of the standard
        /// </summary>
        [XmlElement("use")]
        public FhirCode<String> Use { get; set; }
        /// <summary>
        /// Gets or sets the period the telecom is valid
        /// </summary>
        [XmlElement("period")]
        public FhirPeriod Period { get; set; }

        /// <summary>
        /// Write text
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {
            w.WriteStartElement("a", NS_XHTML);
            w.WriteAttributeString("href", this.Value);
            w.WriteString(this.Value.ToString());
            w.WriteEndElement(); // a
            w.WriteString(String.Format("({0})", this.Use));
        }
    }
}
