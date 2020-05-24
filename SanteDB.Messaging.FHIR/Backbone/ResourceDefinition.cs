/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using SanteDB.Messaging.FHIR.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{

    /// <summary>
    /// Resource versioning policy
    /// </summary>
    [XmlType("ResourceVersionPolicy", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/versioning-policy")]
    public enum ResourceVersionPolicy
    {
        [XmlEnum("no-version")]
        NonVersioned,
        [XmlEnum("versioned")]
        Versioned,
        [XmlEnum("versioned-update")]
        VersionedUpdate
    }

    /// <summary>
    /// Conditional delete status
    /// </summary>
    [XmlType("ConditionalDeleteStatus", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/conditional-delete-status")]
    public enum ConditionalDeleteStatus
    {
        /// <summary>
        /// Conditional delete is not supported by this server
        /// </summary>
        [XmlEnum("not-supported")]
        NotSupported,
        /// <summary>
        /// Deletion is supported on single object
        /// </summary>
        [XmlEnum("single")]
        Single, 
        /// <summary>
        /// Deletion is supported on multiple objects
        /// </summary>
        [XmlEnum("multiple")]
        Multiple
    }

    /// <summary>
    /// Conditional delete status
    /// </summary>
    [XmlType("ReferencePolicy", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/reference-policy")]
    public enum ReferencePolicy
    {
        [XmlEnum("literal")]
        Literal,
        [XmlEnum("logical")]
        Logical,
        [XmlEnum("resolves")]
        Resolves,
        [XmlEnum("enforced")]
        Enforced,
        [XmlEnum("local")]
        Local
    }

    /// <summary>
    /// Resource definition
    /// </summary>
    [XmlType("Resource", Namespace = "http://hl7.org/fhir")]
    public class ResourceDefinition : BackboneElement
    {

        /// <summary>
        /// Creates a new resource definition
        /// </summary>
        public ResourceDefinition()
        {
            this.Interaction = new List<InteractionDefinition>();
            this.SearchParams = new List<SearchParamDefinition>();
            this.SearchInclude = new List<FhirString>();
            this.SearchRevInclude = new List<FhirString>();
            this.ReferencePolicy = new List<FhirCode<Backbone.ReferencePolicy>>();
        }

        /// <summary>
        /// Gets or sets the type of resource
        /// </summary>
        [XmlElement("type")]
        [Description("Resource type")]
        [FhirElement(MinOccurs = 1)]
        public FhirCode<String> Type { get; set; }

        /// <summary>
        /// The profile reference
        /// </summary>
        [XmlElement("profile")]
        [Description("Resource profiles supported")]
        public Reference<Resources.StructureDefinition> Profile { get; set; }

        /// <summary>
        /// Gets or sets the operations supported
        /// </summary>
        [XmlElement("interaction")]
        [Description("Operations supported")]
        public List<InteractionDefinition> Interaction { get; set; }

        /// <summary>
        /// Gets or sets the versioning policy on the resource
        /// </summary>
        [XmlElement("versioning")]
        [Description("Versioning policy on the resource")]
        public FhirCode<ResourceVersionPolicy> Versioning { get; set; }

        /// <summary>
        /// True if history is supported
        /// </summary>
        [XmlElement("readHistory")]
        [Description("Whether vRead can return past versions")]
        public FhirBoolean ReadHistory { get; set; }

        /// <summary>
        /// Gets or sets whether update can create new identifiers
        /// </summary>
        [XmlElement("updateCreate")]
        [Description("If update can commit to a new identity")]
        public FhirBoolean UpdateCreate { get; set; }

        /// <summary>
        /// Gets or sets whether conditional creates
        /// </summary>
        [XmlElement("conditionalCreate")]
        [Description("If allows/uses conditional create")]
        public FhirBoolean ConditionalCreate { get; set; }

        /// <summary>
        /// Gets or sets whether condition update is allowed
        /// </summary>
        [XmlElement("conditionalUpdate")]
        [Description("If allows/uses condition update")]
        public FhirBoolean ConditionalUpdate { get; set; }

        /// <summary>
        /// Gets or sets the conditional delete status
        /// </summary>
        [XmlElement("conditionalDelete")]
        [Description("Conditional delete status")]
        public FhirCode<ConditionalDeleteStatus> ConditionalDelete { get; set; }

        /// <summary>
        /// Gets or sets the reference policy applied
        /// </summary>
        [XmlElement("referencePolicy")]
        [Description("Reference policy applied")]
        public List<FhirCode<ReferencePolicy>> ReferencePolicy { get; set; }

        /// <summary>
        /// Gets or sets _include value supported by server
        /// </summary>
        [XmlElement("searchInclude")]
        [Description("_include values supported by server")]
        public List<FhirString> SearchInclude { get; set; }

        /// <summary>
        /// Gets or sets revision include parameters supported
        /// </summary>
        [XmlElement("searchRevInclude")]
        [Description("_revinclude values supported by server")]
        public List<FhirString> SearchRevInclude { get; set; }

        /// <summary>
        /// Search parameters defined
        /// </summary>
        [XmlElement("searchParam")]
        [Description("Search parameters defined")]
        public List<SearchParamDefinition> SearchParams { get; set; }

        /// <summary>
        /// Write test
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {
            this.Type.WriteText(w);
            // Now profiles?
            if (this.Profile != null)
            {
                w.WriteStartElement("blockquote");
                w.WriteElementString("strong", "Profiles:");
                w.WriteStartElement("br");
                w.WriteEndElement();
                this.Profile.WriteText(w);
                w.WriteStartElement("br");
                w.WriteEndElement();
                w.WriteEndElement(); // blockquote
            }

            w.WriteStartElement("blockquote");
            w.WriteElementString("strong", "Search Parameters:");
            w.WriteStartElement("br");
            w.WriteEndElement();
            foreach (var itm in this.SearchParams)
            {
                w.WriteStartElement("a");
                w.WriteAttributeString("href", itm?.Definition?.Value?.ToString());
                itm.Name?.WriteText(w);
                w.WriteEndElement(); // a
                w.WriteStartElement("br");
                w.WriteEndElement();
            }
            w.WriteEndElement(); // blockquote
        }
    }
}
