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
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{

    /// <summary>
    /// Extension context value set
    /// </summary>
    [XmlType("ExtensionContext", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/extension-context")]
    public enum ExtensionContext
    {
        /// <summary>
        /// The extension applies to a FHIR resource
        /// </summary>
        [XmlEnum("resource")]
        Resource,
        /// <summary>
        /// The extension applies to a FHIR datatype
        /// </summary>
        [XmlEnum("datatype")]
        Datatype,
        /// <summary>
        /// The extension is a mapping
        /// </summary>
        [XmlEnum("mapping")]
        Mapping,
        /// <summary>
        /// The extension applies to an extension
        /// </summary>
        [XmlEnum("extension")]
        Extension
    }

    /// <summary>
    /// Structure definition kind
    /// </summary>
    [XmlType("StructureDefinitionKind", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/structure-definition-kind")]
    public enum StructureDefinitionKind
    {
        /// <summary>
        /// The structure is a complex-type
        /// </summary>
        [XmlEnum("complex-type")]
        Complex,
        /// <summary>
        /// The structure is a primitive type
        /// </summary>
        [XmlEnum("primitive-type")]
        Datatype,
        /// <summary>
        /// The structure is a resource
        /// </summary>
        [XmlEnum("resource")]
        Resource,
        /// <summary>
        /// The structure is a logical construct
        /// </summary>
        [XmlEnum("logical")]
        Logical
    }

    /// <summary>
    /// Type derivation rules
    /// </summary>
    [XmlType("TypeDerivationRule", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/type-derivation-rule")]
    public enum TypeDerivationRule
    {
        /// <summary>
        /// This type constrains the object is derives
        /// </summary>
        [XmlEnum("constraint")]
        Constraint,
        /// <summary>
        /// This type is a specialization of the object type it dervies
        /// </summary>
        [XmlEnum("specialization")]
        Specialization
    }

    /// <summary>
    /// Represents a profile
    /// </summary>
    [XmlType("StructureDefinition", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("StructureDefinition", Namespace = "http://hl7.org/fhir")]
    public class StructureDefinition : DomainResourceBase
    {
        /// <summary>
        /// Structure definition
        /// </summary>
        public StructureDefinition()
        {
            this.Identifier = new List<FhirIdentifier>();
            this.Contact = new List<ContactDetail>();
        }

        /// <summary>
        /// Gets or sets the absolute URL to access the resource
        /// </summary>
        [XmlElement("url")]
        [Description("Absolute URL used to reference this StructureDefinition")]
        [FhirElement(MinOccurs = 1)]
        public FhirUri Url { get; set; }
        /// <summary>
        /// Gets or sets other identifiers for the definition
        /// </summary>
        [XmlElement("identifier")]
        [Description("Other identifier for the StructureDefinition")]
        public List<FhirIdentifier> Identifier { get; set; }
        /// <summary>
        /// Gets or sets the logical identifier for the version of the structure definition
        /// </summary>
        [XmlElement("version")]
        [Description("Logical identifier for the version of the structure")]
        public FhirString Version { get; set; }
        /// <summary>
        /// Gets or sets the informal name for the structure definition
        /// </summary>
        [XmlElement("name")]
        [Description("Informal name for this structure definition")]
        public FhirString Name { get; set; }
        /// <summary>
        /// Gets or sets the display name of the structure
        /// </summary>
        [XmlElement("title")]
        [Description("Use this name when displaying the value")]
        public FhirString Title { get; set; }

        /// <summary>
        /// Gets or sets the status of the structure definition
        /// </summary>
        [XmlElement("status")]
        [Description("The status of the structure definition")]
        public FhirCode<PublicationStatus> Status { get; set; }

        /// <summary>
        /// Gets or sets whether this structure definition is experimental
        /// </summary>
        [XmlElement("experimental")]
        [Description("If for testing purposes, not real usage")]
        public FhirBoolean Experimental { get; set; }

        /// <summary>
        /// Gets or sets the date for this version of the structure definition
        /// </summary>
        [XmlElement("date")]
        [Description("Date for this version of the structure definition")]
        public FhirDateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the publisher name
        /// </summary>
        [XmlElement("publisher")]
        [Description("Name of the publisher")]
        public FhirString Publisher { get; set; }

        /// <summary>
        /// Gets or sets the contact information for the publisher
        /// </summary>
        [XmlElement("contact")]
        [Description("Contact details of the publisher")]
        public List<ContactDetail> Contact { get; set; }
       
        /// <summary>
        /// Gets or sets natural language description
        /// </summary>
        [XmlElement("description")]
        [Description("Natural language description of the structure definition")]
        public FhirString Description { get; set; }

        /// <summary>
        /// Gets or sets the context 
        /// </summary>
        [XmlElement("useContext")]
        [Description("Content intends to support these contexts")]
        public List<FhirCodeableConcept> UseContext { get; set; }
        
        /// <summary>
        /// Gets or sets use and/or publishing restrictions
        /// </summary>
        [XmlElement("copyright")]
        [Description("Use and/or publishing restrictions")]
        public FhirString Copyright { get; set; }

        /// <summary>
        /// Assists with indexing and finding
        /// </summary>
        [XmlElement("code")]
        [Description("Assists with indexing and finding")]
        [FhirElement(MaxOccurs = 0)]
        public List<FhirCodeableConcept> Code { get; set; }

        /// <summary>
        /// Gets or sets the version of FHIR
        /// </summary>
        [XmlElement("fhirVersion")]
        [Description("FHIR Version this StructureDefinition targets")]
        public FhirString FhirVersion { get; set; }

        /// <summary>
        /// Gets or sets the kind of structure definition
        /// </summary>
        [XmlElement("kind")]
        [Description("Identifies the kind of structure definition")]
        [FhirElement(MinOccurs = 1)]
        public FhirCode<StructureDefinitionKind> Kind { get; set; }

        /// <summary>
        /// Gets or sets the abstract structure definition
        /// </summary>
        [XmlElement("abstract")]
        [Description("Whether the structure is abstract")]
        [FhirElement(MinOccurs = 1)]
        public FhirBoolean Abstract { get; set; }

        /// <summary>
        /// Gets or sets the context where an extension can be used
        /// </summary>
        [XmlElement("contextType")]
        [Description("Where the extension can be used in instances")]
        public FhirCode<ExtensionContext> ContextType { get; set; }

        /// <summary>
        /// Where the element can be used 
        /// </summary>
        [XmlElement("context")]
        [Description("Where the element extension can be used")]
        public FhirString Context { get; set; }

        /// <summary>
        /// The type contained by the structure
        /// </summary>
        [Description("The derfined type constrainted by this structure")]
        [XmlElement("type")]
        public FhirCode<String> Type { get; set; }

        /// <summary>
        /// Gets or sets the derivation rule
        /// </summary>
        [XmlElement("derivation")]
        [Description("Identifies whether the structure is a constraint or specialization")]
        public FhirCode<TypeDerivationRule> DerivationType { get; set; }

        /// <summary>
        /// Structure that this structure definition extends
        /// </summary>
        [XmlElement("baseDefinition")]
        [Description("Structure that this set of constraints applies to")]
        public FhirUri Base { get; set; }

        ///// <summary>
        ///// Snapshot view of the structure 
        ///// </summary>
        [XmlElement("snapshot")]
        [Description("Snapshot view of the structure")]
        public StructureDefinitionContent Snapshot { get; set; }
    }
}
