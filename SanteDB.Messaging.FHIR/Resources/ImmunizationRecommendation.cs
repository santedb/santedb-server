using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Represents the proposal to perform an immunization
    /// </summary>
    [XmlType(nameof(ImmunizationRecommendation), Namespace = "http://hl7.org/fhir")]
    [XmlRoot(nameof(ImmunizationRecommendation), Namespace = "http://hl7.org/fhir")]
    public class ImmunizationRecommendation : DomainResourceBase
    {

        /// <summary>
        /// Immunization recommendataion
        /// </summary>
        public ImmunizationRecommendation()
        {
            this.Identifier = new List<FhirIdentifier>();
            this.Recommendation = new List<Backbone.ImmunizationRecommendation>();
        }

        /// <summary>
        /// Gets or sets the identifiers for the immunization recommendation
        /// </summary>
        [XmlElement("identifier")]
        public List<FhirIdentifier> Identifier { get; set; }

        /// <summary>
        /// Gets or sets the patient to which the recommendation applies
        /// </summary>
        [XmlElement("patient")]
        [FhirElement(MinOccurs = 1)]
        public Reference<Patient> Patient { get; set; }

        /// <summary>
        /// Gets or sets recommendations
        /// </summary>
        [XmlElement("recommendation")]
        [FhirElement(MinOccurs = 1)]
        public List<Backbone.ImmunizationRecommendation> Recommendation { get; set; }

    }
}
