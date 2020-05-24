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
using SanteDB.Messaging.FHIR.DataTypes;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Composed from other systems
    /// </summary>
    [XmlType("ValueSet.Compose", Namespace = "http://hl7.org/fhir")]
    public class ComposeDefinition : BackboneElement
    {

        /// <summary>
        /// Compse a definition
        /// </summary>
        public ComposeDefinition()
        {
            this.Import = new List<FhirUri>();
            this.Include = new List<ComposeIncludeDefinition>();
            this.Exclude = new List<ComposeIncludeDefinition>();
        }

        /// <summary>
        /// The uri of an import
        /// </summary>
        [XmlElement("import")]
        public List<FhirUri> Import { get; set; }

        /// <summary>
        /// Included concepts
        /// </summary>
        [XmlElement("include")]
        public List<ComposeIncludeDefinition> Include { get; set; }

        /// <summary>
        /// Gets or sets the list of items to exclude
        /// </summary>
        [XmlElement("exclude")]
        public List<ComposeIncludeDefinition> Exclude { get; set; }
    }
}
