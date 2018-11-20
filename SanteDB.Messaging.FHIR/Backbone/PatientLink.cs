using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources;
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
    /// Patient Link type
    /// </summary>
    [XmlType("LinkType", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/link-type")]
    public enum PatientLinkType
    {
        [XmlEnum("replace")]
        Replace,
        [XmlEnum("refer")]
        Refer,
        [XmlEnum("seealso")]
        SeeAlso
    }

    /// <summary>
    /// Represents a link between two patients
    /// </summary>
    [XmlType("Patient.Link", Namespace = "http://hl7.org/fhir")]
    public class PatientLink : BackboneElement
    {

        /// <summary>
        /// Gets or sets the other patient
        /// </summary>
        [XmlElement("other")]
        [Description("The other patient resource the link refers to")]
        public Reference<Patient> Other { get; set; }

        /// <summary>
        /// Gets or sets the type of link
        /// </summary>
        [XmlElement("type")]
        [Description("Type of link")]
        public FhirCode<PatientLinkType> Type { get; set; }
    }
}
