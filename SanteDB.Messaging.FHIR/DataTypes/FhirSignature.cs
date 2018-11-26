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
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.Resources;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// A signature holds digital signature information
    /// </summary>
    [XmlType("Signature", Namespace = "http://hl7.org/fhir")]
    public class FhirSignature : FhirElement
    {

        /// <summary>
        /// Gets or sets the indication of the reason the entity signed the object
        /// </summary>
        [XmlElement("type")]
        [Description("Indication of the reason the entity signed the objects")]
        [FhirElement(MinOccurs = 1)]
        public FhirCoding Type { get; set; }

        /// <summary>
        /// Gets or sets when the signature was created
        /// </summary>
        [XmlElement("when")]
        [Description("When the signature was created")]
        [FhirElement(MinOccurs = 1)]
        public FhirInstant CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the author of the signature
        /// </summary>
        [XmlElement("whoUri", Type = typeof(FhirUri))]
        [XmlElement("whoReference", Type = typeof(Reference))]
        [Description("Who signed the signature")]
        public FhirElement Author { get; set; }

        /// <summary>
        /// Gets or sets the technical format of the signature
        /// </summary>
        [XmlElement("contentType")]
        [Description("The technical format of the signature")]
        public FhirCode<String> ContentType { get; set; }

        /// <summary>
        /// Gets or sets the actual signature content
        /// </summary>
        [XmlElement("blob")]
        [Description("The actual signature content")]
        public FhirBase64Binary Blob { get; set; }
    }
}
