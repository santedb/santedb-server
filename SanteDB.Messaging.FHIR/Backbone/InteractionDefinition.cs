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
using SanteDB.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;
using System.ComponentModel;
using SanteDB.Messaging.FHIR.Attributes;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Search parameter type
    /// </summary>
    [XmlType("SearchParamType", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/type-restful-interaction")]
    public enum TypeRestfulInteraction
    {
        [XmlEnum("read")]
        Read,
        [XmlEnum("vread")]
        VersionRead,
        [XmlEnum("update")]
        Update,
        [XmlEnum("patch")]
        Patch,
        [XmlEnum("delete")]
        Delete,
        [XmlEnum("history-instance")]
        InstanceHistory,
        [XmlEnum("history-type")]
        ResourceHistory,
        [XmlEnum("create")]
        Create,
        [XmlEnum("search-type")]
        Search
    }
    /// <summary>
    /// Operation definition
    /// </summary>
    [XmlType("InteractionDefinition", Namespace = "http://hl7.org/fhir")]
    public class InteractionDefinition : BackboneElement
    {

        /// <summary>
        /// Type of operation
        /// </summary>
        [Description("Type of operation")]
        [XmlElement("code")]
        public FhirCode<TypeRestfulInteraction> Type { get; set; }

        /// <summary>
        /// Documentation related to the operation
        /// </summary>
        [Description("Documentation related to the operation")]
        [XmlElement("documentation")]
        public FhirString Documentation { get; set; }

    }
}
