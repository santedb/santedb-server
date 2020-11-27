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
using System;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Represents a link used within bundles
    /// </summary>
    [XmlType("Bundle.Link", Namespace = "http://hl7.org/fhir")]
    public class BundleLink : BackboneElement
    {

        /// <summary>
        /// Represents a bundle link
        /// </summary>
        public BundleLink()
        {

        }

        /// <summary>
        /// Creates a new bundle link
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="relation"></param>
        public BundleLink(Uri uri, String relation)
        {
            this.Url = uri;
            this.Relation = relation;
        }
        /// <summary>
        /// Gets or sets the relationship the link has to the bundle
        /// </summary>
        [FhirElement(MinOccurs = 1)]
        [Description("http://www.iana.org/assignments/link-relations/link-relations.xhtml")]
        [XmlElement("relation")]
        public FhirString Relation { get; set; }

        /// <summary>
        /// Gets or sets the url of the link
        /// </summary>
        [FhirElement(MinOccurs = 1)]
        [XmlElement("url")]
        [Description("Reference detailes for the link")]
        public FhirUri Url { get; set; }

        /// <summary>
        /// Write text
        /// </summary>
        internal override void WriteText(XmlWriter w)
        {
            w.WriteStartElement("a");
            w.WriteAttributeString("href", this.Url?.Value?.ToString());
            this.Relation?.WriteText(w);
            w.WriteEndElement(); // a
        }
    }
}
