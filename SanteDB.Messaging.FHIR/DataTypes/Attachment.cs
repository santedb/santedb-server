using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Identifies an attachment
    /// </summary>
    [XmlType("Attachment", Namespace = "http://hl7.org/fhir")]
    [Serializable]
    public class Attachment : FhirElement
    {

        /// <summary>
        /// Gets or sets the content-type
        /// </summary>
        [XmlElement("contentType")]
        public FhirCode<String> ContentType { get; set; }

        /// <summary>
        /// Gets or sets the language
        /// </summary>
        [XmlElement("language")]
        public FhirCode<String> Language { get; set; }

        /// <summary>
        /// Gets or sets the data for the attachment
        /// </summary>
        [XmlElement("data")]
        public FhirBase64Binary Data { get; set; }

        /// <summary>
        /// Gets or sets a url reference
        /// </summary>
        [XmlElement("url")]
        public FhirUri Url { get; set; }

        /// <summary>
        /// Gets or sets the size
        /// </summary>
        [XmlElement("size")]
        public FhirInt Size { get; set; }

        /// <summary>
        /// Gets or sets the hash code
        /// </summary>
        [XmlElement("hash")]
        public Primitive<byte[]> Hash { get; set; }

        /// <summary>
        /// Gets or sets the title
        /// </summary>
        [XmlElement("title")]
        public FhirString Title { get; set; }

    }
}
