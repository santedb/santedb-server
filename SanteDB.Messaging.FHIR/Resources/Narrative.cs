/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * User: justin
 * Date: 2018-11-23
 */
using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

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
