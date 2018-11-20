using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents
    /// </summary>
    [XmlType("Schedule", Namespace = "http://hl7.org/fhir")]
    public class FhirSchedule : FhirElement
    {

        /// <summary>
        /// The event that is being scheduled
        /// </summary>
        [XmlElement("event")]
        public FhirPeriod Event { get; set; }

        /// <summary>
        /// Only if there is onw or none events
        /// </summary>
        [XmlElement("repeat")]
        public ScheduleRepeat Repeat { get; set; }

    }
}
