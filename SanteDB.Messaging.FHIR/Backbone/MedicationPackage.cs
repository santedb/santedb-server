using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Represents packaging of a particular medication
    /// </summary>
    [XmlType("MedicationPackage", Namespace = "http://hl7.org/fhir")]
    public class MedicationPackage : BackboneElement
    {

        /// <summary>
        /// Gets or sets information about the container
        /// </summary>
        [XmlElement("container")]
        [Description("Type of container")]
        [FhirElement(RemoteBinding = "http://hl7.org/fhir/ValueSet/medication-package-form")]
        public FhirCodeableConcept Container { get; set; }

        /// <summary>
        /// Gets or sets details about the batch
        /// </summary>
        [XmlElement("batch")]
        [Description("Represents the batch of a particular package")]
        public MedicationBatch Batch { get; set; }
    }
}
