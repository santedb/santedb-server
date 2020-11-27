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
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{

    /// <summary>
    /// A resource representing a photographic image
    /// </summary>
    [XmlRoot("Picture", Namespace = "http://hl7.org/fhir")]
    [XmlType("Picture", Namespace = "http://hl7.org/fhir")]
    public class Picture : DomainResourceBase
    {
        
        /// <summary>
        /// The subject of picture
        /// </summary>
        [XmlElement("subject")]
        [Description("Who/What this image is taken of")]
        public Reference<Patient> Subject { get; set; }

        /// <summary>
        /// The date/time the picture was taken
        /// </summary>
        [XmlElement("dateTime")]
        [Description("When the image was taken")]
        public FhirDateTime DateTime { get; set; }

        /// <summary>
        /// Gets or sets the operator of the image
        /// </summary>
        [XmlElement("operator")]
        [Description("The person who generated the image")]
        public Reference<Practitioner> Operator { get; set; }

        /// <summary>
        /// Identifies the image
        /// </summary>
        [XmlElement("identifier")]
        [Description("Identifier for the image")]
        public FhirIdentifier Identifier { get; set; }

        /// <summary>
        /// Used by the accessor to link back to image
        /// </summary>
        [XmlElement("accessionNo")]
        [Description("Used by the requestor to link back to the original context")]
        public FhirIdentifier AccessionNo { get; set; }

        /// <summary>
        /// Identifies the study of which the image is a part
        /// </summary>
        [XmlElement("studyId")]
        [Description("The session in which the picture was taken")]
        public FhirIdentifier StudyId { get; set; }

        /// <summary>
        /// Identiifes the series of which the image is a part
        /// </summary>
        [XmlElement("seriesId")]
        [Description("The series of images in whcih this picture was taken")]
        public FhirIdentifier SeriesId { get; set; }

        /// <summary>
        /// Identiifs the method how the image was taken
        /// </summary>
        [XmlElement("method")]
        [Description("How the image was taken")]
        public FhirCodeableConcept Method { get; set; }

        /// <summary>
        /// identifies the person that requested the image
        /// </summary>
        [XmlElement("requester")]
        [Description("Who asked that this image be taken")]
        public Reference<Practitioner> Requester { get; set; }

        /// <summary>
        /// Identifies the modality of the image
        /// </summary>
        [XmlElement("modality")]
        [Description("The type of image machinery")]
        [FhirElement(MinOccurs = 1, RemoteBinding = "http://hl7.org/fhir/picture-type")]
        public FhirCode<String> Modality { get; set; }

        /// <summary>
        /// Identifies the name of the manufacturer
        /// </summary>
        [XmlElement("deviceName")]
        [Description("The name of the manufacturer")]
        public FhirString DeviceName { get; set; }

        /// <summary>
        /// Identifies the height
        /// </summary>
        [XmlElement("height")]
        [Description("The height of the image")]
        public FhirInt Height { get; set; }

        /// <summary>
        /// Identifies the width
        /// </summary>
        [XmlElement("width")]
        [Description("The width of the image")]
        public FhirInt Width { get; set; }

        /// <summary>
        /// Identifies the BPP of the image
        /// </summary>
        [XmlElement("bits")]
        [Description("The number of bits of color (2..32)")]
        public FhirInt Bits { get; set; }

        /// <summary>
        /// Identifies the number of frames
        /// </summary>
        [XmlElement("frames")]
        [Description("The number of frames")]
        public FhirInt Frames { get; set; }

        /// <summary>
        /// Identifies the delay between frames
        /// </summary>
        [XmlElement("frameDelay")]
        [Description("The delay between frames")]
        public FhirQuantity FrameDelay { get; set; }

        /// <summary>
        /// Identifies the view (lateral, AP, etc)
        /// </summary>
        [XmlElement("view")]
        [Description("The view e.g. Lateral or Anero-posterior (AP)")]
        public FhirCodeableConcept View { get; set; }

        /// <summary>
        /// Identifies the content - ref or data
        /// </summary>
        [XmlElement("content")]
        [Description("Actual picture or reference to data")]
        [FhirElement(MinOccurs = 1)]
        public Attachment Content { get; set; }
    }
}
