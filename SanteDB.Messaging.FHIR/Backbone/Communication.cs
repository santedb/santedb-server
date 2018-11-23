using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;
using System.ComponentModel;
using SanteDB.Messaging.FHIR.Attributes;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Represents a language of communication
    /// </summary>
    [XmlType("Patient.Communication", Namespace = "http://hl7.org/fhir")]
    public class Communication : BackboneElement
    {
        /// <summary>
        /// Gets or sets the language code
        /// </summary>
        [XmlElement("language")]
        [Description("Language with optional region")]
        [FhirElement(MinOccurs = 1, RemoteBinding = "http://tools.ietf.org/html/bcp47")]
        public FhirCodeableConcept Value { get; set; }
        /// <summary>
        /// Gets or sets the preference indicator
        /// </summary>
        [XmlElement("preferred")]
        [Description("Language preference indicator")]
        public FhirBoolean Preferred { get; set; }
    }
}
