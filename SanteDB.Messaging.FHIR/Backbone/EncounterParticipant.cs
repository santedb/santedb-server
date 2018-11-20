using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources;
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
    /// Represents a participant who actively cooperates in teh carrying out of the encounter
    /// </summary>
    [XmlType("EncounterParticipant", Namespace = "http://hl7.org/fhir")]
    public class EncounterParticipant : BackboneElement
    {

        /// <summary>
        /// Gets or sets the type of role
        /// </summary>
        [Description("Role of the participant in the encounter")]
        [XmlElement("type")]
        [FhirElement(MinOccurs = 0)]
        public List<FhirCodeableConcept> Type { get; set; }

        /// <summary>
        /// Gets or sets the period of involvement
        /// </summary>
        [XmlElement("period")]
        [Description("The period that this person was involved")]
        public FhirPeriod Period { get; set; }

        /// <summary>
        /// Gets or sets the person involved
        /// </summary>
        [XmlElement("individual")]
        [Description("The person who was involved")]
        public Reference<Practitioner> Individual { get; set; }
    }
}
