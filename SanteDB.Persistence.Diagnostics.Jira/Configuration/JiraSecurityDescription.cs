/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using SanteDB.Core.Http;
using SanteDB.Core.Http.Description;

namespace SanteDB.Persistence.Diagnostics.Jira.Configuration
{
    /// <summary>
    /// Represents a security description for JIRA
    /// </summary>
    internal class JiraSecurityDescription : IRestClientSecurityDescription
    {

        /// <summary>
        /// Gets or sets the authentication realm
        /// </summary>
        public string AuthRealm
        {
            get
            {
                return "*";
            }
        }

        /// <summary>
        /// Gets the certificate validator
        /// </summary>
        public ICertificateValidator CertificateValidator
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the certificate description
        /// </summary>
        public IRestClientCertificateDescription ClientCertificate
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Credential provider
        /// </summary>
        public ICredentialProvider CredentialProvider
        {
            get
            {
                return null;
            }
        }
        
        /// <summary>
        /// Gets the security scheme
        /// </summary>
        public SecurityScheme Mode
        {
            get
            {
                return SecurityScheme.Bearer;
            }
        }

        /// <summary>
        /// Preemtive authentication
        /// </summary>
        public bool PreemptiveAuthentication {  get { return true; } set { } }
    }
}