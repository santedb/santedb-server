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
            if(String.IsNullOrEmpty(role))
            {
                throw new ArgumentNullException(nameof(role), ErrorMessages.ERR_ARGUMENT_NULL);
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
