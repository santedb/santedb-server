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
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Value set definition
    /// </summary>
    [XmlType("ValueSet", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("ValueSet", Namespace = "http://hl7.org/fhir")]
    public class ValueSet : DomainResourceBase
    {

        /// <summary>
        /// Creates a new value set
        /// </summary>
        public ValueSet()
        {
            this.Contact = new List<ContactDetail>();
        }

        /// <summary>
        /// Gets or sets the globally unique identifier for the value set
        /// </summary>
        [XmlElement("url")]
        [Description("Globally unique logical identifier for the value set")]
        public FhirUri Url { get; set; }

        /// <summary>
        /// Identifier of the value set
        /// </summary>
        [XmlElement("identifier")]
        [Description("Additional identifier for the value set")]
        public FhirString Identifier { get; set; }

        /// <summary>
        /// Version of the value set
        /// </summary>
        [XmlElement("version")]
        [Description("Logical identifier for this version of the value set")]
        public FhirString Version { get; set; }

        /// <summary>
        /// Name of the value set
        /// </summary>
        [XmlElement("name")]
        [Description("Infomral name for this value set")]
        public FhirString Name { get; set; }

        /// <summary>
        /// Gets or sets the status of the conformance element
        /// </summary>
        [XmlElement("status")]
        [Description("Status of the value set")]
        [FhirElement(MinOccurs = 1)]
        public FhirCode<PublicationStatus> Status { get; set; }

        /// <summary>
        /// Gets or sets an indicator describing whether the value set is for experimental use
        /// </summary>
        [XmlElement("experimental")]
        [Description("If for testing purposes not real usage")]
        public FhirBoolean Experimental { get; set; }

        /// <summary>
        /// Publisher of the value set
        /// </summary>
        [XmlElement("publisher")]
        [Description("Name of the publisher of this valueset")]
        public FhirString Publisher { get; set; }

        /// <summary>
        /// Gets or sets contact details for the publisher
        /// </summary>
        [XmlElement("contact")]
        [Description("Contact details of the publisher")]
        public List<ContactDetail> Contact { get; set; }

        /// <summary>
        /// The date of publication
        /// </summary>
        [XmlElement("date")]
        [Description("Date for given status")]
        public FhirDateTime Date { get; set; }

        /// <summary>
        /// The date all content is fixed
        /// </summary>
        [XmlElement("lockedDate")]
        [Description("Fixed date for all reference code systems and value sets")]
        public FhirDate LockedDate { get; set; }

        /// <summary>
        /// Description of the value set
        /// </summary>
        [XmlElement("description")]
        [Description("Human language description of the value set")]
        public FhirString Description { get; set; }

        /// <summary>
        /// Gets or sets the use under which the context can be used
        /// </summary>
        [XmlElement("useContext")]
        [Description("Content intends to support these contexts")]
        public FhirCodeableConcept UseContext { get; set; }

        /// <summary>
        /// Gets or sets the immutable indicator
        /// </summary>
        [XmlElement("immutable")]
        [Description("Indicates whether or not any change to the content logical definition may occur")]
        public FhirBoolean Immutable { get; set; }

        /// <summary>
        /// Gets or sets why the value set is needed
        /// </summary>
        [XmlElement("requirements")]
        [Description("Why needed")]
        public FhirString Requirements { get; set; }

        /// <summary>
        /// Gets or sets copyright information
        /// </summary>
        [XmlElement("copyright")]
        [Description("Use and/or publishing restrictions")]
        public FhirString Copyright { get; set; }

        /// <summary>
        /// Gets or sets the extensibility 
        /// </summary>
        [XmlElement("extensible")]
        [Description("Whether this is intended to be used with an extensible binding")]
        public FhirBoolean Extensible { get; set; }

        /// <summary>
        /// Gets or sets the definition
        /// </summary>
        [XmlElement("codeSystem")]
        [Description("An inline code system which is part of this value set")]
        public CodeSystemDefinition Define { get; set; }

        /// <summary>
        /// Compse a definitoin
        /// </summary>
        [XmlElement("compose")]
        public ComposeDefinition Compose { get; set; }

        /// <summary>
        /// Gets or sets the expanded value set definition
        /// </summary>
        [XmlElement("expansion")]
        public ExpansionDefinition Expansion { get; set; }

        /// <summary>
        /// Write text
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {

            w.WriteStartElement("table");
            w.WriteAttributeString("border", "1");
            w.WriteStartElement("caption");
            this.Name.WriteText(w);
            w.WriteString("(");

            if(this.Identifier != null)
                this.Identifier.WriteText(w);
            w.WriteString(") - ");

            if (this.Define != null)
                w.WriteString("Defines");
            else
                w.WriteString("Composed of");

            w.WriteEndElement();// caption
            w.WriteStartElement("tbody");


            // Write headers
            if (this.Define != null)
            {
                w.WriteStartElement("tr");
                this.WriteTableHeader(w, (FhirString)"Mnemonic");
                this.WriteTableHeader(w, (FhirString)"System");
                this.WriteTableHeader(w, (FhirString)"Display Name");
                w.WriteEndElement(); // tr

                foreach (var itm in this.Define.Concept)
                {
                    w.WriteStartElement("tr");
                    this.WriteTableCell(w, itm);
                    this.WriteTableCell(w, this.Define.System);
                    this.WriteTableCell(w, itm.Display);
                    w.WriteEndElement(); // tr
                }
            }
            else
            {
                w.WriteStartElement("tr");
                this.WriteTableHeader(w, (FhirString)"System");
                this.WriteTableHeader(w, (FhirString)"Mnemonic");
                w.WriteEndElement(); // tr

                foreach (var itm in this.Compose.Include)
                    foreach (var code in itm.Concept)
                    {
                        w.WriteStartElement("tr");

                        if(code == itm.Concept.FirstOrDefault())
                            this.WriteTableCell(w, itm.System, 1, itm.Concept.Count);
                        this.WriteTableCell(w, code);
                        w.WriteEndElement(); // tr
                    }
            }
            w.WriteEndElement();
            w.WriteEndElement(); // table

        }
    }
}
