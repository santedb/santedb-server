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
