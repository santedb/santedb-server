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
    /// A backbone element for tracking encounter status history
    /// </summary>
    [XmlType("EncounterStatusHistory", Namespace = "http://hl7.org/fhir")]
    public class EncounterStatusHistory : BackboneElement
    {

        /// <summary>
        /// Gets or sets the history
        /// </summary>
        [XmlElement("status")]
        [FhirElement(MinOccurs = 1)]
        [Description("The status of the encounter at this point in time")]
        public FhirCode<EncounterStatus> Status { get; set; }

        /// <summary>
        /// Gets or sets the period 
        /// </summary>
        [XmlElement("period")]
        [FhirElement(MinOccurs = 1)]
        [Description("The time span when the status was valid")]
        public FhirPeriod Period { get; set; }
    }
}
