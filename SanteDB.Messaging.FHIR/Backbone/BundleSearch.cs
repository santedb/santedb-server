using SanteDB.Messaging.FHIR.Attributes;
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
    /// Search modes for bundle search
    /// </summary>
    [XmlType("SearchEntryMode", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/search-entry-mode")]
    public enum SearchEntryMode
    {
        [XmlEnum("match")]
        Match,
        [XmlEnum("include")]
        Include,
        [XmlEnum("outcome")]
        Outcome
    }

    /// <summary>
    /// Identifies a backbone element for the bundle search
    /// </summary>
    [XmlType("Bundle.Search", Namespace = "http://hl7.org/fhir")]
    public class BundleSearch : BackboneElement
    {

        /// <summary>
        /// Gets or sets the mode of the search 
        /// </summary>
        [XmlElement("mode")]
        [Description("Why this is in the result set")]
        public FhirCode<SearchEntryMode> Mode { get; set; }

        /// <summary>
        /// Gets or sets a ranking of the search result
        /// </summary>
        [XmlElement("score")]
        [Description("Search ranking between 0 and 1")]
        public FhirDecimal Score { get; set; }
    }
}
