using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{

    /// <summary>
    /// Substance status codes
    /// </summary>
    [XmlType("SubstanceStatus", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/FHIRsubstanceStatus")]
    public enum SubstanceStatus
    {
        /// <summary>
        /// Substance is active
        /// </summary>
        [XmlEnum("active")]
        Active,
        /// <summary>
        /// Substance is inactive
        /// </summary>
        [XmlEnum("inactive")]
        Inactive,
        /// <summary>
        /// Substance is nullified
        /// </summary>
        [XmlEnum("entered-in-error")]
        Nullified
    }

    /// <summary>
    /// Represents a substance which is packaged or represents a type of substance
    /// </summary>
    [XmlType("Substance", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("Substance", Namespace = "http://hl7.org/fhir")]
    public class Substance : DomainResourceBase
    {

        /// <summary>
        /// Creates a new instance of the substance
        /// </summary>
        public Substance()
        {
            this.Identifier = new List<FhirIdentifier>();
            this.Instance = new List<SubstanceInstance>();
        }

        /// <summary>
        /// Gets or sets the identifier for the substance
        /// </summary>
        [XmlElement("identifier")]
        [Description("Unique identifier for the substance")]
        public List<FhirIdentifier> Identifier { get; set; }

        /// <summary>
        /// Gets or sets the functional status of the substance
        /// </summary>
        [XmlElement("status")]
        [Description("The status of the substance")]
        public FhirCode<SubstanceStatus> Status { get; set; }

        /// <summary>
        /// Gets or sets the type or category of stubstance represented
        /// </summary>
        [XmlElement("category")]
        [Description("The category/type of the substance represented")]
        public FhirCodeableConcept Category { get; set; }

        /// <summary>
        /// Gets or sets the coded representation of the substance
        /// </summary>
        [XmlElement("code")]
        [Description("What substance is represented")]
        public FhirCodeableConcept Code { get; set; }

        /// <summary>
        /// .Gets or sets the textual representation of the substance
        /// </summary>
        [XmlElement("description")]
        [Description("Textual description of the substance or comments")]
        public FhirString Description { get; set; }

        /// <summary>
        /// Gets or sets the instances which this substance represents
        /// </summary>
        [XmlElement("instance")]
        [Description("Describes a sepcificy package or container for this substance")]
        public List<SubstanceInstance> Instance { get; set; }


        /// <summary>
        /// Represent as a string
        /// </summary>
        public override string ToString()
        {
            return String.Format("[Substance] {0}", this.Code?.GetPrimaryCode()?.Display);
        }

    }
}
