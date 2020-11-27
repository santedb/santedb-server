/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{

    /// <summary>
    /// Identifies http verbs
    /// </summary>
    [XmlType("HTTPVerb", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/http-verb")]
    public enum HttpVerb
    {
        /// <summary>
        /// The GET verb is supported
        /// </summary>
        [XmlEnum("GET")]
        Get,
        /// <summary>
        /// The HTTP POST verb
        /// </summary>
        [XmlEnum("POST")]
        Post,
        /// <summary>
        /// The HTTP PUT verb
        /// </summary>
        [XmlEnum("PUT")]
        Put,
        /// <summary>
        /// The HTTP DELETE Verb
        /// </summary>
        [XmlEnum("DELETE")]
        Delete
    }

    /// <summary>
    /// Represents transaction information related to a bundle
    /// </summary>
    [XmlType("Bundle.Request", Namespace = "http://hl7.org/fhir")]
    public class BundleRequest : BackboneElement
    {

        /// <summary>
        /// Gets or sets the method used for the bundle request
        /// </summary>
        [XmlElement("method")]
        [Description("The HTTP verb used")]
        [FhirElement(MinOccurs = 1)]
        public FhirCode<HttpVerb> Method { get; set; }

        /// <summary>
        /// Gets or sets the url the request equivalent
        /// </summary>
        [XmlElement("url")]
        [Description("URL for HTTP equivalent of this entry")]
        [FhirElement(MinOccurs = 1)]
        public FhirUri Url { get; set; }

        /// <summary>
        /// Gets or sets a setting for cache currency
        /// </summary>
        [XmlElement("ifNoneMatch")]
        [Description("For managing cache currency")]
        public FhirString IfNoneMatch { get; set; }

        /// <summary>
        /// Gets or sets a setting for update contention
        /// </summary>
        [Description("For managing update contention")]
        [XmlElement("ifModifiedSince")]
        public FhirInstant IfModifiedSince { get; set; }

        /// <summary>
        /// Gets or sets a setting for update contention
        /// </summary>
        [Description("For managing update contention")]
        [XmlElement("ifMatch")]
        public FhirString IfMatch { get; set; }

        /// <summary>
        /// For conditional creates
        /// </summary>
        [Description("For conditional creates")]
        [XmlElement("ifNoneExist")]
        public FhirString IfNoneExist { get; set; }

    }
}
