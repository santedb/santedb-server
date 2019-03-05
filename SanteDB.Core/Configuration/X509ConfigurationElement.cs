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
using SanteDB.Core.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
{
    /// <summary>
    /// Represents a base configuration for a X509 cert
    /// </summary>
    [XmlType(nameof(X509ConfigurationElement), Namespace = "http://santedb.org/configuration")]
    public class X509ConfigurationElement
    {

        // Certificate
        private X509Certificate2 m_certificate;

        /// <summary>
        /// The find type
        /// </summary>
        [XmlAttribute("findType")]
        public X509FindType FindType { get; set; }

        /// <summary>
        /// The store name
        /// </summary>
        [XmlAttribute("storeName")]
        public StoreName StoreName { get; set; }

        /// <summary>
        /// The store location
        /// </summary>
        [XmlAttribute("storeLocation")]
        public StoreLocation StoreLocation { get; set; }

        /// <summary>
        /// The find value
        /// </summary>
        [XmlAttribute("findValue")]
        public string FindValue { get; set; }

        /// <summary>
        /// Get the certificate
        /// </summary>
        public X509Certificate2 GetCertificate()
        {
            if(this.m_certificate != null)
                this.m_certificate = X509CertificateUtils.FindCertificate(this.FindType, this.StoreLocation, this.StoreName, this.FindValue);
            return this.m_certificate;
        }
    }
}