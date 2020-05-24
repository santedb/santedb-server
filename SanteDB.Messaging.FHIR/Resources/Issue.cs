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
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{

    /// <summary>
    /// Issue severity
    /// </summary>
    [XmlType("IssueSeverity", Namespace = "http://hl7.org/fhir")]
    public enum IssueSeverity
    {
        /// <summary>
        /// The detected issue is fatal
        /// </summary>
        [XmlEnum("fatal")]
        Fatal,
        /// <summary>
        /// The detected issue is an error
        /// </summary>
        [XmlEnum("error")]
        Error,
        /// <summary>
        /// The issue is a warning
        /// </summary>
        [XmlEnum("warning")]
        Warning,
        /// <summary>
        /// The issue is an informational text
        /// </summary>
        [XmlEnum("information")]
        Information
    }

    /// <summary>
    /// Represents an issue detail
    /// </summary>
    [XmlType("Issue", Namespace = "http://hl7.org/fhir")]
    public class Issue : FhirElement
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public Issue()
        {
            this.Location = new List<FhirString>();
        }

        /// <summary>
        /// Gets or sets the severity
        /// </summary>
        [XmlElement("severity")]
        [Description("Identifies the severity of operation")]
        [FhirElement(MinOccurs = 1, RemoteBinding = "http://hl7.org/fhir/issue-severity")]
        public FhirCode<IssueSeverity> Severity { get; set; }

        /// <summary>
        /// Gets or sets the type of error
        /// </summary>
        [XmlElement("code")]
        [Description("Identifies the type of issue detected")]
        [FhirElement(RemoteBinding = "http://hl7.org/fhir/issue-type")]
        public FhirCoding Code { get; set; }

        /// <summary>
        /// Gets or sets the details of the issue
        /// </summary>
        [XmlElement("diagnostics")]
        [Description("Additional description of the issue")]
        public FhirString Diagnostics { get; set; }

        /// <summary>
        /// Gets or sets the location
        /// </summary>
        [XmlElement("location")]
        [Description("XPath of the element(s) related to the issue")]
        public List<FhirString> Location { get; set; }
    }
}
