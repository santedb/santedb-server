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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.Attributes;
using System.ComponentModel;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Qualification
    /// </summary>
    [XmlType("Qualification", Namespace = "http://hl7.org/fhir")]
    public class Qualification : FhirElement
    {
        /// <summary>
        /// Qualification
        /// </summary>
        public Qualification()
        {
            this.Identifier = new List<FhirIdentifier>();
        }

        /// <summary>
        /// Identifier for this qualification
        /// </summary>
        [XmlElement("identifier")]
        [Description("An identifier for this qualification")]
        public List<FhirIdentifier> Identifier { get; set; }

        /// <summary>
        /// Gets or sets the code
        /// </summary>
        [XmlElement("code")]
        [FhirElement(MinOccurs = 1)]
        public FhirCodeableConcept Code { get; set; }

        /// <summary>
        /// Gets or sets the period of time
        /// </summary>
        [XmlElement("period")]
        public FhirPeriod Period { get; set; }

        /// <summary>
        /// Gets or sets the issuer organization
        /// </summary>
        [XmlElement("issuer")]
        public Reference<Organization> Issuer { get; set; }

    }
}
