using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
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
    /// Represents the source information or reporter for the audit
    /// </summary>
    [XmlType("AuditEventSource", Namespace = "http://hl7.org/fhir")]
    public class AuditEventSource : BackboneElement
    {
        /// <summary>
        /// Gets or sets the site under which the data reported
        /// </summary>
        [XmlElement("site")]
        [Description("The site of the audit reporter")]
        public FhirString Site { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the reporter
        /// </summary>
        [XmlElement("identifier")]
        [Description("The identifier of the reporting source")]
        [FhirElement(MinOccurs = 1)]
        public FhirIdentifier Identifier { get; set; }

        /// <summary>
        /// Gets or sets the type(s) of source 
        /// </summary>
        [XmlElement("type")]
        [Description("Represents the type of source")]
        public List<FhirCoding> Type { get; set; }
    }
}
