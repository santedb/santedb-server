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
                new SanteDBClaim(SanteDBClaimTypes.AuthenticationInstant, sessionInfo.NotBefore.ToString("o")),
                new SanteDBClaim(SanteDBClaimTypes.AuthenticationInstant, sessionInfo.NotAfter.ToString("o")),
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
        /// Find first claim matching
        /// </summary>
        internal IClaim FindFirst(string claimType) => this.Claims.FirstOrDefault(o => o.Type == claimType);

        /// <summary>
        /// Find all claims
        /// </summary>
        internal IEnumerable<IClaim> Find(string claimType) => this.Claims.Where(o => o.Type == claimType);
    }
}
