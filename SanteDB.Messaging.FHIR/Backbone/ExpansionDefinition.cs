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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Represents an expanded value set
    /// </summary>
    [XmlType("ValueSet.Expansion", Namespace = "http://hl7.org/fhir")]
    public class ExpansionDefinition : BackboneElement
    {

        /// <summary>
        /// Expansion definition
        /// </summary>
        public ExpansionDefinition()
        {
            this.Contains = new List<ExpansionContainsDefinition>();
        }

        /// <summary>
        /// Gets or sets the identifier for the expansion
        /// </summary>
        [XmlElement("identifier")]
        [Description("Uniquely identifies this expansion")]
        [FhirElement(MinOccurs = 1)]
        public FhirUri Identifier { get; set; }

        /// <summary>
        /// Gets or sets the time the valueset was expanded
        /// </summary>
        [XmlElement("timestamp")]
        [Description("Time ValueSet expansion happened")]
        [FhirElement(MinOccurs = 1)]
        public FhirDateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the total number of codes in the expansion
        /// </summary>
        [XmlElement("total")]
        [Description("Total number of codes in the expansion")]
        public FhirInt Total { get; set; }

        /// <summary>
        /// Gets or sets the offset at which this resource starts
        /// </summary>
        [XmlElement("offset")]
        [Description("Offset at which this resource starts")]
        public FhirInt Offset { get; set; }

        /// <summary>
        /// Gets or sets the concepts contained in the expansion
        /// </summary>
        [XmlElement("contains")]
        [Description("Codes contained in the value set expansion")]
        public List<ExpansionContainsDefinition> Contains { get; set; }
    }
}
