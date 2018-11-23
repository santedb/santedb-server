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
using SanteDB.Messaging.FHIR.Attributes;
using System.ComponentModel;

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

    }
}
