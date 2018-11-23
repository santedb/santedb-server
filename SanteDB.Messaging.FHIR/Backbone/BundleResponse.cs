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
    /// Represents response transaction control information 
    /// </summary>
    [XmlType("Bundle.Response", Namespace = "http://hl7.org/fhir")]
    public class BundleResponse : BackboneElement
    {

        /// <summary>
        /// Gets or sets the status 
        /// </summary>
        [XmlElement("status")]
        [Description("Status return code for entry")]
        [FhirElement(MinOccurs = 1)]
        public FhirString Status { get; set; }

        /// <summary>
        /// Gets or sets the location of the entry
        /// </summary>
        [XmlElement("location")]
        [Description("The location, if the operation returns a location")]
        public FhirUri Location { get; set; }

        /// <summary>
        /// Gets or sets the etag of the entry
        /// </summary>
        [XmlElement("etag")]
        [Description("The etag for the resource if relevant")]
        public FhirString ETag { get; set; }

        /// <summary>
        /// Gets or sets the last modified time
        /// </summary>
        [XmlElement("lastModified")]
        [Description("The server's date time modified")]
        public FhirInstant LastModified { get; set; }
    }
}
