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
    /// Identifies http verbs
    /// </summary>
    [XmlType("HTTPVerb", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/http-verb")]
    public enum HttpVerb
    {
        [XmlEnum("GET")]
        Get,
        [XmlEnum("POST")]
        Post,
        [XmlEnum("PUT")]
        Put,
        [XmlEnum("DELETE")]
        Delete
    }

    /// <summary>
    /// Represents transaction information related to a bundle
    /// </summary>
    [XmlType("Bundle.Request", Namespace = "http://hl7.org/fhir")]
    public class BundleRequest : BackboneElement
    {

        /// <summary>
        /// Gets or sets the method used for the bundle request
        /// </summary>
        [XmlElement("method")]
        [Description("The HTTP verb used")]
        [FhirElement(MinOccurs = 1)]
        public FhirCode<HttpVerb> Method { get; set; }

        /// <summary>
        /// Gets or sets the url the request equivalent
        /// </summary>
        [XmlElement("url")]
        [Description("URL for HTTP equivalent of this entry")]
        [FhirElement(MinOccurs = 1)]
        public FhirUri Url { get; set; }

        /// <summary>
        /// Gets or sets a setting for cache currency
        /// </summary>
        [XmlElement("ifNoneMatch")]
        [Description("For managing cache currency")]
        public FhirString IfNoneMatch { get; set; }

        /// <summary>
        /// Gets or sets a setting for update contention
        /// </summary>
        [Description("For managing update contention")]
        [XmlElement("ifModifiedSince")]
        public FhirInstant IfModifiedSince { get; set; }

        /// <summary>
        /// Gets or sets a setting for update contention
        /// </summary>
        [Description("For managing update contention")]
        [XmlElement("ifMatch")]
        public FhirString IfMatch { get; set; }

        /// <summary>
        /// For conditional creates
        /// </summary>
        [Description("For conditional creates")]
        [XmlElement("ifNoneExist")]
        public FhirString IfNoneExist { get; set; }

    }
}
