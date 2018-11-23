using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Attributes;
using System.ComponentModel;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Operation outcome
    /// </summary>
    [XmlType("OperationOutcome", Namespace="http://hl7.org/fhir")]
    [XmlRoot("OperationOutcome", Namespace = "http://hl7.org/fhir")]
    public class OperationOutcome : DomainResourceBase
    {

        /// <summary>
        /// Namespace Declarations
        /// </summary>
        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces Namespaces { get { return this.m_namespaces; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public OperationOutcome()
        {
            this.Issue = new List<Issue>();
        }

        /// <summary>
        /// Gets or sets a list of issues 
        /// </summary>
        [XmlElement("issue")]
        [Description("A list of issues related to the operation")]
        [FhirElement(MinOccurs = 1)]
        public List<Issue> Issue { get; set; }

    }
}
