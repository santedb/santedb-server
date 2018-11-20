using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using System.ComponentModel;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Identifies a concept set
    /// </summary>
    [XmlType("ValueSet.Compose.Include", Namespace = "http://hl7.org/fhir")]
    public class ComposeIncludeDefinition : BackboneElement
    {
        /// <summary>
        /// Concept set
        /// </summary>
        public ComposeIncludeDefinition()
        {
            this.Concept = new List<ConceptDefinition>();
        }

        /// <summary>
        /// Gets or sets the codification system from which codes are included
        /// </summary>
        [XmlElement("system")]
        [Description("The system the codes come from")]
        [FhirElement(MinOccurs = 1)]
        public FhirUri System { get; set; }

        /// <summary>
        /// Gets or sets the version of the code system
        /// </summary>
        [XmlElement("version")]
        [Description("Specific version of the code system referred to")]
        public FhirString Version { get; set; }

        /// <summary>
        /// Gets or sets the codes to be imported
        /// </summary>
        [XmlElement("concept")]
        [Description("Concepts defined in the system to be composed")]
        public List<ConceptDefinition> Concept { get; set; }

        /// <summary>
        /// Gets or sets a filter for composition
        /// </summary>
        [XmlElement("filter")]
        public List<ComposeFilterDefinition> Filter { get; set; }
    }
}
