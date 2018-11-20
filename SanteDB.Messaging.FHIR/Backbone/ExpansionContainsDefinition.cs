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
    /// Yet another way to represent a code selected in a value set, this time for the purpose of the expansion option
    /// </summary>
    [XmlType("ValueSet.Expansion.Contains", Namespace = "http://hl7.org/fhir")]
    public class ExpansionContainsDefinition : BackboneElement
    {
        /// <summary>
        /// Creates a new expansion contains definition
        /// </summary>
        public ExpansionContainsDefinition()
        {
            this.Contains = new List<ExpansionContainsDefinition>();
        }

        /// <summary>
        /// Gets or sets the system in which the expansion fits
        /// </summary>
        [XmlElement("system")]
        [Description("System value for the code")]
        public FhirUri System { get; set; }

        /// <summary>
        /// Gets or sets whether user can select entry
        /// </summary>
        [XmlElement("abstract")]
        [Description("If user cannot select this entry")]
        public FhirBoolean Abstract { get; set; }

        /// <summary>
        /// Gets or sets the version of the code system
        /// </summary>
        [XmlElement("version")]
        [Description("Version in which this code/display is defined")]
        public FhirString Version { get; set; }

        /// <summary>
        /// Gets or sets the code 
        /// </summary>
        [XmlElement("code")]
        [Description("Code- if blank this is not a selectable code")]
        public FhirCode<String> Code { get; set; }

        /// <summary>
        /// User display for the concept
        /// </summary>
        [XmlElement("display")]
        [Description("User display for the concept")]
        public FhirString Display { get; set; }

        /// <summary>
        /// Codes contained under the entry
        /// </summary>
        [XmlElement("contains")]
        [Description("Codes contained under this entry")]
        public List<ExpansionContainsDefinition> Contains { get; set; }

    }
}
