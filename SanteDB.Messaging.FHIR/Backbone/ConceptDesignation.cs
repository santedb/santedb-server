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
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Concept designation
    /// </summary>
    [XmlType("ValueSet.Concept.Designation", Namespace = "http://hl7.org/fhir")]
    public class ConceptDesignation : BackboneElement
    {

        /// <summary>
        /// Gets or sets the language of the designation
        /// </summary>
        [XmlElement("language")]
        [Description("Human language of the designation")]
        [FhirElement(RemoteBinding = "http://tools.ietf.org/html/bcp47")]
        public FhirCode<String> Language { get; set; }

        /// <summary>
        /// Gets or sets how the designation should be used
        /// </summary>
        [XmlElement("use")]
        [Description("Details how this designation would be used")]
        [FhirElement(RemoteBinding = "http://hl7.org/fhir/ValueSet/designation-use")]
        public FhirCoding Use { get; set; }

        /// <summary>
        /// Gets or sets the value of the designation
        /// </summary>
        [FhirElement(MinOccurs = 1)]
        [XmlElement("value")]
        [Description("The text value for the designation")]
        public FhirString Value { get; set; }

    }
}
