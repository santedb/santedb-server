using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.DataTypes;
using System.ComponentModel;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Represents data related to animal patients
    /// </summary>
    [XmlType("Patient.Animal", Namespace = "http://hl7.org/fhir")]
    public class Animal : BackboneElement
    {
        /// <summary>
        /// Gets or sets the species code
        /// </summary>
        [XmlElement("species")]
        [Description("E.g. Dog, Cow")]
        public FhirCodeableConcept Species { get; set; }

        /// <summary>
        /// Gets or sets the breed code
        /// </summary>
        [XmlElement("breed")]
        [Description("E.g. Poodle, Angus")]
        public FhirCodeableConcept Breed { get; set; }

        /// <summary>
        /// Gets or sets the status of the gender
        /// </summary>
        [XmlElement("genderStatus")]
        [Description("E.g. Neutered, Intact")]
        public FhirCodeableConcept GenderStatus { get; set; }

    }
}
