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
 * Date: 2022-9-7
 */
using SanteDB.Core.Security.Claims;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Persistence.Data.Security
{
    /// <summary>
    /// Represents a session principal from an ADO session
    /// </summary>
    internal class AdoSessionPrincipal : IClaimsPrincipal
    {

        // Session information
        private AdoSecuritySession m_session;

        // Claims
        private IEnumerable<IClaim> m_claims;

        /// <summary>
        /// Creates anew session principal based on a database session
        /// </summary>
        public AdoSessionPrincipal(AdoSecuritySession session, IEnumerable<IClaimsIdentity> identities)
        {
            this.m_session = session;
            this.Identities = identities.ToArray();
            this.m_claims = session.Claims.Union(identities.SelectMany(i => i.Claims)).ToList();
        }

        /// <summary>
        /// Get all claims for this principal
        /// </summary>
        public IEnumerable<IClaim> Claims => this.m_session.Claims.Union(this.Identities.SelectMany(r=>r.Claims));

        /// <summary>
        /// Get all identities associated with this session
        /// </summary>
        public IClaimsIdentity[] Identities { get; }

        /// <summary>
        /// Get the primary identity
        /// </summary>
        public IIdentity Identity => this.Identities[0];

        /// <summary>
        /// Add an identity to this sesison
        /// </summary>
        public void AddIdentity(IIdentity identity)
        {
            throw new NotSupportedException("You cannot add an identity to an existing session. Create a new session and then call Authenticate on the ISessionIdentityProviderService.");
        }

        /// <summary>
        /// Find all claims 
        /// </summary>
        /// <remarks>
        /// Searchs the claims on this session as well as any subordinate claims on identities
        /// </remarks>
        public IEnumerable<IClaim> FindAll(string claimType)
        {
            return this.Claims.Where(o => o.Type == claimType);
        }

        /// <summary>
        /// Find the first object
        /// </summary>
        public IClaim FindFirst(string claimType) => this.FindAll(claimType).FirstOrDefault();

        /// <summary>
        /// True if the object has the specified claim
        /// </summary>
        public bool HasClaim(Func<IClaim, bool> predicate) => this.Claims.Any(predicate);

        /// <summary>
        /// True if the session has a role
        /// </summary>
        public bool IsInRole(string role) => this.Claims.Any(o => o.Type == SanteDBClaimTypes.DefaultRoleClaimType && o.Value.Equals(role, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Try to get the specified claim value
        /// </summary>
        public bool TryGetClaimValue(string claimType, out string value)
        {
            value = this.Claims.FirstOrDefault(o => o.Type == claimType)?.Value;
            return String.IsNullOrEmpty(claimType);
        }
    }
}
