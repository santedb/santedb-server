using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Narrative
    /// </summary>
    [XmlType("Narrative", Namespace = "http://hl7.org/fhir")]
    [Serializable]
    public class Narrative : FhirElement
    {

        /// <summary>
        /// Gets or sets the status of the narrative
        /// </summary>
        [XmlElement("status")]
        public FhirCode<String> Status { get; set; }

        /// <summary>
        /// Gets or sets the contents
        /// </summary>
        [XmlElement("div", Namespace = "http://www.w3.org/1999/xhtml")]
        public RawXmlWrapper Div { get; set; }

        /// <summary>
        /// Convert to string
        /// </summary>
        public override string ToString()
        {
            StringWriter writer = new StringWriter();
            using(XmlWriter xw = XmlWriter.Create(writer, new XmlWriterSettings() { ConformanceLevel = ConformanceLevel.Fragment }))
                foreach (var e in (XmlElement[])Div)
                    e.WriteTo(xw);

            return writer.ToString();
        }
    }
}
