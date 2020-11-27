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
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{

    /// <summary>
    /// Bundle resource
    /// </summary>
    [XmlType("Bundle.Resource", Namespace = "http://hl7.org/fhir")]
    public class BundleResrouce : FhirElement
    {

        /// <summary>
        /// 
        /// </summary>
        public BundleResrouce()
        {

        }
        /// <summary>
        /// Creates a new instance of the resource bunlde
        /// </summary>
        /// <param name="r"></param>
        public BundleResrouce(DomainResourceBase r)
        {
            this.Resource = r;
        }

        /// <summary>
        /// Gets or sets the resource
        /// </summary>
        [XmlElement("Patient", Type = typeof(Patient))]
        [XmlElement("ValueSet", Type = typeof(ValueSet))]
        [XmlElement("Organization", Type = typeof(Organization))]
        [XmlElement("Practitioner", Type = typeof(Practitioner))]
        [XmlElement("Immunization", Type = typeof(Immunization))]
        [XmlElement("ImmunizationRecommendation", Type = typeof(Resources.ImmunizationRecommendation))]
        [XmlElement("RelatedPerson", Type = typeof(RelatedPerson))]
        [XmlElement("Location", Type = typeof(Location))]
        [XmlElement("Observation", Type = typeof(Observation))]
        [XmlElement("Medication", Type = typeof(Medication))]
        [XmlElement("Substance", Type = typeof(Substance))]
        [XmlElement("AllergyIntolerance", Type = typeof(AllergyIntolerance))]
        [XmlElement("AdverseEvent", Type = typeof(AdverseEvent))]
        [XmlElement("Condition", Type = typeof(Condition))]
        [XmlElement("MedicationAdministration", Type = typeof(MedicationAdministration))]
        [XmlElement("Encounter", Type = typeof(Encounter))]
        public DomainResourceBase Resource { get; set; }

    }
}