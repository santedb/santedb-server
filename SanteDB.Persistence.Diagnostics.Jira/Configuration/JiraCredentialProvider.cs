/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 */
using SanteDB.Core.Http;
using SanteDB.Persistence.Diagnostics.Jira.Model;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;

namespace SanteDB.Persistence.Diagnostics.Jira.Configuration
{
    /// <summary>
    /// Credential provider for JIRA
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class JiraCredentialProvider : ICredentialProvider
    {
        public const string UserNameConfigurationProperty = "username";
        public const string PasswordConfigurationProperty = "password";
        private JiraAuthenticationResponse m_authentication;

        /// <summary>
        /// Authenticate the JIRA client
        /// </summary>
        public RestRequestCredentials Authenticate(IRestClient context)
        {
            var credentialProperty = context.Description.Binding.Security.CredentialProviderParameters;
            if (!credentialProperty.TryGetValue(UserNameConfigurationProperty, out string userName) || !credentialProperty.TryGetValue(PasswordConfigurationProperty, out string password))
            {
                throw new InvalidOperationException("Endpoint configuraiton is missing username and password property");
            }

            var result = context.Post<JiraAuthenticationRequest, JiraAuthenticationResponse>("auth/1/session", "application/json", new JiraAuthenticationRequest(userName, password));
            this.m_authentication = result;
            return new JiraCredentials(this.m_authentication);
        }

        /// <summary>
        /// Get current credentials
        /// </summary>
        public RestRequestCredentials GetCredentials(IRestClient context) => this.Authenticate(context);

        /// <summary>
        /// Not supported to get credentials on this endpoint via principal
        /// </summary>
        public RestRequestCredentials GetCredentials(IPrincipal principal)
        {
            throw new NotImplementedException();
        }

    }
}
