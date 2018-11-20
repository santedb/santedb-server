using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Attributes;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Value set definition
    /// </summary>
    [XmlType("ValueSet.CodeSystem", Namespace = "http://hl7.org/fhir")]
    public class CodeSystemDefinition : BackboneElement
    {

        /// <summary>
        /// Value set definition
        /// </summary>
        public CodeSystemDefinition()
        {
            this.Concept = new List<ConceptDefinition>();
        }

        /// <summary>
        /// The code system which is defined by this value set
        /// </summary>
        [XmlElement("system")]
        [FhirElement(MinOccurs = 1)]
        public FhirUri System { get; set; }

        /// <summary>
        /// Gets or sets the version information
        /// </summary>
        [XmlElement("version")]
        public FhirString Version { get; set; }

        /// <summary>
        /// Indicates whether the code system is case sensitive
        /// </summary>
        [XmlElement("caseSensitive")]
        public FhirBoolean CaseSensitive { get; set; }

        /// <summary>
        /// Gets or sets the list of concepts
        /// </summary>
        [XmlElement("concept")]
        public List<ConceptDefinition> Concept { get; set; }
    }
}
