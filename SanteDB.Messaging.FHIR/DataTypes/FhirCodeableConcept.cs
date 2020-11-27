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
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents a codeable concept
    /// </summary>
    [XmlType("CodeableConcept", Namespace = "http://hl7.org/fhir")]
    public class FhirCodeableConcept : FhirElement
    {
        /// <summary>
        /// Codeable concept default ctor
        /// </summary>
        public FhirCodeableConcept()
        {
            this.Coding = new List<FhirCoding>();
        }

        /// <summary>
        /// Codable concept ctor
        /// </summary>
        public FhirCodeableConcept(Uri system, String code)
        {
            this.Coding = new List<FhirCoding>() { 
                new FhirCoding(system, code) { Primary = true }
            };
        }

        /// <summary>
        /// Coding
        /// </summary>
        [XmlElement("coding")]
        public List<FhirCoding> Coding { get; set; }

        /// <summary>
        /// Gets or sets the text
        /// </summary>
        [XmlElement("text")]
        public FhirString Text { get; set; }
 
        /// <summary>
        /// Get the primary code
        /// </summary>
        public FhirCoding GetPrimaryCode()
        {
            FhirCoding primary = this.Coding.FirstOrDefault(c=>c.Primary == true);
            if (primary == null && this.Coding.Count > 0)
                return Coding[0];
            else if (primary == null)
                return null;
            return primary;
        }

        /// <summary>
        /// Get the coding from the specified namespace
        /// </summary>
        public FhirCoding GetCoding(Uri system)
        {
            return this.Coding.Find(o => o.System == system);
        }

        /// <summary>
        /// Write text
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {
            if (this.Coding.Count > 1)
            {
                w.WriteStartElement("table", NS_XHTML);
                w.WriteStartElement("tbody", NS_XHTML);

                foreach (var cd in this.Coding)
                {
                    w.WriteStartElement("tr", NS_XHTML);
                    w.WriteStartElement("td", NS_XHTML);
                    if(cd.Primary == true)
                        w.WriteAttributeString("style", "font-weight:bold");
                    cd.WriteText(w);
                    w.WriteEndElement(); // td
                    w.WriteEndElement();
                }
                w.WriteEndElement(); //tbody
                w.WriteEndElement(); // table
            }
            else if(this.Coding.Count == 1)
            {
                this.GetPrimaryCode().WriteText(w);
            }
        }
    }
}
