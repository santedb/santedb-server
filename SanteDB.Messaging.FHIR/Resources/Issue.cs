using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;
using System.ComponentModel;
using SanteDB.Messaging.FHIR.Attributes;

namespace SanteDB.Messaging.FHIR.Resources
{

    /// <summary>
    /// Issue severity
    /// </summary>
    [XmlType("IssueSeverity", Namespace = "http://hl7.org/fhir")]
    public enum IssueSeverity
    {
        [XmlEnum("fatal")]
        Fatal,
        [XmlEnum("error")]
        Error,
        [XmlEnum("warning")]
        Warning,
        [XmlEnum("information")]
        Information
    }

    /// <summary>
    /// Represents an issue detail
    /// </summary>
    [XmlType("Issue", Namespace = "http://hl7.org/fhir")]
    public class Issue : FhirElement
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public Issue()
        {
            this.Location = new List<FhirString>();
        }

        /// <summary>
        /// Gets or sets the severity
        /// </summary>
        [XmlElement("severity")]
        [Description("Identifies the severity of operation")]
        [FhirElement(MinOccurs = 1, RemoteBinding = "http://hl7.org/fhir/issue-severity")]
        public FhirCode<IssueSeverity> Severity { get; set; }

        /// <summary>
        /// Gets or sets the type of error
        /// </summary>
        [XmlElement("code")]
        [Description("Identifies the type of issue detected")]
        [FhirElement(RemoteBinding = "http://hl7.org/fhir/issue-type")]
        public FhirCoding Code { get; set; }

        /// <summary>
        /// Gets or sets the details of the issue
        /// </summary>
        [XmlElement("diagnostics")]
        [Description("Additional description of the issue")]
        public FhirString Diagnostics { get; set; }

        /// <summary>
        /// Gets or sets the location
        /// </summary>
        [XmlElement("location")]
        [Description("XPath of the element(s) related to the issue")]
        public List<FhirString> Location { get; set; }
    }
}
