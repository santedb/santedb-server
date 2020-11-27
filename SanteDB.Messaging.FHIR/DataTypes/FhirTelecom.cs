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
using System;
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
