using SanteDB.Messaging.FHIR.DataTypes;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Resource base
    /// </summary>
    [XmlType("Metadata", Namespace = "http://hl7.org/fhir")]
    public  class ResourceMetadata
    {
        /// <summary>
        /// Resource metadata ctor
        /// </summary>
        public ResourceMetadata()
        {
            this.Security = new List<FhirCoding>();
            this.Tags = new List<FhirCoding>();
        }

        /// <summary>
        /// Version id
        /// </summary>
        [XmlElement("versionId")]
        public FhirString VersionId { get; set; }

        /// <summary>
        /// Last update time
        /// </summary>
        [XmlElement("lastUpdated")]
        public FhirDateTime LastUpdated { get; set; }

        /// <summary>
        /// Profile id
        /// </summary>
        [XmlElement("profile")]
        public FhirUri Profile { get; set; }

        /// <summary>
        /// Security tags
        /// </summary>
        [XmlElement("security")]
        public List<FhirCoding> Security { get; set; }

        /// <summary>
        /// Tags 
        /// </summary>
        [XmlElement("tag")]
        public List<FhirCoding> Tags { get; set; }
    }
}