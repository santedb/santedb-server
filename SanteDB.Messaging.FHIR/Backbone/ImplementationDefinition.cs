using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;
using System.ComponentModel;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Implementation definition
    /// </summary>
    [XmlType("Implementation", Namespace = "http://hl7.org/fhir")]
    public class ImplementationDefinition : BackboneElement
    {

        /// <summary>
        /// Gets or sets the description of the implementation
        /// </summary>
        [XmlElement("description")]
        [Description("Description of the specific instance")]
        public FhirString Description { get; set; }
        /// <summary>
        /// Gets or sets the base URL 
        /// </summary>
        [XmlElement("url")]
        [Description("Base URL for the instance")]
        public FhirUri Url { get; set; }

    }
}
