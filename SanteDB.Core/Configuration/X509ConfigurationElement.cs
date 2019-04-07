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
using System.ComponentModel;
using System.Drawing.Design;
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
        [DisplayName("Certificate Search")]
        [Description("Identifies the algorithm to use to locate the security certificate")]
        public X509FindType FindType { get; set; }

        /// <summary>
        /// The store name
        /// </summary>
        [XmlAttribute("storeName")]
        [DisplayName("X509 Store")]
        [Description("Identifies the secure X.509 certificate store to search")]
        public StoreName StoreName { get; set; }

        /// <summary>
        /// The store location
        /// </summary>
        [XmlAttribute("storeLocation")]
        [DisplayName("X509 Location")]
        [Description("Identifies the location of the X.509 certificate store to load from")]
        public StoreLocation StoreLocation { get; set; }

        /// <summary>
        /// Whether the find type was provided
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public bool FindTypeSpecified { get; set; }

        /// <summary>
        /// Whether the store name was provided
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public bool StoreNameSpecified { get; set; }

        /// <summary>
        /// Whether the store location was provided
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public bool StoreLocationSpecified { get; set; }

        /// <summary>
        /// The find value
        /// </summary>
        [XmlAttribute("findValue")]
        [DisplayName("Certificate Identification")]
        [Description("The certificate value to look for in the secure store")]
        [ReadOnly(true)]
        public string FindValue { get; set; }

        /// <summary>
        /// Get the certificate
        /// </summary>
        [XmlIgnore]
        [Description("The X509 certificate to use")]
        [DisplayName("Certificate")]
        [Editor("SanteDB.Configuration.Editors.X509Certificate2Editor, SanteDB.Configuration, Version=1.0.0.0", typeof(UITypeEditor))]
        public X509Certificate2 Certificate
        {
            get => this.GetCertificate();
            set
            {
                if (value == null)
                    this.FindValue = null;
                else
                    switch(this.FindType)
                    {
                        case X509FindType.FindBySubjectName:
                            this.FindValue = value.Subject;
                            break;
                        case X509FindType.FindByThumbprint:
                            this.FindValue = value.Thumbprint;
                            break;
                        case X509FindType.FindBySerialNumber:
                            this.FindValue = value.SerialNumber;
                            break;
                        default:
                            this.FindType = X509FindType.FindByThumbprint;
                            this.FindValue = value.Thumbprint;
                            this.FindTypeSpecified = true;
                            break;
                    }
            }
        }

        /// <summary>
        /// Get the certificate
        /// </summary>
        private X509Certificate2 GetCertificate()
        {
            if(this.m_certificate != null)
                this.m_certificate = X509CertificateUtils.FindCertificate(this.FindType, this.StoreLocation, this.StoreName, this.FindValue);
            return this.m_certificate;
        }
    }
}