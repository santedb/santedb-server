using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.Resources;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents an extension
    /// </summary>
    [XmlType("Extension", Namespace = "http://hl7.org/fhir")]
    public class Extension : FhirElement
    {
        /// <summary>
        /// URL of the extension definition
        /// </summary>
        [XmlAttribute("url")]
        public String Url { get; set; }

        /// <summary>
        /// Value choice
        /// </summary>
        [XmlElement("valueInteger", typeof(FhirInt))]
        [XmlElement("valueDecimal", typeof(FhirDecimal))]
        [XmlElement("valueDateTime", typeof(FhirDateTime))]
        [XmlElement("valueDate", typeof(FhirDate))]
        [XmlElement("valueInstant", typeof(Primitive<DateTime>))]
        [XmlElement("valueString", typeof(FhirString))]
        [XmlElement("valueUri", typeof(FhirUri))]
        [XmlElement("valueBoolean", typeof(FhirBoolean))]
        [XmlElement("valueCode", typeof(FhirCode<String>))]
        [XmlElement("valueBase64Binary", typeof(FhirBase64Binary))]
        [XmlElement("valueCoding", typeof(FhirCoding))]
        [XmlElement("valueCodeableConcept", typeof(FhirCodeableConcept))]
        [XmlElement("valueAttachment", typeof(Attachment))]
        [XmlElement("valueIdentifier", typeof(FhirIdentifier))]
        [XmlElement("valueQuantity", typeof(FhirQuantity))]
        [XmlElement("valueChoice", typeof(FhirChoice))]
        [XmlElement("valueRange", typeof(FhirRange))]
        [XmlElement("valuePeriod", typeof(FhirPeriod))]
        [XmlElement("valueRatio", typeof(FhirRatio))]
        [XmlElement("valueHumanName", typeof(FhirHumanName))]
        [XmlElement("valueAddress", typeof(FhirAddress))]
        [XmlElement("valueContact" ,typeof(FhirTelecom))]
        [XmlElement("valueSchedule", typeof(FhirSchedule))]
        [XmlElement("valueResource", typeof(Reference))]
        public FhirElement Value { get; set; }


        /// <summary>
        /// Write extension information
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {
            if(this.Value != null)
                this.Value.WriteText(w);
            w.WriteString(" - Profile: ");
            w.WriteStartElement("a");
            w.WriteAttributeString("href", this.Url);
            w.WriteString(this.Url);
            w.WriteEndElement(); //a
        }

    }
}
