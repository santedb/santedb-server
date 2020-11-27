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
using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Yet another way to represent a code selected in a value set, this time for the purpose of the expansion option
    /// </summary>
    [XmlType("ValueSet.Expansion.Contains", Namespace = "http://hl7.org/fhir")]
    public class ExpansionContainsDefinition : BackboneElement
    {
        /// <summary>
        /// Creates a new expansion contains definition
        /// </summary>
        public ExpansionContainsDefinition()
        {
            this.Contains = new List<ExpansionContainsDefinition>();
        }

        /// <summary>
        /// Gets or sets the system in which the expansion fits
        /// </summary>
        [XmlElement("system")]
        [Description("System value for the code")]
        public FhirUri System { get; set; }

        /// <summary>
        /// Gets or sets whether user can select entry
        /// </summary>
        [XmlElement("abstract")]
        [Description("If user cannot select this entry")]
        public FhirBoolean Abstract { get; set; }

        /// <summary>
        /// Gets or sets the version of the code system
        /// </summary>
        [XmlElement("version")]
        [Description("Version in which this code/display is defined")]
        public FhirString Version { get; set; }

        /// <summary>
        /// Gets or sets the code 
        /// </summary>
        [XmlElement("code")]
        [Description("Code- if blank this is not a selectable code")]
        public FhirCode<String> Code { get; set; }

        /// <summary>
        /// User display for the concept
        /// </summary>
        [XmlElement("display")]
        [Description("User display for the concept")]
        public FhirString Display { get; set; }

        /// <summary>
        /// Codes contained under the entry
        /// </summary>
        [XmlElement("contains")]
        [Description("Codes contained under this entry")]
        public List<ExpansionContainsDefinition> Contains { get; set; }

    }
}
