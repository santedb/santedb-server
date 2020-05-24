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
    /// Location status
    /// </summary>
    [XmlType("LocationStatus", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/locationStatus")]
    public enum LocationStatus
    {
        /// <summary>
        /// The location is active
        /// </summary>
        [XmlEnum("active")]
        Active,
        /// <summary>
        /// The location record has been suspended
        /// </summary>
        [XmlEnum("suspended")]
        Suspended,
        /// <summary>
        /// The location is currently inactive
        /// </summary>
        [XmlEnum("inactive")]
        Inactive
    }

    /// <summary>
    /// Identifies the mode of the location entry
    /// </summary>
    [XmlType("LocationMode", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/locationMode")]
    public enum LocationMode
    {
        /// <summary>
        /// The location represents a specific location (example: Toronto General Hospital)
        /// </summary>
        [XmlEnum("instance")]
        Instance,
        /// <summary>
        /// The location represents a kind of location (example: Hospital)
        /// </summary>
        [XmlEnum("kind")]
        Kind
    }

    /// <summary>
    /// Represents a phyiscal location where care is delivered
    /// </summary>
    [XmlType("Location", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("Location", Namespace = "http://hl7.org/fhir")]
    [Description("Identifies a location where services are delivered")]
    public class Location : DomainResourceBase
    {
        /// <summary>
        /// Default ctor
        /// </summary>
        public Location()
        {
            this.Identifier = new List<FhirIdentifier>();
            this.Alias = new List<FhirString>();
            this.Telecom = new List<FhirTelecom>();
        }

        /// <summary>
        /// Gets or sets the identifiers for the location resource
        /// </summary>
        [Description("Unique code or number identifying the location")]
        [XmlElement("identifier")]
        public List<FhirIdentifier> Identifier { get; set; }

        /// <summary>
        /// Gets or sets the status of the location
        /// </summary>
        [XmlElement("status")]
        [Description("Identifies the status of the location")]
        public FhirCode<LocationStatus> Status { get; set; }

        /// <summary>
        /// Gets or sets the operation status of the location
        /// </summary>
        [XmlElement("operationalStatus")]
        [Description("The operational status of the location")]
        public FhirCoding OperationalStatus { get; set; }

        /// <summary>
        /// Gets or sets the name of the location
        /// </summary>
        [XmlElement("name")]
        [Description("Name of the location")]
        public FhirString Name { get; set; }

        /// <summary>
        /// Gets or sets the alias names for the location
        /// </summary>
        [XmlElement("alias")]
        [Description("Alternate names for the location")]
        public List<FhirString> Alias { get; set; }

        /// <summary>
        /// Gets or sets the mode of the location instance
        /// </summary>
        [XmlElement("mode")]
        [Description("Indicates whether the location is a type or instance")]
        public FhirCode<LocationMode> Mode { get; set; }

        /// <summary>
        /// Gets or sets the type of the location
        /// </summary>
        [XmlElement("type")]
        [Description("Classifies the type of physical location or function provided")]
        public FhirCodeableConcept Type { get; set; }

        /// <summary>
        /// Gets or sets the list of telecom addresses
        /// </summary>
        [XmlElement("telecom")]
        [Description("Telecommunications addresses for the location")]
        public List<FhirTelecom> Telecom { get; set; }

        /// <summary>
        /// Gets or sets the address for the location
        /// </summary>
        [XmlElement("address")]
        [Description("Physical address for the location")]
        public FhirAddress Address { get; set; }

        /// <summary>
        /// Gets or sets the physical type
        /// </summary>
        [XmlElement("physicalType")]
        [Description("Physical form of the location")]
        public FhirCodeableConcept PhysicalType { get; set; }

        /// <summary>
        /// Gets or sets the position of the location
        /// </summary>
        [XmlElement("position")]
        [Description("The absolute geographic data for the location")]
        public Position Position { get; set; }

        /// <summary>
        /// Gets or sets the managing organization
        /// </summary>
        [XmlElement("managingOrganization")]
        [Description("Orgnaization responsible for the provisiioning and upkeep of services")]
        public Reference<Organization> ManagingOrganization { get; set; }

        /// <summary>
        /// Gets or sets the parent or master group of this location
        /// </summary>
        [XmlElement("partOf")]
        [Description("Another location that this one is physically part of")]
        public Reference<Location> PartOf { get; set; }
        
        /// <summary>
        /// Represent as a string
        /// </summary>
        public override string ToString()
        {
            return String.Format("[Location] {0}", this.Name);
        }
    }
}
