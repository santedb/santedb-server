/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: justin
 * Date: 2018-11-23
 */
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using System.ComponentModel;
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
