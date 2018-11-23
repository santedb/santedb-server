using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.Resources;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents an identifier
    /// </summary>
    [XmlType("Identifier", Namespace = "http://hl7.org/fhir")]
    public class FhirIdentifier : FhirElement
    {

        /// <summary>
        /// Identifies the intended use of the item
        /// </summary>
        [XmlElement("use")]
        public FhirCoding Use { get; set; }

        /// <summary>
        /// Represents a label for the identifier
        /// </summary>
        [XmlElement("type")]
        public FhirCodeableConcept Type { get; set; }

        /// <summary>
        /// Identifies the system which assigned the ID
        /// </summary>
        [XmlElement("system")]
        public FhirUri System { get; set; }

        /// <summary>
        /// Identifies the key (unique value) of the primitive
        /// </summary>
        [XmlElement("value")]
        public FhirString Value { get; set; }

        /// <summary>
        /// Identifies the period the identifier is valid
        /// </summary>
        [XmlElement("period")]
        public FhirPeriod Period { get; set; }

        /// <summary>
        /// Identifies the assigning organization of the identifier
        /// </summary>
        [XmlElement("assigner")]
        public Reference<Organization> Assigner { get; set; }

        /// <summary>
        /// Identifier
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {

                w.WriteStartElement("strong", NS_XHTML);
                if (this.Type == null)
                    w.WriteString("UNKNOWN");
                else
                    this.Type.WriteText(w);
                w.WriteString(":");
                w.WriteEndElement();//strong


            if(this.Value != null)
                this.Value.WriteText(w);

            // System in brackets
            if (this.System != null)
            {
                w.WriteString("(");
                this.System.WriteText(w);
                w.WriteString(")");
            }

            // Italic (the name of the maintainer
            if (this.Assigner != null && this.Assigner.Display != null)
            {
                w.WriteStartElement("br", NS_XHTML);
                w.WriteEndElement();
                w.WriteStartElement("em", NS_XHTML);
                this.Assigner.Display.WriteText(w);
                w.WriteEndElement();
            }

            
            
        }

    }
}
