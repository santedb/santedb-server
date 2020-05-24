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
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{

    /// <summary>
    /// Substance status codes
    /// </summary>
    [XmlType("SubstanceStatus", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/FHIRsubstanceStatus")]
    public enum SubstanceStatus
    {
        /// <summary>
        /// Substance is active
        /// </summary>
        [XmlEnum("active")]
        Active,
        /// <summary>
        /// Substance is inactive
        /// </summary>
        [XmlEnum("inactive")]
        Inactive,
        /// <summary>
        /// Substance is nullified
        /// </summary>
        [XmlEnum("entered-in-error")]
        Nullified
    }

    /// <summary>
    /// Represents a substance which is packaged or represents a type of substance
    /// </summary>
    [XmlType("Substance", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("Substance", Namespace = "http://hl7.org/fhir")]
    public class Substance : DomainResourceBase
    {

        /// <summary>
        /// Creates a new instance of the substance
        /// </summary>
        public Substance()
        {
            this.Identifier = new List<FhirIdentifier>();
            this.Instance = new List<SubstanceInstance>();
        }

        /// <summary>
        /// Gets or sets the identifier for the substance
        /// </summary>
        [XmlElement("identifier")]
        [Description("Unique identifier for the substance")]
        public List<FhirIdentifier> Identifier { get; set; }

        /// <summary>
        /// Gets or sets the functional status of the substance
        /// </summary>
        [XmlElement("status")]
        [Description("The status of the substance")]
        public FhirCode<SubstanceStatus> Status { get; set; }

        /// <summary>
        /// Gets or sets the type or category of stubstance represented
        /// </summary>
        [XmlElement("category")]
        [Description("The category/type of the substance represented")]
        public FhirCodeableConcept Category { get; set; }

        /// <summary>
        /// Gets or sets the coded representation of the substance
        /// </summary>
        [XmlElement("code")]
        [Description("What substance is represented")]
        public FhirCodeableConcept Code { get; set; }

        /// <summary>
        /// .Gets or sets the textual representation of the substance
        /// </summary>
        [XmlElement("description")]
        [Description("Textual description of the substance or comments")]
        public FhirString Description { get; set; }

        /// <summary>
        /// Gets or sets the instances which this substance represents
        /// </summary>
        [XmlElement("instance")]
        [Description("Describes a sepcificy package or container for this substance")]
        public List<SubstanceInstance> Instance { get; set; }


        /// <summary>
        /// Represent as a string
        /// </summary>
        public override string ToString()
        {
            return String.Format("[Substance] {0}", this.Code?.GetPrimaryCode()?.Display);
        }

    }
}
