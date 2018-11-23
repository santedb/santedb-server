using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.Resources;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Codified concept
    /// </summary>
    [XmlType("Coding", Namespace = "http://hl7.org/fhir")]
    public class FhirCoding : FhirElement
    {
        /// <summary>
        /// Coding
        /// </summary>
        public FhirCoding()
        {

        }

        /// <summary>
        /// Creates a new coding variable
        /// </summary>
        public FhirCoding(Uri system, string code)
        {
            this.System = new FhirUri(system);
            this.Code = new FhirCode<string>(code);
        }

        /// <summary>
        /// The codification system
        /// </summary>
        [XmlElement("system")]
        public FhirUri System { get; set; }

        /// <summary>
        /// Version of the codification system
        /// </summary>
        [XmlElement("version")]
        public FhirString Version { get; set; }

        /// <summary>
        /// The code 
        /// </summary>
        [XmlElement("code")]
        public FhirCode<String> Code { get; set; }

        /// <summary>
        /// Gets or sets the display
        /// </summary>
        [XmlElement("display")]
        public FhirString Display { get; set; }

        /// <summary>
        /// Primary code?
        /// </summary>
        [XmlIgnore]
        public FhirBoolean Primary { get; set; }

        /// <summary>
        /// Write text
        /// </summary>
        /// <param name="w"></param>
        internal override void WriteText(System.Xml.XmlWriter w)
        {
            if (this.Display != null)
                w.WriteString(this.Display);
            else
                w.WriteString(this.Code);
            if (this.System != null)
            {
                w.WriteString(" (");
                this.System.WriteText(w);
                w.WriteString(")");
            }
        }
    }
}
