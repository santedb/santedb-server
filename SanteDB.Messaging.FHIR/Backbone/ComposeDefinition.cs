using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.DataTypes;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Composed from other systems
    /// </summary>
    [XmlType("ValueSet.Compose", Namespace = "http://hl7.org/fhir")]
    public class ComposeDefinition : BackboneElement
    {

        /// <summary>
        /// Compse a definition
        /// </summary>
        public ComposeDefinition()
        {
            this.Import = new List<FhirUri>();
            this.Include = new List<ComposeIncludeDefinition>();
            this.Exclude = new List<ComposeIncludeDefinition>();
        }

        /// <summary>
        /// The uri of an import
        /// </summary>
        [XmlElement("import")]
        public List<FhirUri> Import { get; set; }

        /// <summary>
        /// Included concepts
        /// </summary>
        [XmlElement("include")]
        public List<ComposeIncludeDefinition> Include { get; set; }

        /// <summary>
        /// Gets or sets the list of items to exclude
        /// </summary>
        [XmlElement("exclude")]
        public List<ComposeIncludeDefinition> Exclude { get; set; }
    }
}
