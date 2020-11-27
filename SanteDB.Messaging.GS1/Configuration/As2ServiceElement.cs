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
using SanteDB.Core.Configuration;
using SanteDB.Core.Http;
using SanteDB.Core.Security.Configuration;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Xml.Serialization;

namespace SanteDB.Messaging.GS1.Configuration
{

    /// <summary>
    /// AS2 Service configuration
    /// </summary>
    [XmlType(nameof(As2ServiceElement), Namespace = "http://santedb.org/configuration")]
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
        [DisplayName("Use AS.2 MIME"), Description("When true, instructs the service to use AS.2 mime encoded messages instead of REST messages")]
        public bool UseAS2MimeEncoding {
            get;set;
        }

        /// <summary>
        /// Gets or sets the username
        /// </summary>
        [XmlAttribute("userName")]
        [DisplayName("User Name"), Description("The user name to use when connecting to the GS1 broker")]
        public String UserName {
            get;set;
        }

        /// <summary>
        /// Gets or sets the password
        /// </summary>
        [XmlAttribute("password")]
        [PasswordPropertyTextAttribute, DisplayName("Password"), Description("The password to use when connecting to the GS1 broker")]
        public String Password {
            get;set;
        }

        /// <summary>
        /// Configuration property for trusted cert
        /// </summary>
        [XmlElement("clientCertificate")]
        [TypeConverter(typeof(ExpandableObjectConverter)), DisplayName("Client Cert"), Description("If using client certificates to communicate with GS1 broker, the client certificate configuration")]
        public X509ConfigurationElement ClientCertificate {
            get;set;
        }


    }
}
