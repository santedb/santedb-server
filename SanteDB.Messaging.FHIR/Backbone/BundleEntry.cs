using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources;
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
    /// Represents a bundle entry
    /// </summary>
    [XmlType("Bundle.Entry", Namespace = "http://hl7.org/fhir")]
    public class BundleEntry : BackboneElement
    {

        /// <summary>
        /// Creates a new bundle entry
        /// </summary>
        public BundleEntry()
        {
            this.Link = new List<BundleLink>();
        }

        /// <summary>
        /// Gets or sets links to the entry
        /// </summary>
        [XmlElement("link")]
        [Description("Links related to this entry")]
        public List<BundleLink> Link { get; set; }

        /// <summary>
        /// Gets or sets the URL of the resource
        /// </summary>
        [XmlElement("fullUrl")]
        [Description("Absolute URL for the resource")]
        public FhirUri FullUrl { get; set; }

        /// <summary>
        /// Gets or sets the resource content
        /// </summary>
        [XmlElement("resource")]
        [Description("The resource in the bundle")]
        public BundleResrouce Resource { get; set; }

        /// <summary>
        /// Gets or sets the search related information
        /// </summary>
        [XmlElement("search")]
        [Description("Search related information")]
        public BundleSearch Search { get; set; }

        /// <summary>
        /// Gets or sets the request information
        /// </summary>
        [XmlElement("request")]
        [Description("Transaction related information")]
        public BundleRequest Request { get; set; }

        /// <summary>
        /// Gets or sets the response transaction information
        /// </summary>
        [XmlElement("response")]
        [Description("response")]
        public BundleResponse Response { get; set; }

        /// <summary>
        /// Write text
        /// </summary>
        internal override void WriteText(XmlWriter w)
        {
            w.WriteStartElement("h2");
            w.WriteAttributeString("class", "bundle_Entry_Title");
            w.WriteStartElement("a");
            w.WriteAttributeString("href", this.FullUrl.Value.ToString());
            w.WriteString(this.FullUrl?.Value?.ToString());
            w.WriteEndElement(); // a
            w.WriteEndElement(); // h2

            if (this.Resource != null)
                this.Resource.WriteText(w);
        }
    }
}
