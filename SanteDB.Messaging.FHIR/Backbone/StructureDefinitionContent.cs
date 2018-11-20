using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Structure definition content model
    /// </summary>
    [XmlType("StructureDefinitionContent", Namespace = "http://hl7.org/fhir")]
    public class StructureDefinitionContent : BackboneElement
    {
        /// <summary>
        /// Structure definition
        /// </summary>
        public StructureDefinitionContent()
        {
            this.Element = new List<ElementDefinition>();
        }

        /// <summary>
        /// Gets or sets the element definition
        /// </summary>
        [XmlElement("element")]
        public List<ElementDefinition> Element { get; set; }
    }
}
