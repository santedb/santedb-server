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
    /// Represents the dosage of medication given to a patient
    /// </summary>
    [XmlType("MedicationDosage", Namespace = "http://hl7.org/fhir")]
    public class MedicationDosage : BackboneElement
    {

        /// <summary>
        /// Gets or sets the textual description of the dosage
        /// </summary>
        [XmlElement("text")]
        [Description("Free text dosing instructions")]
        public FhirString Text { get; set; }

        /// <summary>
        /// Gets or sets the site of administration
        /// </summary>
        [XmlElement("site")]
        [Description("Body site administered to")]
        public FhirCodeableConcept Site { get; set; }

        /// <summary>
        /// Route of administration
        /// </summary>
        [XmlElement("route")]
        [Description("Route of administration")]
        public FhirCodeableConcept Route { get; set; }

        /// <summary>
        /// Gets or sets the method of administration
        /// </summary>
        [XmlElement("method")]
        [Description("Method of administration of drug")]
        public FhirCodeableConcept Method { get; set; }

        /// <summary>
        /// Gets or sets the dose amount
        /// </summary>
        [XmlElement("dose")]
        [Description("Dose quantity of medication")]
        public FhirQuantity Dose { get; set; }
        
    }
}
