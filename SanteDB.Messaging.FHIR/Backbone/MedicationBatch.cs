using SanteDB.Messaging.FHIR.DataTypes;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{

    /// <summary>
    /// Medication batch
    /// </summary>
    [XmlType("MedicationBatch", Namespace = "http://hl7.org/fhir")]
    public class MedicationBatch : BackboneElement
    {

        /// <summary>
        /// Gets or sets the lot number
        /// </summary>
        [XmlElement("lotNumber")]
        [Description("Identifier assigned to the batch")]
        public FhirString LotNumber { get; set; }

        /// <summary>
        /// Gets or sets the expiration time
        /// </summary>
        [XmlElement("expirationDate")]
        [Description("When the batch will expire")]
        public FhirDateTime Expiration { get; set; }
    }
}