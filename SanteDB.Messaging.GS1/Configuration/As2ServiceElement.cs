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
 * Date: 2018-6-22
 */
using SanteDB.Core.Http;
using System;
using System.Configuration;
using System.Xml.Serialization;

namespace SanteDB.Messaging.GS1.Configuration
{

    /// <summary>
    /// AS2 Service configuration
    /// </summary>
    [XmlType(nameof(As2ServiceElement), Namespace = "http://santedb.org/configuration/gs1")]
    public class As2ServiceElement : ServiceClientDescription
    {

        /// <summary>
        /// AS2 service configuration
        /// </summary>
        public As2ServiceElement()
        {

        }

        /// <summary>
        /// Use AS2 standard mime based encoding
        /// </summary>
        [XmlAttribute("useAs2MimeEncoding")]
        public bool UseAS2MimeEncoding {
            get;set;
        }

        /// <summary>
        /// Gets or sets the username
        /// </summary>
        [XmlAttribute("userName")]
        public String UserName {
            get;set;
        }

        /// <summary>
        /// Gets or sets the password
        /// </summary>
        [ConfigurationProperty("password"), XmlAttribute("password")]
        public String Password {
            get;set;
        }

        /// <summary>
        /// Configuration property for trusted cert
        /// </summary>
        [ConfigurationProperty("trustedCert"), XmlAttribute("trustedCert")]
        public String TrustedCertificate {
            get;set;
        }


    }
}
