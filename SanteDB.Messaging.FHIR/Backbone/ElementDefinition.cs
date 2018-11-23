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
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Property representation
    /// </summary>
    [XmlType("PropertyRepresentation", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/property-representation")]
    public enum PropertyRepresentation
    {
        [XmlEnum("xmlAttr")]
        XmlAttribute
    }

    /// <summary>
    /// An element definition
    /// </summary>
    [XmlType("ElementDefinition", Namespace = "http://hl7.org/fhir")]
    public class ElementDefinition : FhirElement
    {

        public ElementDefinition()
        {
            this.Representation = new List<FhirCode<PropertyRepresentation>>();
            this.Code = new List<FhirCoding>();
            this.Alias = new List<FhirString>();
            this.Constraint = new List<ElementConstraint>();
            this.Condition = new List<FhirId>();
            this.Type = new List<ElementType>();
        }
        /// <summary>
        /// Gets or sets the path of the element
        /// </summary>
        [Description("The path of the element")]
        [FhirElement(MinOccurs = 1)]
        [XmlElement("path")]
        public FhirString Path { get; set; }

        /// <summary>
        /// Gets or sets how the element ios represented in instances
        /// </summary>
        [XmlElement("representation")]
        [Description("How the element is represented in instances")]
        public List<FhirCode<PropertyRepresentation>> Representation { get; set; }

        /// <summary>
        /// Gets or sets the name of the element
        /// </summary>
        [XmlElement("sliceName")]
        [Description("The name of this particular sliced element")]
        public FhirString SliceName { get; set; }

        /// <summary>
        /// Gets or sets name for element to display or prompt
        /// </summary>
        [XmlElement("label")]
        [Description("Name for element to display or prompt")]
        public FhirString Label { get; set; }

        /// <summary>
        /// Gets or sets the defining codes
        /// </summary>
        [XmlElement("code")]
        [Description("Defining code")]
        public List<FhirCoding> Code { get; set; }

        /// <summary>
        /// Gets or sets the element slices
        /// </summary>
        [XmlElement("slicing")]
        [Description("This element is sliced. Slices follow")]
        public ElementSlicing Slicing { get; set; }

        /// <summary>
        /// Gets or sets the concise definition for the element
        /// </summary>
        [XmlElement("short")]
        [Description("Concise definition for the element")]
        public FhirString Short { get; set; }

        /// <summary>
        /// Gets or sets the full formal definition
        /// </summary>
        [XmlElement("definition")]
        [Description("Full formal definition as narrative text")]
        public FhirString Definition { get; set; }

        /// <summary>
        /// Gets or sets comments related to the element
        /// </summary>
        [XmlElement("comment")]
        [Description("Comments about the use of this element")]
        public FhirString Comments { get; set; }

        /// <summary>
        /// Whi is this needed?
        /// </summary>
        [XmlElement("requirements")]
        [Description("Why is this needed?")]
        public FhirString Requirements { get; set; }

        /// <summary>
        /// Gets or sets other names
        /// </summary>
        [XmlElement("alias")]
        [Description("Other names")]
        public List<FhirString> Alias { get; set; }

        /// <summary>
        /// Minimum cardinality
        /// </summary>
        [XmlElement("min")]
        [Description("Minimum cardinality")]
        public FhirInt Min { get; set; }

        /// <summary>
        /// Gets or sets the maximum cardinality
        /// </summary>
        [XmlElement("max")]
        [Description("Maximum cardinality")]
        public FhirString Max { get; set; }

        /// <summary>
        /// Gets or sets the base type upon which this element is based
        /// </summary>
        [XmlElement("base")]
        [Description("Base definition information for tools")]
        public ElementBase Base { get; set; }

        /// <summary>
        /// Data type and profile for element
        /// </summary>
        [XmlElement("type")]
        [Description("Data type and profile for the element")]
        public List<ElementType> Type { get; set; }
        

        /// <summary>
        /// Default values
        /// </summary>
        [XmlElement("defaultValueInt", Type = typeof(FhirInt))]
        [XmlElement("defaultValueString", Type = typeof(FhirString))]
        [XmlElement("defaultValueCode", Type = typeof(FhirCode<String>))]
        [XmlElement("defaultValueQuantity", Type = typeof(FhirQuantity))]
        [XmlElement("defaultValueInstant", Type = typeof(FhirInstant))]
        [XmlElement("defaultValueDateTime", Type = typeof(FhirDateTime))]
        [XmlElement("defaultValueDate", Type = typeof(FhirDate))]
        [XmlElement("defaultValueAddress", Type = typeof(FhirAddress))]
        [XmlElement("defaultValueHumanName", Type = typeof(FhirHumanName))]
        [XmlElement("defaultValueBase64Binary", Type = typeof(FhirBase64Binary))]
        [XmlElement("defaultValueCoding", Type = typeof(FhirCoding))]
        [XmlElement("defaultValueDecimal", Type = typeof(FhirDecimal))]
        [XmlElement("defaultValueId", Type = typeof(FhirIdentifier))]
        [XmlElement("defaultValuePeriod", Type = typeof(FhirPeriod))]
        [XmlElement("defaultValueTelecom", Type = typeof(FhirTelecom))]
        [XmlElement("defaultValueUri", Type = typeof(FhirUri))]
        [XmlElement("defaultValueSignature", Type = typeof(FhirSignature))]
        [XmlElement("defaultValueRange", Type = typeof(FhirRange))]
        [XmlElement("defaultValueRatio", Type = typeof(FhirRatio))]
        [Description("Default values")]
        public FhirElement DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the implicit meaning when the element is missing
        /// </summary>
        [XmlElement("meaningWhenMissing")]
        [Description("Implicit meaning when this element is missing")]
        public FhirString MeaningWhenMissing { get; set; }

        /// <summary>
        /// Gets or sets the fixed value
        /// </summary>
        [XmlElement("fixedInt", Type = typeof(FhirInt))]
        [XmlElement("fixedString", Type = typeof(FhirString))]
        [XmlElement("fixedCode", Type = typeof(FhirCode<String>))]
        [XmlElement("fixedQuantity", Type = typeof(FhirQuantity))]
        [XmlElement("fixedInstant", Type = typeof(FhirInstant))]
        [XmlElement("fixedDateTime", Type = typeof(FhirDateTime))]
        [XmlElement("fixedDate", Type = typeof(FhirDate))]
        [XmlElement("fixedAddress", Type = typeof(FhirAddress))]
        [XmlElement("fixedHumanName", Type = typeof(FhirHumanName))]
        [XmlElement("fixedBase64Binary", Type = typeof(FhirBase64Binary))]
        [XmlElement("fixedCoding", Type = typeof(FhirCoding))]
        [XmlElement("fixedDecimal", Type = typeof(FhirDecimal))]
        [XmlElement("fixedId", Type = typeof(FhirIdentifier))]
        [XmlElement("fixedPeriod", Type = typeof(FhirPeriod))]
        [XmlElement("fixedTelecom", Type = typeof(FhirTelecom))]
        [XmlElement("fixedUri", Type = typeof(FhirUri))]
        [XmlElement("fixedSignature", Type = typeof(FhirSignature))]
        [XmlElement("fixedRange", Type = typeof(FhirRange))]
        [XmlElement("fixedRatio", Type = typeof(FhirRatio))]
        [Description("Value must be exactly this")]
        public FhirElement Fixed { get; set; }

        /// <summary>
        /// Gets or sets the pattern of value which an instance must contain
        /// </summary>
        [XmlElement("patternInt", Type = typeof(FhirInt))]
        [XmlElement("patternString", Type = typeof(FhirString))]
        [XmlElement("patternCode", Type = typeof(FhirCode<String>))]
        [XmlElement("patternQuantity", Type = typeof(FhirQuantity))]
        [XmlElement("patternInstant", Type = typeof(FhirInstant))]
        [XmlElement("patternDateTime", Type = typeof(FhirDateTime))]
        [XmlElement("patternDate", Type = typeof(FhirDate))]
        [XmlElement("patternAddress", Type = typeof(FhirAddress))]
        [XmlElement("patternHumanName", Type = typeof(FhirHumanName))]
        [XmlElement("patternBase64Binary", Type = typeof(FhirBase64Binary))]
        [XmlElement("patternCoding", Type = typeof(FhirCoding))]
        [XmlElement("patternDecimal", Type = typeof(FhirDecimal))]
        [XmlElement("patternId", Type = typeof(FhirIdentifier))]
        [XmlElement("patternPeriod", Type = typeof(FhirPeriod))]
        [XmlElement("patternTelecom", Type = typeof(FhirTelecom))]
        [XmlElement("patternUri", Type = typeof(FhirUri))]
        [XmlElement("patternSignature", Type = typeof(FhirSignature))]
        [XmlElement("patternRange", Type = typeof(FhirRange))]
        [XmlElement("patternRatio", Type = typeof(FhirRatio))]
        [Description("Value must have at least these property values")]
        public FhirElement Pattern { get; set; }

        /// <summary>
        /// Gets or sets an example
        /// </summary>
        [XmlElement("exampleInt", Type = typeof(FhirInt))]
        [XmlElement("exampleString", Type = typeof(FhirString))]
        [XmlElement("exampleCode", Type = typeof(FhirCode<String>))]
        [XmlElement("exampleQuantity", Type = typeof(FhirQuantity))]
        [XmlElement("exampleInstant", Type = typeof(FhirInstant))]
        [XmlElement("exampleDateTime", Type = typeof(FhirDateTime))]
        [XmlElement("exampleDate", Type = typeof(FhirDate))]
        [XmlElement("exampleAddress", Type = typeof(FhirAddress))]
        [XmlElement("exampleHumanName", Type = typeof(FhirHumanName))]
        [XmlElement("exampleBase64Binary", Type = typeof(FhirBase64Binary))]
        [XmlElement("exampleCoding", Type = typeof(FhirCoding))]
        [XmlElement("exampleDecimal", Type = typeof(FhirDecimal))]
        [XmlElement("exampleId", Type = typeof(FhirIdentifier))]
        [XmlElement("examplePeriod", Type = typeof(FhirPeriod))]
        [XmlElement("exampleTelecom", Type = typeof(FhirTelecom))]
        [XmlElement("exampleUri", Type = typeof(FhirUri))]
        [XmlElement("exampleSignature", Type = typeof(FhirSignature))]
        [XmlElement("exampleRange", Type = typeof(FhirRange))]
        [XmlElement("exampleRatio", Type = typeof(FhirRatio))]
        [Description("Example value")]
        public FhirElement Example { get; set; }

        /// <summary>
        /// Minimum allowed value
        /// </summary>
        [XmlElement("minValueInt", Type = typeof(FhirInt))]
        [XmlElement("minValueCode", Type = typeof(FhirCode<String>))]
        [XmlElement("minValueQuantity", Type = typeof(FhirQuantity))]
        [XmlElement("minValueInstant", Type = typeof(FhirInstant))]
        [XmlElement("minValueDateTime", Type = typeof(FhirDateTime))]
        [XmlElement("minValueDate", Type = typeof(FhirDate))]
        [XmlElement("minValueDecimal", Type = typeof(FhirDecimal))]
        [XmlElement("minValuePeriod", Type = typeof(FhirPeriod))]
        [XmlElement("minValueRange", Type = typeof(FhirRange))]
        [XmlElement("minValueRatio", Type = typeof(FhirRatio))]
        [Description("Minimum allowed value")]
        public FhirElement MinValue { get; set; }

        /// <summary>
        /// Maximum allowed value
        /// </summary>
        [XmlElement("maxValueInt", Type = typeof(FhirInt))]
        [XmlElement("maxValueCode", Type = typeof(FhirCode<String>))]
        [XmlElement("maxValueQuantity", Type = typeof(FhirQuantity))]
        [XmlElement("maxValueInstant", Type = typeof(FhirInstant))]
        [XmlElement("maxValueDateTime", Type = typeof(FhirDateTime))]
        [XmlElement("maxValueDate", Type = typeof(FhirDate))]
        [XmlElement("maxValueDecimal", Type = typeof(FhirDecimal))]
        [XmlElement("maxValuePeriod", Type = typeof(FhirPeriod))]
        [XmlElement("maxValueRange", Type = typeof(FhirRange))]
        [XmlElement("maxValueRatio", Type = typeof(FhirRatio))]
        [Description("Maximum allowed value")]
        public FhirElement MaxValue { get; set; }

        /// <summary>
        /// Gets or sets the maximum length for strings
        /// </summary>
        [XmlElement("maxLength")]
        [Description("Max length for strings")]
        public FhirInt MaxLength { get; set; }

        /// <summary>
        /// Reference to invariant about presence
        /// </summary>
        [XmlElement("condition")]
        [Description("Reference to invariant about presence")]
        public List<FhirId> Condition { get; set; }

        /// <summary>
        /// Gets or sets a condition that must evaluate to true
        /// </summary>
        [XmlElement("constraint")]
        [Description("Condition that must evaluate to true")]
        public List<ElementConstraint> Constraint { get; set; }

        /// <summary>
        /// If the element must be supported
        /// </summary>
        [XmlElement("mustSupport")]
        [Description("If the element must be supported")]
        public FhirBoolean MustSupport { get; set; }

        /// <summary>
        /// If this modifies the meaning of other elements
        /// </summary>
        [XmlElement("isModifier")]
        [Description("If this modifies the meaning of other elements")]
        public FhirBoolean IsModifier { get; set; }

        /// <summary>
        /// Gets or sets if the item is a summary element
        /// </summary>
        [XmlElement("isSummary")]
        [Description("Include when _summary = true")]
        public FhirBoolean IsSummary { get; set; }

        /// <summary>
        /// Value set details if this is coded
        /// </summary>
        [XmlElement("binding")]
        [Description("Valueset details if this is coded")]
        public ElementBinding Binding { get; set; }

    }
}