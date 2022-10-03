/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 * User: fyfej
 * Date: 2022-5-30
 */
using SanteDB.Core.Http;
using SanteDB.Core.Security;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;

namespace SanteDB.Server.AdminConsole.Security
{
    /// <summary>
    /// Credential providerwhich will identify this application
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class OAuth2CredentialProvider : ICredentialProvider
    {
        #region ICredentialProvider implementation
        /// <summary>
        /// Gets or sets the credentials which are used to authenticate
        /// </summary>
        /// <returns>The credentials.</returns>
        /// <param name="context">Context.</param>
        public Credentials GetCredentials(IRestClient context)
        {
            // return this application's credentials
            return new OAuthTokenServiceCredentials(AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Authentication request is required
        /// </summary>
        /// <param name="context">Context.</param>
        public Credentials Authenticate(IRestClient context)
        {
            // return this application's credentials
            return new OAuthTokenServiceCredentials(AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Get oauth credentials
        /// </summary>
        public Credentials GetCredentials(IPrincipal principal)
        {
            // return this application's credentials
            return new OAuthTokenServiceCredentials(AuthenticationContext.Current.Principal);
        }
        #endregion
    }
}

