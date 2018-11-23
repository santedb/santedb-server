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
    /// Represents an instance of a substance specifically for medications
    /// </summary>
    [XmlType("Medication", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("Medication", Namespace = "http://hl7.org/fhir")]
    public class Medication : DomainResourceBase
    {
        /// <summary>
        /// Gets or sets the code of the medication
        /// </summary>
        [XmlElement("code")]
        [Description("Codes that identify the medication")]
        [FhirElement(RemoteBinding = "http://hl7.org/fhir/ValueSet/medication-codes")]
        public FhirCodeableConcept Code { get; set; }

        /// <summary>
        /// Gets or sets the status of the medication
        /// </summary>
        [XmlElement("status")]
        [Description("Formal status of the medication entry")]
        [FhirElement(RemoteBinding = "http://hl7.org/fhir/ValueSet/medication-status")]
        public FhirCode<SubstanceStatus> Status { get; set; }

        /// <summary>
        /// Gets or sets the indicator which dictates if the medication is brand name
        /// </summary>
        [XmlElement("isBrand")]
        [Description("True if the medication is a brand")]
        public FhirBoolean IsBrand { get; set; }

        /// <summary>
        /// Over the counter
        /// </summary>
        [XmlElement("isOverTheCounter")]
        [Description("True if the medication is an OTC medication")]
        public FhirBoolean IsOverTheCounter { get; set; }

        /// <summary>
        /// Gets or sets the manufacturer
        /// </summary>
        [XmlElement("manufacturer")]
        [Description("Manufacterer of the item")]
        public Reference<Organization> Manufacturer { get; set; }

        /// <summary>
        /// Gets or sets the form code
        /// </summary>
        [XmlElement("form")]
        [Description("Identifies the form of the medication")]
        [FhirElement(RemoteBinding = "http://hl7.org/fhir/ValueSet/medication-form-codes")]
        public FhirCodeableConcept Form { get; set; }

        /// <summary>
        /// Packaging
        /// </summary>
        [XmlElement("package")]
        [Description("Details about the package of the medication")]
        public MedicationPackage Package { get; set; }

        /// <summary>
        /// Gets or sets a picture of the medication
        /// </summary>
        [XmlElement("image")]
        [Description("A picture of the medication")]
        public Attachment Image { get; set; }

        /// <summary>
        /// Represent as a string
        /// </summary>
        public override string ToString()
        {
            return String.Format("[Medication] {0}", this.Code?.GetPrimaryCode()?.Display);
        }
    }
}
