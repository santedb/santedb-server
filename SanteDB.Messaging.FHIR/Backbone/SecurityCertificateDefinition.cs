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
using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Security certificate definition
    /// </summary>
    [XmlType("SecurityCertificateDefinition", Namespace = "http://hl7.org/fhir")]
    public class SecurityCertificateDefinition : BackboneElement
    {

        /// <summary>
        /// Gets or sets the mime type of the certificate
        /// </summary>
        [XmlElement("type")]
        [Description("Mime type of certificate")]
        public FhirCode<String> Type { get; set; }

        /// <summary>
        /// Gets or sets the blob of the certificate
        /// </summary>
        [XmlElement("blob")]
        [Description("Actual certificate")]
        public FhirBase64Binary Blob { get; set; }

    }
}
