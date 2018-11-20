using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using System.ComponentModel;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Represents a physical position
    /// </summary>
    [XmlType("Position", Namespace = "http://hl7.org/fhir")]
    public class Position : BackboneElement
    {

        /// <summary>
        /// Gets or sets the latitude
        /// </summary>
        [XmlElement("latitude")]
        [Description("The latitude of the physical position")]
        public FhirDecimal Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude
        /// </summary>
        [XmlElement("longitude")]
        [Description("The longitude of the physical location")]
        public FhirDecimal Longitude { get; set; }

        /// <summary>
        /// Gets or sets the altitude of the position
        /// </summary>
        [XmlElement("altitude")]
        public FhirDecimal Altitude { get; set; }

        /// <summary>
        /// Write the position as text
        /// </summary>
        internal override void WriteText(XmlWriter w)
        {
            w.WriteString(" lat=");
            this.Latitude?.WriteText(w);
            w.WriteString(" lng=");
            this.Longitude?.WriteText(w);
        }
    }
}
