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
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Value set definition
    /// </summary>
    [XmlType("ValueSet.CodeSystem", Namespace = "http://hl7.org/fhir")]
    public class CodeSystemDefinition : BackboneElement
    {

        /// <summary>
        /// Value set definition
        /// </summary>
        public CodeSystemDefinition()
        {
            this.Concept = new List<ConceptDefinition>();
        }

        /// <summary>
        /// The code system which is defined by this value set
        /// </summary>
        [XmlElement("system")]
        [FhirElement(MinOccurs = 1)]
        public FhirUri System { get; set; }

        /// <summary>
        /// Gets or sets the version information
        /// </summary>
        [XmlElement("version")]
        public FhirString Version { get; set; }

        /// <summary>
        /// Indicates whether the code system is case sensitive
        /// </summary>
        [XmlElement("caseSensitive")]
        public FhirBoolean CaseSensitive { get; set; }

        /// <summary>
        /// Gets or sets the list of concepts
        /// </summary>
        [XmlElement("concept")]
        public List<ConceptDefinition> Concept { get; set; }
    }
}
