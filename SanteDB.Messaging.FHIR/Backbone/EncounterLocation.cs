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
    /// A backbone element which allows expression of locations in an encounter
    /// </summary>
    [XmlType("EncounterLocation", Namespace = "http://hl7.org/fhir")]
    public class EncounterLocation : BackboneElement
    {

        /// <summary>
        /// Gets or sets the location
        /// </summary>
        [XmlElement("location")]
        [FhirElement(MinOccurs = 1)]
        [Description("The location involved in the encounter")]
        public Reference<Location> Location { get; set; }

        /// <summary>
        /// Gets or sets the period that this location was involved.
        /// </summary>
        [XmlElement("period")]
        [Description("The period of involvement of this location")]
        public FhirPeriod Period { get; set; }
    }
}
