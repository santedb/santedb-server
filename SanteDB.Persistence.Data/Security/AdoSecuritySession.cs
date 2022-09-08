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
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Security
{
    /// <summary>
    /// Represents an ADO Session
    /// </summary>
    internal class AdoSecuritySession : ISession
    {

        /// <summary>
        /// Represents the session key
        /// </summary>
        internal Guid Key { get;}

        /// <summary>
        /// Gets the identifier for the sesison
        /// </summary>
        public byte[] Id { get; }

        /// <summary>
        /// Not before
        /// </summary>
        public DateTimeOffset NotBefore { get; }

        /// <summary>
        /// Not after
        /// </summary>
        public DateTimeOffset NotAfter { get; }

        /// <summary>
        /// Gets the refresh token
        /// </summary>
        public byte[] RefreshToken { get; }

        /// <summary>
        /// Gets the claims for this session
        /// </summary>
        public IClaim[] Claims { get; }

        /// <summary>
        /// Creates a new ADO Session
        /// </summary>
        internal AdoSecuritySession(byte[] token, byte[] refreshToken, DbSession sessionInfo, IEnumerable<DbSessionClaim> claims)
        {
            var addlClaims = new List<IClaim>()
            {
                new SanteDBClaim(SanteDBClaimTypes.AuthenticationInstant, sessionInfo.NotBefore.ToUniversalTime().ToString("o")),
                new SanteDBClaim(SanteDBClaimTypes.Expiration, sessionInfo.NotAfter.ToUniversalTime().ToString("o")),
                new SanteDBClaim(SanteDBClaimTypes.SanteDBSessionIdClaim, sessionInfo.Key.ToString())
            };

            this.Claims = addlClaims.Union(claims.Select(o => new SanteDBClaim(o.ClaimType, o.ClaimValue))).ToArray();
            this.Key = sessionInfo.Key;
            this.Id = token;
            this.RefreshToken = refreshToken;
            this.NotBefore = sessionInfo.NotBefore;
            this.NotAfter = sessionInfo.NotAfter;
        }

        /// <summary>
        /// Copy an ADO session
        /// </summary>
        public AdoSecuritySession(AdoSecuritySession adoSession)
        {
            var addlClaims = new List<IClaim>()
            {
                new SanteDBClaim(SanteDBClaimTypes.AuthenticationInstant, adoSession.NotBefore.ToUniversalTime().ToString("o")),
                new SanteDBClaim(SanteDBClaimTypes.Expiration, adoSession.NotAfter.ToUniversalTime().ToString("o")),
                new SanteDBClaim(SanteDBClaimTypes.SanteDBSessionIdClaim, adoSession.Key.ToString())
            };

            this.Claims = addlClaims.Union(adoSession.Claims.Select(o => new SanteDBClaim(o.Type, o.Value))).ToArray();
            this.Key = adoSession.Key;
            this.Id = adoSession.Id;
            this.NotBefore = adoSession.NotBefore;
            this.NotAfter = adoSession.NotAfter;
        }

        /// <summary>
        /// Find first claim matching
        /// </summary>
        internal IClaim FindFirst(string claimType) => this.Claims.FirstOrDefault(o => o.Type == claimType);

        /// <summary>
        /// Find all claims
        /// </summary>
        internal IEnumerable<IClaim> Find(string claimType) => this.Claims.Where(o => o.Type == claimType);
    }
}
