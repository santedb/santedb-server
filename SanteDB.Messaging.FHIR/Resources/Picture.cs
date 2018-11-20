using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Attributes;
using System.ComponentModel;

namespace SanteDB.Messaging.FHIR.Resources
{

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
