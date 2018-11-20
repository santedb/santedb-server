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
    /// Represents an expanded value set
    /// </summary>
    [XmlType("ValueSet.Expansion", Namespace = "http://hl7.org/fhir")]
    public class ExpansionDefinition : BackboneElement
    {

        /// <summary>
        /// Expansion definition
        /// </summary>
        public ExpansionDefinition()
        {
            this.Contains = new List<ExpansionContainsDefinition>();
        }

        /// <summary>
        /// Gets or sets the identifier for the expansion
        /// </summary>
        [XmlElement("identifier")]
        [Description("Uniquely identifies this expansion")]
        [FhirElement(MinOccurs = 1)]
        public FhirUri Identifier { get; set; }

        /// <summary>
        /// Gets or sets the time the valueset was expanded
        /// </summary>
        [XmlElement("timestamp")]
        [Description("Time ValueSet expansion happened")]
        [FhirElement(MinOccurs = 1)]
        public FhirDateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the total number of codes in the expansion
        /// </summary>
        [XmlElement("total")]
        [Description("Total number of codes in the expansion")]
        public FhirInt Total { get; set; }

        /// <summary>
        /// Gets or sets the offset at which this resource starts
        /// </summary>
        [XmlElement("offset")]
        [Description("Offset at which this resource starts")]
        public FhirInt Offset { get; set; }

        /// <summary>
        /// Gets or sets the concepts contained in the expansion
        /// </summary>
        [XmlElement("contains")]
        [Description("Codes contained in the value set expansion")]
        public List<ExpansionContainsDefinition> Contains { get; set; }
    }
}
