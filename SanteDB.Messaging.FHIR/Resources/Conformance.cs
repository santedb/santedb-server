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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.DataTypes;
using System.ComponentModel;
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.Backbone;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Conformance resource status
    /// </summary>
    [XmlType("UnknownContentCode", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/unknown-content-code")]
    public enum UnknownContentCode
    {
        [XmlEnum("no")]
        None,
        [XmlEnum("elements")]
        Elements,
        [XmlEnum("extensions")]
        Extensions,
        [XmlEnum("both")]
        Both
    }

    /// <summary>
    /// Conformance resource status
    /// </summary>
    [XmlType("ConformanceResourceStatus", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/publication-status")]
    public enum PublicationStatus
    {
        [XmlEnum("draft")]
        Draft,
        [XmlEnum("active")]
        Active,
        [XmlEnum("retired")]
        Retired
    }

    /// <summary>
    /// Conformance statement kind
    /// </summary>
    [XmlType("ConformanceStatementKind", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/capability-statement-kind")]
    public enum CapabilityStatementKind
    {
        [XmlEnum("instance")]
        Instance,
        [XmlEnum("capability")]
        Capability,
        [XmlEnum("requirements")]
        Requirements
    }

    /// <summary>
    /// Conformance resource
    /// </summary>
    [XmlType("CapabilityStatement", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("CapabilityStatement", Namespace = "http://hl7.org/fhir")]
    public class Conformance : DomainResourceBase
    {

        /// <summary>
        /// Creates a new instance of the conformance class
        /// </summary>
        public Conformance()
        {
            this.Rest = new List<RestDefinition>();
            this.Format = new List<FhirCode<string>>();
            this.Contact = new List<ContactDetail>();
            this.Profile = new List<Reference<Resources.StructureDefinition>>();
        }

        /// <summary>
        /// Logical identifier to this resource
        /// </summary>
        [Description("Logical uri to refernece this statement")]
        [XmlElement("url")]
        public FhirUri Url { get; set; }

        /// <summary>
        /// Logical id for this version of the statement
        /// </summary>
        [Description("Logical id for the version of this statement")]
        [XmlElement("version")]
        public FhirString Version { get; set; }

        /// <summary>
        /// Gets or sets the name of the statement
        /// </summary>
        [XmlElement("name")]
        [Description("Informal name for this statement")]
        public FhirString Name { get; set; }

        /// <summary>
        /// Gets or sets the title of the conformance statement
        /// </summary>
        [XmlElement("title")]
        [Description("The human friendly title for the statement")]
        public FhirString Title { get; set; }

        /// <summary>
        /// The status of the conformance statement
        /// </summary>
        [Description("Status of the conformance statement")]
        [XmlElement("status")]
        [FhirElement(MinOccurs = 1)]
        public FhirCode<PublicationStatus> Status { get; set; }
        
        /// <summary>
        /// True if the conformance statement is experimental
        /// </summary>
        [Description("If for testing purposes, not real useage")]
        [XmlElement("experimental")]
        public FhirBoolean Experimental { get; set; }

        /// <summary>
        /// The publishing organization
        /// </summary>
        [Description("Publishing organization")]
        [XmlElement("publisher")]
        public FhirString Publisher { get; set; }

        /// <summary>
        /// Date the spec was published
        /// </summary>
        [Description("Date of publication")]
        [XmlElement("date")]
        [FhirElement(MinOccurs = 1)]
        public FhirDateTime Date { get; set; }

        /// <summary>
        /// Gets or sets contact information for the publisher
        /// </summary>
        [XmlElement("contact")]
        [Description("Contact details of the publisher")]
        public List<ContactDetail> Contact { get; set; }

        /// <summary>
        /// Description of the conformance statement
        /// </summary>
        [Description("Human description of the conformance statement")]
        [XmlElement("description")]
        public FhirString Description { get; set; }
       
        /// <summary>
        /// Gets or sets copyright information related to the conformance statement
        /// </summary>
        [XmlElement("copyright")]
        [Description("Use and/or publishing restrictions")]
        public FhirString Copyright { get; set; }

        /// <summary>
        /// Gets or sets the kind of conformance statement
        /// </summary>
        [XmlElement("kind")]
        [Description("Kind of conformance statement represented")]
        public FhirCode<CapabilityStatementKind> Kind { get; set; }

        /// <summary>
        /// Describes the software that is covered by this conformance statement
        /// </summary>
        [XmlElement("software")]
        [Description("Describes the software that is covered by this conformance statement")]
        public SoftwareDefinition Software { get; set; }

        /// <summary>
        /// Describes the specified instance
        /// </summary>
        [Description("Describes the specific instnace")]
        [XmlElement("implementation")]
        public ImplementationDefinition Implementation { get; set; }

        /// <summary>
        /// Gets or sets the FHIR version
        /// </summary>
        [Description("The FHIR version")]
        [XmlElement("fhirVersion")]
        [FhirElement(MinOccurs = 1)]
        public FhirString FhirVersion { get; set; }

        /// <summary>
        /// True if application accepts unknown elements
        /// </summary>
        [Description("If application accepts unknown elements")]
        [XmlElement("acceptUnknown")]
        [FhirElement(MinOccurs = 1)]
        public FhirCode<UnknownContentCode> AcceptUnknown { get; set; }

        /// <summary>
        /// Formats supported
        /// </summary>
        [Description("Formats supported")]
        [XmlElement("format")]
        public List<FhirCode<String>> Format { get; set; }

        /// <summary>
        /// Formats supported
        /// </summary>
        [Description("Patch formats supported")]
        [XmlElement("patchFormat")]
        public List<FhirCode<String>> PatchFormat { get; set; }

        /// <summary>
        /// Profiles supported
        /// </summary>
        [XmlElement("profile")]
        [Description("Profiles for use cases supported")]
        public List<Reference<StructureDefinition>> Profile { get; set; }

        /// <summary>
        /// Endpoint if restful
        /// </summary>
        [XmlElement("rest")]
        [Description("Endpoint if restful")]
        public List<RestDefinition> Rest { get; set; }

        /// <summary>
        /// Write text for the resource
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {
            w.WriteStartElement("div");
            w.WriteAttributeString("class", "h1");
            w.WriteString(String.Format("{0} - Conformance", this.Title));
            w.WriteEndElement(); // div

            w.WriteStartElement("table");
            w.WriteAttributeString("border", "1");
            w.WriteStartElement("caption");
            this.Name.WriteText(w);
            w.WriteEndElement(); // caption

            // Elements
            w.WriteStartElement("tbody");
            base.WriteTableRows(w, "Publisher", this.Publisher);
            base.WriteTableRows(w, "Published On", this.Date); 
            base.WriteTableRows(w, "Description", this.Description);
            base.WriteTableRows(w, "Status", this.Status);
            base.WriteTableRows(w, "Software Name", this.Software.Name);
            base.WriteTableRows(w, "Software Version", this.Software.Version);
            base.WriteTableRows(w, "Software Release Date", this.Software.ReleaseDate);
            base.WriteTableRows(w, "Base URI", this.Implementation.Url);
            base.WriteTableRows(w, "FHIR Version", this.FhirVersion);
            base.WriteTableRows(w, "Accepted Formats", this.Format.ToArray());
            base.WriteTableRows(w, "RESTful Implementations", this.Rest.ToArray());
            w.WriteEndElement();
            w.WriteEndElement(); // table

        }
    }
}
