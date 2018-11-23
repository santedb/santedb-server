using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Resource base
    /// </summary>
    [XmlType("ResourceBase", Namespace = "http://hl7.org/fhir")]
    public class ResourceBase : FhirElement
    {
        
        /// <summary>
        /// ctor
        /// </summary>
        public ResourceBase()
        {
            this.Attributes = new List<ResourceAttributeBase>();
        }

        /// <summary>
        /// Gets or sets the internal identifier for the resource
        /// </summary>
        [XmlIgnore]
        public string Id { get; set; }

        /// <summary>
        /// Version identifier
        /// </summary>
        [XmlIgnore]
        public string VersionId
        {
            get { return this.Meta?.VersionId; }
            set
            {
                if (this.Meta == null) this.Meta = new ResourceMetadata();
                this.Meta.VersionId = value;
            }
        }

        /// <summary>
        /// Extended observations about the resource that can be used to tag the resource
        /// </summary>
        [XmlIgnore]
        public List<ResourceAttributeBase> Attributes { get; set; }

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        [XmlIgnore]
        public DateTime Timestamp
        {
            get { return this.Meta?.LastUpdated; }
            set
            {
                if (this.Meta == null) this.Meta = new ResourceMetadata();
                this.Meta.LastUpdated = value;
            }
        }

        /// <summary>
        /// Gets or sets the metadata
        /// </summary>
        [XmlElement("meta")]
        public ResourceMetadata Meta { get; set; }

    }
}
