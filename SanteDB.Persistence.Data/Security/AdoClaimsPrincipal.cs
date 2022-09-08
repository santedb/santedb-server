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
using SanteDB.Core.i18n;
using SanteDB.Core.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Persistence.Data.Security
{
    /// <summary>
    /// ADO Claims principal
    /// </summary>
    internal class AdoClaimsPrincipal : IClaimsPrincipal
    {
        // The identity of the principal
        private AdoIdentity m_identity;

        /// <summary>
        /// Create a claims principal
        /// </summary>
        /// <param name="focalIdentity">The focal identity (the primary identity)</param>
        internal AdoClaimsPrincipal(AdoIdentity focalIdentity)
        {
            this.m_identity = focalIdentity;
        }

        /// <summary>
        /// Get claims
        /// </summary>
        public IEnumerable<IClaim> Claims => this.m_identity.Claims;

        /// <summary>
        /// Get all identities
        /// </summary>
        public IClaimsIdentity[] Identities => new IClaimsIdentity[] { this.m_identity };

        /// <summary>
        /// Get the primary identity
        /// </summary>
        public IIdentity Identity => this.m_identity;

        /// <summary>
        /// Add an identity to this principal
        /// </summary>
        public void AddIdentity(IIdentity identity)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Find all claims
        /// </summary>
        public IEnumerable<IClaim> FindAll(string claimType) => this.Claims.Where(o => o.Type == claimType);

        /// <summary>
        /// Find the first claim
        /// </summary>
        public IClaim FindFirst(string claimType) => this.FindAll(claimType).FirstOrDefault();

        /// <summary>
        /// True if the objet has a claim
        /// </summary>
        public bool HasClaim(Func<IClaim, bool> predicate)
        {
            return this.Claims.Any(predicate);
        }

        /// <summary>
        /// Is the user in role
        /// </summary>
        public bool IsInRole(string role)
        {
            if (String.IsNullOrEmpty(role))
            {
                throw new ArgumentNullException(nameof(role));
            }
            return this.Claims.Any(c => c.Type == SanteDBClaimTypes.DefaultRoleClaimType && c.Value.Equals(role, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Try to get the specified claim value
        /// </summary>
        public bool TryGetClaimValue(string claimType, out string value)
        {
            value = this.FindFirst(claimType)?.Value;
            return !String.IsNullOrEmpty(value);
        }
    }
}