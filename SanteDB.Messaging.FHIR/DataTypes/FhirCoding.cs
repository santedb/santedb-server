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
