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

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Identifies a concept definition
    /// </summary>
    [XmlType("ValueSet.CodeSystem.Concept", Namespace = "http://hl7.org/fhir")]
    public class ConceptDefinition : BackboneElement
    {

        /// <summary>
        /// Concept designation
        /// </summary>
        public ConceptDefinition()
        {
            this.Designation = new List<ConceptDesignation>();
            this.Concept = new List<ConceptDefinition>();
        }

        /// <summary>
        /// Gets or sets the code
        /// </summary>
        [XmlElement("code")]
        [FhirElement(MinOccurs = 1)]
        public FhirCode<String> Code { get; set; }
        
        /// <summary>
        /// Gets or sets the abstract modifier
        /// </summary>
        [XmlElement("abstract")]
        public FhirBoolean Abstract { get; set; }

        /// <summary>
        /// Gets or sets the display name for the code
        /// </summary>
        [XmlElement("display")]
        public FhirString Display { get; set; }

        /// <summary>
        /// Gets or sets the definition
        /// </summary>
        [XmlElement("definition")]
        public FhirString Definition { get; set; }

        /// <summary>
        /// Gets or sets additional designations
        /// </summary>
        [XmlElement("designation")]
        public List<ConceptDesignation> Designation { get; set; }

        /// <summary>
        /// Gets or sets child concepts
        /// </summary>
        [XmlElement("concept")]
        public List<ConceptDefinition> Concept { get; set; }

        /// <summary>
        /// Write text
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {
            w.WriteStartElement("a");
            w.WriteAttributeString("href", String.Format("#{0}", this.Code));
            this.Code.WriteText(w);
            w.WriteEndElement();
        }
    }
}
