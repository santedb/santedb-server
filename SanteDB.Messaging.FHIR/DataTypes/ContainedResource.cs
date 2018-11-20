using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.Resources;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// A list of contained resource
    /// </summary>
    [XmlType("ContainedResource", Namespace = "http://hl7.org/fhir")]
    [Serializable]
    public class ContainedResource : FhirElement
    {
        /// <summary>
        /// The contained resource collection
        /// </summary>
        public ContainedResource()
        {
        }

        /// <summary>
        /// Items within the collection
        /// </summary>
        [XmlElement(ElementName = "Patient", Type = typeof(Patient))]
        [XmlElement(ElementName = "Organization", Type = typeof(Organization))]
        [XmlElement(ElementName = "Picture", Type = typeof(Picture))]
        [XmlElement(ElementName = "Practictioner", Type = typeof(Practitioner))]
        [XmlElement(ElementName = "OperationOutcome", Type = typeof(OperationOutcome))]
        [XmlElement(ElementName = "ValueSet", Type = typeof(ValueSet))]
        [XmlElement(ElementName = "Profile", Type = typeof(StructureDefinition))]
        [XmlElement(ElementName = "Conformance", Type = typeof(Conformance))]
        [XmlElement(ElementName = "RelatedPerson", Type = typeof(RelatedPerson))]
        public DomainResourceBase Item { get; set; }

        /// <summary>
        /// Write the item text
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {
            this.Item.WriteText(w);
        }
    }
}
