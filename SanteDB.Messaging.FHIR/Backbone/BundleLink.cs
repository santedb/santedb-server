using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Represents a link used within bundles
    /// </summary>
    [XmlType("Bundle.Link", Namespace = "http://hl7.org/fhir")]
    public class BundleLink : BackboneElement
    {

        public BundleLink()
        {

        }
        /// <summary>
        /// Creates a new bundle link
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="relation"></param>
        public BundleLink(Uri uri, String relation)
        {
            this.Url = uri;
            this.Relation = relation;
        }
        /// <summary>
        /// Gets or sets the relationship the link has to the bundle
        /// </summary>
        [FhirElement(MinOccurs = 1)]
        [Description("http://www.iana.org/assignments/link-relations/link-relations.xhtml")]
        [XmlElement("relation")]
        public FhirString Relation { get; set; }

        /// <summary>
        /// Gets or sets the url of the link
        /// </summary>
        [FhirElement(MinOccurs = 1)]
        [XmlElement("url")]
        [Description("Reference detailes for the link")]
        public FhirUri Url { get; set; }

        /// <summary>
        /// Write text
        /// </summary>
        internal override void WriteText(XmlWriter w)
        {
            w.WriteStartElement("a");
            w.WriteAttributeString("href", this.Url?.Value?.ToString());
            this.Relation?.WriteText(w);
            w.WriteEndElement(); // a
        }
    }
}
