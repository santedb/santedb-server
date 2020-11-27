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
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{

    /// <summary>
    /// Patient Link type
    /// </summary>
    [XmlType("LinkType", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/link-type")]
    public enum PatientLinkType
    {
        [XmlEnum("replace")]
        Replace,
        [XmlEnum("refer")]
        Refer,
        [XmlEnum("seealso")]
        SeeAlso
    }

    /// <summary>
    /// Represents a link between two patients
    /// </summary>
    [XmlType("Patient.Link", Namespace = "http://hl7.org/fhir")]
    public class PatientLink : BackboneElement
    {

        /// <summary>
        /// Gets or sets the other patient
        /// </summary>
        [XmlElement("other")]
        [Description("The other patient resource the link refers to")]
        public Reference<Patient> Other { get; set; }

        /// <summary>
        /// Gets or sets the type of link
        /// </summary>
        [XmlElement("type")]
        [Description("Type of link")]
        public FhirCode<PatientLinkType> Type { get; set; }
    }
}
