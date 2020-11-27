/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using SanteDB.Messaging.FHIR.Resources;
using System;
using System.Xml.Serialization;

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
