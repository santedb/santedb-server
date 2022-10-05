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
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;

namespace SanteDB.Server.AdminConsole.Security
{
    /// <summary>
    /// Represents an HTTP BASIC identity provider
    /// </summary>
    /// <remarks>This is used when no local storage is in the application context and identity must be used with HTTP basic server</remarks>
    [ExcludeFromCodeCoverage]
    public class HttpBasicIdentityProvider : IIdentityProviderService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public String ServiceName => "HTTP Basic Identity Provider";
#pragma warning disable CS0067
        /// <summary>
        /// Fired when the basic identity provider has authenticated successfully
        /// </summary>
        public event EventHandler<AuthenticatedEventArgs> Authenticated;

        /// <summary>
        /// Fired when the basic identity provider is about to authenticate
        /// </summary>
        public event EventHandler<AuthenticatingEventArgs> Authenticating;
#pragma warning restore CS0067
        /// <summary>
        /// Adds a claim to the current identity
        /// </summary>
        /// <param name="userName">The user identity to add the claim to</param>
        /// <param name="claim">The claim</param>
        /// <param name="principal">The princiapl asserting the claim</param>
        /// <remarks>Not implemented</remarks>
        public void AddClaim(string userName, IClaim claim, IPrincipal principal, TimeSpan? expiry = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Authenticate the specified user against the configured server
        /// </summary>
        /// <param name="userName">The username to authentcate with</param>
        /// <param name="password">The password to authenticate</param>
        /// <returns>The principal created as a result of authentication</returns>
        public IPrincipal Authenticate(string userName, string password)
        {
            return this.Authenticate(userName, password, null);
        }

        /// <summary>
        /// Authenticates a user against the HTTP basic provider using a TFA secret (OTP)
        /// </summary>
        /// <param name="userName">The username</param>
        /// <param name="password">The password</param>
        /// <param name="tfaSecret">The one time password </param>
        /// <returns>The principal created as a result of </returns>
        public IPrincipal Authenticate(string userName, string password, string tfaSecret)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Change the user's password
        /// </summary>
        public void ChangePassword(string userName, string newPassword, IPrincipal principal)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Creates an identity
        /// </summary>
        public IIdentity CreateIdentity(string userName, string password, IPrincipal principal)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Delete an identity
        /// </summary>
        public void DeleteIdentity(string userName, IPrincipal principal)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Generate a TFA secret
        /// </summary>
        public string GenerateTfaSecret(string userName)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<IClaim> GetClaims(string userName)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get identity details without authenticating
        /// </summary>
        /// <param name="userName">The user name to gather identity for</param>
        /// <returns>The constructed identity</returns>
        public IIdentity GetIdentity(string userName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get identity details without authenticating
        /// </summary>
        /// <param name="sid">The security identifier</param>
        /// <returns>The constructed identity</returns>
        public IIdentity GetIdentity(Guid sid)
        {
            throw new NotImplementedException();
        }

        public Guid GetSid(string name)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Re-authenticates (extends) a session
        /// </summary>
        public IPrincipal ReAuthenticate(IPrincipal principal)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Remove a claim from the user
        /// </summary>
        public void RemoveClaim(string userName, string claimType, IPrincipal principal)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Set the lockout of the user
        /// </summary>
        public void SetLockout(string userName, bool lockout, IPrincipal principal)
        {
            throw new NotSupportedException();
        }
    }
}
