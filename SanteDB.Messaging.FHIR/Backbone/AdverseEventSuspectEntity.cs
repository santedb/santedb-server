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
    /// Adverse event suspected entity
    /// </summary>
    [XmlType("AdverseEventSuspectEntity", Namespace = "http://hl7.org/fhir")]
    public class AdverseEventSuspectEntity : BackboneElement
    {
        
        /// <summary>
        /// Gets or sets the instance of the substance which caused the event
        /// </summary>
        [XmlElement("instance")]
        [Description("The specific entity that cused the adverse event")]
        public Reference Instance { get; set; }
        
    }
}
