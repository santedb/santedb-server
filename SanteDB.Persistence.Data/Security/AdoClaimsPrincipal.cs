using SanteDB.Core.Security.Claims;
using System;
using System.Collections.Generic;
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

        public IEnumerable<IClaim> FindAll(string santeDBDeviceIdentifierClaim)
        {
            throw new NotImplementedException();
        }

        public IClaim FindFirst(string santeDBDeviceIdentifierClaim)
        {
            throw new NotImplementedException();
        }

        public bool HasClaim(Func<IClaim, bool> predicate)
        {
            throw new NotImplementedException();
        }

        public bool IsInRole(string role)
        {
            throw new NotImplementedException();
        }

        public bool TryGetClaimValue(string claimType, out string value)
        {
            throw new NotImplementedException();
        }
    }
}
