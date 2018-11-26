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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{

    /// <summary>
    /// Transaction mode
    /// </summary>
    [XmlType("TransactionMode", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/transaction-mode")]
    [Flags]
    public enum TransactionMode 
    {
        [XmlEnum("not-supported")]
        NotSupported = 0x0,
        [XmlEnum("batch")]
        Batch = 0x1,
        [XmlEnum("transaction")]
        Transaction = 0x2,
        [XmlEnum("both")]
        Both = Batch | Transaction  
    }
    /// <summary>
    /// RESTFul definition
    /// </summary>
    [XmlType("Rest", Namespace = "http://hl7.org/fhir")]
    public class RestDefinition : BackboneElement
    {

        /// <summary>
        /// Creates a new instance of the rest definition
        /// </summary>
        public RestDefinition()
        {
            this.Resource = new List<ResourceDefinition>();
            this.Interaction = new List<InteractionDefinition>();
            this.SearchParam = new List<SearchParamDefinition>();
            this.Compartment = new List<FhirUri>();
        }

        /// <summary>
        /// Gets or sets the mode (client or server)
        /// </summary>
        [XmlElement("mode")]
        [Description("Describes the mode of REST implementation")]
        [FhirElement(MinOccurs = 1)]
        public FhirCode<String> Mode { get; set; }

        /// <summary>
        /// Gets or sets the documentation about the REST 
        /// </summary>
        [XmlElement("documentation")]
        [Description("General description of the implementation")]
        public FhirString Documentation { get; set; }

        /// <summary>
        /// Gets or sets information about the security implementation
        /// </summary>
        [XmlElement("security")]
        [Description("Information about the security implementation")]
        public SecurityDefinition Security { get; set; }

        /// <summary>
        /// Resource supported by the rest interface
        /// </summary>
        [XmlElement("resource")]
        [Description("Resource served on the rest interface")]
        [FhirElement(MinOccurs = 1)]
        public List<ResourceDefinition> Resource { get; set; }

        /// <summary>
        /// Gets or sets interaction definitions
        /// </summary>
        [XmlElement("interaction")]
        [Description("What operations are supported?")]
        public List<InteractionDefinition> Interaction { get; set; }

        /// <summary>
        /// Search parameter definition for all resources
        /// </summary>
        [XmlElement("searchParam")]
        [Description("Search params for searching all resources")]
        public List<SearchParamDefinition> SearchParam { get; set; }

        //        public List<SystemOperation> Operation { get; set; }

        /// <summary>
        /// Gets or sets compartments serviced by the system
        /// </summary>
        [XmlElement("compartment")]
        [Description("Compartments served/used by system")]
        public List<FhirUri> Compartment { get; set; }

        /// <summary>
        /// RESTful information
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {
            w.WriteStartElement("table");
            w.WriteAttributeString("border", "1");
            w.WriteElementString("caption", String.Format("{0} Operations", this.Mode));
            w.WriteStartElement("tbody");

            // Setup table headers
            List<FhirCode<String>> headers = new List<FhirCode<String>>();
            headers.Add(new FhirCode<string>("Resource"));
            foreach (var res in this.Resource)
                foreach (var op in res.Interaction)
                    if (!headers.Exists(o=>o.Value == op.Type.ToString()))
                        headers.Add(op.Type.ToString());
            w.WriteStartElement("tr");
            foreach (var hdr in headers)
                base.WriteTableHeader(w, hdr);
            w.WriteEndElement();

            // Now create resource options
            foreach (var res in this.Resource)
            {
                w.WriteStartElement("tr");
                base.WriteTableCell(w, res);
                bool[] supported = new bool[headers.Count - 1];
                foreach (var op in res.Interaction)
                    supported[headers.FindIndex(o => o.Value == op.Type.ToString()) - 1] = true;
                for (int i = 0; i < supported.Length; i++)
                    base.WriteTableCell(w, new FhirString(supported[i] ? "X" : " "));
                w.WriteEndElement(); // tr
            }

            w.WriteEndElement(); // tbody
            w.WriteEndElement(); // table


        }

    }
}
