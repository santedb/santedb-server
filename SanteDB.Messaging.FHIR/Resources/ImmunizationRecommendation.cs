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
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Represents the proposal to perform an immunization
    /// </summary>
    [XmlType(nameof(ImmunizationRecommendation), Namespace = "http://hl7.org/fhir")]
    [XmlRoot(nameof(ImmunizationRecommendation), Namespace = "http://hl7.org/fhir")]
    public class ImmunizationRecommendation : DomainResourceBase
    {

        /// <summary>
        /// Immunization recommendataion
        /// </summary>
        public ImmunizationRecommendation()
        {
            this.Identifier = new List<FhirIdentifier>();
            this.Recommendation = new List<Backbone.ImmunizationRecommendation>();
        }

        /// <summary>
        /// Gets or sets the identifiers for the immunization recommendation
        /// </summary>
        [XmlElement("identifier")]
        public List<FhirIdentifier> Identifier { get; set; }

        /// <summary>
        /// Gets or sets the patient to which the recommendation applies
        /// </summary>
        [XmlElement("patient")]
        [FhirElement(MinOccurs = 1)]
        public Reference<Patient> Patient { get; set; }

        /// <summary>
        /// Gets or sets recommendations
        /// </summary>
        [XmlElement("recommendation")]
        [FhirElement(MinOccurs = 1)]
        public List<Backbone.ImmunizationRecommendation> Recommendation { get; set; }

    }
}
