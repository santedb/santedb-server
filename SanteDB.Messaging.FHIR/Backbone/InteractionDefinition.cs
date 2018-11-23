using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;
using System.ComponentModel;
using SanteDB.Messaging.FHIR.Attributes;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Search parameter type
    /// </summary>
    [XmlType("SearchParamType", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/type-restful-interaction")]
    public enum TypeRestfulInteraction
    {
        [XmlEnum("read")]
        Read,
        [XmlEnum("vread")]
        VersionRead,
        [XmlEnum("update")]
        Update,
        [XmlEnum("patch")]
        Patch,
        [XmlEnum("delete")]
        Delete,
        [XmlEnum("history-instance")]
        InstanceHistory,
        [XmlEnum("history-type")]
        ResourceHistory,
        [XmlEnum("create")]
        Create,
        [XmlEnum("search-type")]
        Search
    }
    /// <summary>
    /// Operation definition
    /// </summary>
    [XmlType("InteractionDefinition", Namespace = "http://hl7.org/fhir")]
    public class InteractionDefinition : BackboneElement
    {

        /// <summary>
        /// Type of operation
        /// </summary>
        [Description("Type of operation")]
        [XmlElement("code")]
        public FhirCode<TypeRestfulInteraction> Type { get; set; }

        /// <summary>
        /// Documentation related to the operation
        /// </summary>
        [Description("Documentation related to the operation")]
        [XmlElement("documentation")]
        public FhirString Documentation { get; set; }

    }
}
