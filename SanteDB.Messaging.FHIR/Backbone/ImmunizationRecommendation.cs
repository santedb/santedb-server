using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Represents the recommendation
    /// </summary>
    [XmlType("ImmunizationRecommendation.ImmunizationRecommendation", Namespace = "http://hl7.org/fhir")]
    public class ImmunizationRecommendation : BackboneElement
    {

        /// <summary>
        /// Recommendation
        /// </summary>
        public ImmunizationRecommendation()
        {
            this.DateCriterion = new List<ImmunizationRecommendationDateCriterion>();
        }

        /// <summary>
        /// Gets or sets the date of the recommendatation
        /// </summary>
        [XmlElement("date")]
        [FhirElement(MinOccurs = 1)]
        public FhirDateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the vaccine code
        /// </summary>
        [XmlElement("vaccineCode")]
        [FhirElement(MinOccurs = 1)]
        public FhirCodeableConcept VaccineCode { get; set; }

        /// <summary>
        /// Gets or sets the dose number
        /// </summary>
        [XmlElement("doseNumber")]
        public FhirInt DoseNumber { get; set; }

        /// <summary>
        /// Gets or sets the forecast status
        /// </summary>
        [XmlElement("forecastStatus")]
        [FhirElement(MinOccurs = 1)]
        public FhirCodeableConcept ForecastStatus { get; set; }

        /// <summary>
        /// Gets or sets the date critereons
        /// </summary>
        [XmlElement("dateCriterion")]
        public List<ImmunizationRecommendationDateCriterion> DateCriterion { get; set; }

        /// <summary>
        /// Gets or sets the protocol
        /// </summary>
        [XmlElement("protocol")]
        public ImmunizationProtocol Protocol { get; set; }

    }
}