using SanteDB.Messaging.FHIR.Attributes;
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
    /// Represents a backbone element of who performed a medication administration
    /// event
    /// </summary>
    [XmlType("MedicationPerformer", Namespace = "http://hl7.org/fhir")]
    public class MedicationPerformer : BackboneElement
    {

        /// <summary>
        /// The individual who is performing the act
        /// </summary>
        [XmlElement("actor")]
        [FhirElement(MinOccurs = 1)]
        [Description("Individual who was performing the administration")]
        public Reference<Practitioner> Actor { get; set; }
    }
}
