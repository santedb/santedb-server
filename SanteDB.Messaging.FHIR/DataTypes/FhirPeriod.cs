using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{


    /// <summary>
    /// Represents a period of time
    /// </summary>
    [XmlType("Period", Namespace = "http://hl7.org/fhir")]
    public class FhirPeriod : FhirElement
    {

        /// <summary>
        /// Identifies the start time of the period
        /// </summary>
        [XmlElement("start")]
        public FhirDateTime Start { get; set; }

        /// <summary>
        /// Identifies the stop time of the period
        /// </summary>
        [XmlElement("end")]
        public FhirDateTime Stop { get; set; }


    }
}
