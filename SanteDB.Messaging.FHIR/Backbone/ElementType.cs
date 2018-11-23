using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{

    [XmlType("AggregationMode", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/resource-aggregation-mode")]
    public enum AggregationMode
    {
        [XmlEnum("contained")]
        Contained,
        [XmlEnum("referenced")]
        Referenced,
        [XmlEnum("bundled")]
        Bundled
    }

    /// <summary>
    /// Data type and profile for an element
    /// </summary>
    [XmlType("ElementType", Namespace = "http://hl7.org/fhir")]
    public class ElementType : FhirElement
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ElementType()
        {
            this.Profile = new List<FhirUri>();
            this.Aggregation = new List<FhirCode<AggregationMode>>();
        }

        /// <summary>
        /// The datatype resource being profiles
        /// </summary>
        [XmlElement("code")]
        [Description("Name of datatype or resource")]
        [FhirElement(MinOccurs = 1, RemoteBinding = "http://hl7.org/fhir/ValueSet/defined-types")]
        public FhirCode<String> Code { get; set; }

        /// <summary>
        /// Profile to apply
        /// </summary>
        [XmlElement("profile")]
        [Description("Profile to apply")]
        public List<FhirUri> Profile { get; set; }

        /// <summary>
        /// Identifies the aggregation mode
        /// </summary>
        public List<FhirCode<AggregationMode>> Aggregation { get; set; }
    }
}