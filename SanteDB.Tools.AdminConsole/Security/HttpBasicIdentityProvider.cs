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
 * Date: 2018-10-24
 */
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using System;
using System.Security.Claims;
using System.Security.Principal;

namespace SanteDB.Tools.AdminConsole.Security
{
    /// <summary>
    /// Represents an HTTP BASIC identity provider
    /// </summary>
    public class HttpBasicIdentityProvider : IIdentityProviderService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public String ServiceName => "HTTP Basic Identity Provider";

        public event EventHandler<AuthenticatedEventArgs> Authenticated;
        public event EventHandler<AuthenticatingEventArgs> Authenticating;

        public void AddClaim(string userName, IClaim claim, IPrincipal principal)
        {
            throw new NotImplementedException();
        }

        public IPrincipal Authenticate(string userName, string password)
        {
            return this.Authenticate(userName, password);
        }

        public IPrincipal Authenticate(string userName, string password, string tfaSecret)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity(userName), new Claim[] { new Claim("passwd", password), new Claim(SanteDBClaimTypes.SanteDBTfaSecretClaim, tfaSecret) }));
        }

        public void ChangePassword(string userName, string newPassword, IPrincipal principal)
        {
            throw new NotImplementedException();
        }

        public IIdentity CreateIdentity(string userName,  string password, IPrincipal principal)
        {
            throw new NotImplementedException();
        }

        public void DeleteIdentity(string userName, IPrincipal principal)
        {
            throw new NotImplementedException();
        }

        public string GenerateTfaSecret(string userName)
        {
            throw new NotImplementedException();
        }

        public IIdentity GetIdentity(string userName)
        {
            throw new NotImplementedException();
        }

        public IPrincipal ReAuthenticate(IPrincipal principal)
        {
            throw new NotImplementedException();
        }

        public void RemoveClaim(string userName, string claimType, IPrincipal principal)
        {
            throw new NotImplementedException();
        }

        public void SetLockout(string userName, bool lockout, IPrincipal principal)
        {
            throw new NotImplementedException();
        }
    }
}
