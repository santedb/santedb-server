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
using SanteDB.Messaging.FHIR.Resources;
using System;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// A list of contained resource
    /// </summary>
    [XmlType("ContainedResource", Namespace = "http://hl7.org/fhir")]
    [Serializable]
    public class ContainedResource : FhirElement
    {
        /// <summary>
        /// The contained resource collection
        /// </summary>
        public ContainedResource()
        {
        }

        /// <summary>
        /// Items within the collection
        /// </summary>
        [XmlElement(ElementName = "Patient", Type = typeof(Patient))]
        [XmlElement(ElementName = "Organization", Type = typeof(Organization))]
        [XmlElement(ElementName = "Picture", Type = typeof(Picture))]
        [XmlElement(ElementName = "Practictioner", Type = typeof(Practitioner))]
        [XmlElement(ElementName = "OperationOutcome", Type = typeof(OperationOutcome))]
        [XmlElement(ElementName = "ValueSet", Type = typeof(ValueSet))]
        [XmlElement(ElementName = "Profile", Type = typeof(StructureDefinition))]
        [XmlElement(ElementName = "Conformance", Type = typeof(CapabilityStatement))]
        [XmlElement(ElementName = "RelatedPerson", Type = typeof(RelatedPerson))]
        public DomainResourceBase Item { get; set; }

        /// <summary>
        /// Write the item text
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {
            this.Item.WriteText(w);
        }
    }
}
