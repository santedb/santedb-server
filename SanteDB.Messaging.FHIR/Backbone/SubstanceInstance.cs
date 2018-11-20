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
    /// Represents a particular instance of a substance
    /// </summary>
    [XmlType("SubstanceInstanec", Namespace = "http://hl7.org/fhir")]
    public class SubstanceInstance : BackboneElement
    {

        /// <summary>
        /// Gets or sets the identifier of the substance instance
        /// </summary>
        [XmlElement("identifier")]
        [Description("Identifier for the package or container")]
        public FhirIdentifier Identifier { get; set; }

        /// <summary>
        /// Gets or sets the expiration time
        /// </summary>
        [XmlElement("expiry")]
        [Description("When no longer valid to use this instance")]
        public FhirDateTime Expiry { get; set; }

        /// <summary>
        /// Gets or sets the quantity
        /// </summary>
        [XmlElement("quantity")]
        [Description("Amount of substance in the package")]
        public FhirQuantity Quantity { get; set; }

    }
}
