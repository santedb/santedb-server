using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{

    /// <summary>
    /// Bundle resource
    /// </summary>
    [XmlType("Bundle.Resource", Namespace = "http://hl7.org/fhir")]
    public class BundleResrouce : FhirElement
    {

        /// <summary>
        /// 
        /// </summary>
        public BundleResrouce()
        {

        }
        /// <summary>
        /// Creates a new instance of the resource bunlde
        /// </summary>
        /// <param name="r"></param>
        public BundleResrouce(DomainResourceBase r)
        {
            this.Resource = r;
        }

        /// <summary>
        /// Gets or sets the resource
        /// </summary>
        [XmlElement("Patient", Type = typeof(Patient))]
        [XmlElement("ValueSet", Type = typeof(ValueSet))]
        [XmlElement("Organization", Type = typeof(Organization))]
        [XmlElement("Practitioner", Type = typeof(Practitioner))]
        [XmlElement("Immunization", Type = typeof(Immunization))]
        [XmlElement("ImmunizationRecommendation", Type = typeof(Resources.ImmunizationRecommendation))]
        [XmlElement("RelatedPerson", Type = typeof(RelatedPerson))]
        [XmlElement("Location", Type = typeof(Location))]
        [XmlElement("Observation", Type = typeof(Observation))]
        [XmlElement("Medication", Type = typeof(Medication))]
        [XmlElement("Substance", Type = typeof(Substance))]
        [XmlElement("AllergyIntolerance", Type = typeof(AllergyIntolerance))]
        [XmlElement("AdverseEvent", Type = typeof(AdverseEvent))]
        [XmlElement("Condition", Type = typeof(Condition))]
        [XmlElement("MedicationAdministration", Type = typeof(MedicationAdministration))]
        [XmlElement("Encounter", Type = typeof(Encounter))]
        public DomainResourceBase Resource { get; set; }

    }
}