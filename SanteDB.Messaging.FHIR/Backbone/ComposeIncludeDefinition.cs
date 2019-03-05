/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Identifies a concept set
    /// </summary>
    [XmlType("ValueSet.Compose.Include", Namespace = "http://hl7.org/fhir")]
    public class ComposeIncludeDefinition : BackboneElement
    {
        /// <summary>
        /// Concept set
        /// </summary>
        public ComposeIncludeDefinition()
        {
            this.Concept = new List<ConceptDefinition>();
        }

        /// <summary>
        /// Gets or sets the codification system from which codes are included
        /// </summary>
        [XmlElement("system")]
        [Description("The system the codes come from")]
        [FhirElement(MinOccurs = 1)]
        public FhirUri System { get; set; }

        /// <summary>
        /// Gets or sets the version of the code system
        /// </summary>
        [XmlElement("version")]
        [Description("Specific version of the code system referred to")]
        public FhirString Version { get; set; }

        /// <summary>
        /// Gets or sets the codes to be imported
        /// </summary>
        [XmlElement("concept")]
        [Description("Concepts defined in the system to be composed")]
        public List<ConceptDefinition> Concept { get; set; }

        /// <summary>
        /// Gets or sets a filter for composition
        /// </summary>
        [XmlElement("filter")]
        public List<ComposeFilterDefinition> Filter { get; set; }
    }
}
