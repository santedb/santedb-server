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
    /// Identifies a concept definition
    /// </summary>
    [XmlType("ValueSet.CodeSystem.Concept", Namespace = "http://hl7.org/fhir")]
    public class ConceptDefinition : BackboneElement
    {

        /// <summary>
        /// Concept designation
        /// </summary>
        public ConceptDefinition()
        {
            this.Designation = new List<ConceptDesignation>();
            this.Concept = new List<ConceptDefinition>();
        }

        /// <summary>
        /// Gets or sets the code
        /// </summary>
        [XmlElement("code")]
        [FhirElement(MinOccurs = 1)]
        public FhirCode<String> Code { get; set; }
        
        /// <summary>
        /// Gets or sets the abstract modifier
        /// </summary>
        [XmlElement("abstract")]
        public FhirBoolean Abstract { get; set; }

        /// <summary>
        /// Gets or sets the display name for the code
        /// </summary>
        [XmlElement("display")]
        public FhirString Display { get; set; }

        /// <summary>
        /// Gets or sets the definition
        /// </summary>
        [XmlElement("definition")]
        public FhirString Definition { get; set; }

        /// <summary>
        /// Gets or sets additional designations
        /// </summary>
        [XmlElement("designation")]
        public List<ConceptDesignation> Designation { get; set; }

        /// <summary>
        /// Gets or sets child concepts
        /// </summary>
        [XmlElement("concept")]
        public List<ConceptDefinition> Concept { get; set; }

        /// <summary>
        /// Write text
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {
            w.WriteStartElement("a");
            w.WriteAttributeString("href", String.Format("#{0}", this.Code));
            this.Code.WriteText(w);
            w.WriteEndElement();
        }
    }
}
