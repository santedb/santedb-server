﻿using SanteDB.Core.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Security
{
    /// <summary>
    /// Base class for identities in the ADO provider
    /// </summary>
    internal abstract class AdoIdentity : IClaimsIdentity
    {

        // Claims made about the user
        private readonly ICollection<IClaim> m_claims = new List<IClaim>();

        /// <summary>
        /// Creates a new ADO identity
        /// </summary>
        protected AdoIdentity(String name, String authenticationMethod, bool isAuthenticated)
        {
            this.AuthenticationType = authenticationMethod;
            this.Name = name;
            this.IsAuthenticated = isAuthenticated;
            this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.Name, name));
            if (!String.IsNullOrEmpty(this.AuthenticationType))
            {
                this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.AuthenticationType, this.AuthenticationType));
            }
        }

        /// <summary>
        /// Gets the SID
        /// </summary>
        internal abstract Guid Sid { get; }

        /// <summary>
        /// Gets the method of authentication
        /// </summary>
        public string AuthenticationType { get; }

        /// <summary>
        /// Get whether the authentication occurred or this was simply loaded from database
        /// </summary>
        public bool IsAuthenticated { get; }

        /// <summary>
        /// Gets the name of this identity
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Claims for the user
        /// </summary>
        public IEnumerable<IClaim> Claims => this.m_claims;

        /// <summary>
        /// Add a claim to the user object
        /// </summary>
        internal void AddClaim(IClaim claim)
        {
            this.m_claims.Add(claim);
        }

        /// <summary>
        /// Add claim to the identity
        /// </summary>
        internal void AddClaims(IEnumerable<IClaim> claims)
        {
            foreach (var c in claims)
            {
                this.m_claims.Add(c);
            }
        }

        /// <summary>
        /// Find all claims matching the specified type
        /// </summary>
        public virtual IEnumerable<IClaim> FindAll(string claimType) => this.m_claims.Where(o => o.Type.Equals(claimType, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Find the first instance of a claim
        /// </summary>
        public virtual IClaim FindFirst(string claimType) => this.m_claims.FirstOrDefault(o => o.Type.Equals(claimType, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Remove a claim
        /// </summary>
        internal virtual void RemoveClaim(IClaim claim)
        {
            this.m_claims.Remove(claim);
        }
    }
}
