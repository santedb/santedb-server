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
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Operation outcome
    /// </summary>
    [XmlType("OperationOutcome", Namespace="http://hl7.org/fhir")]
    [XmlRoot("OperationOutcome", Namespace = "http://hl7.org/fhir")]
    public class OperationOutcome : DomainResourceBase
    {

        /// <summary>
        /// Namespace Declarations
        /// </summary>
        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces Namespaces { get { return this.m_namespaces; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public OperationOutcome()
        {
            this.Issue = new List<Issue>();
        }

        /// <summary>
        /// Gets or sets a list of issues 
        /// </summary>
        [XmlElement("issue")]
        [Description("A list of issues related to the operation")]
        [FhirElement(MinOccurs = 1)]
        public List<Issue> Issue { get; set; }


        internal override void WriteText(XmlWriter w)
        {
            w.WriteStartElement("ul", NS_XHTML);
            foreach(var iss in this.Issue)
            {
                w.WriteStartElement("li", NS_XHTML);
                w.WriteElementString("strong", NS_XHTML, iss.Severity.Value.ToString());
                w.WriteString($" {iss.Diagnostics.Value} ({iss.Code?.Display})");
                w.WriteEndElement();
            }
            w.WriteEndElement();
        }
    }
}
