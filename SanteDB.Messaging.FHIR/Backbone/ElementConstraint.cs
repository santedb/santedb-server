/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Constraint severity value set
    /// </summary>
    [XmlType("ConstraintSeverity")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/constraint-severity")]
    public enum ConstraintSeverity
    {
        /// <summary>
        /// Constraint violation is an error
        /// </summary>
        [XmlEnum("error")]
        Error,
        /// <summary>
        /// Constraint violation is a warning
        /// </summary>
        [XmlEnum("warning")]
        Warning
    }

    /// <summary>
    /// Element constraint dictates how the element is constrained
    /// </summary>
    [XmlType("ElementConstraint", Namespace = "http://hl7.org/fhir")]
    public class ElementConstraint : FhirElement
    {
        /// <summary>
        /// Target condition reference
        /// </summary>
        [XmlElement("key")]
        [Description("Target condition reference")]
        [FhirElement(MinOccurs = 1)]
        public FhirId Key { get; set; }

        /// <summary>
        /// Why this constraint is necessary or appropriate
        /// </summary>
        [XmlElement("requirements")]
        [Description("Why this constraint is necessary")]
        public FhirString Requirements { get; set; }

        /// <summary>
        /// Gets or sets the severity of constraint
        /// </summary>
        [XmlElement("severity")]
        [Description("Severity of the constraint")]
        [FhirElement(MinOccurs = 1)]
        public FhirCode<ConstraintSeverity> Severity { get; set; }

        /// <summary>
        /// Gets or sets human description of constraint
        /// </summary>
        [XmlElement("human")]
        [Description("Human description of constraint")]
        [FhirElement(MinOccurs = 1)]
        public FhirString Human { get; set; }

        /// <summary>
        /// XPath expression of constraint
        /// </summary>
        [XmlElement("xpath")]
        [Description("Xpath expression of constraint")]
        [FhirElement(MinOccurs = 1)]
        public FhirString Xpath { get; set; }
    }
}