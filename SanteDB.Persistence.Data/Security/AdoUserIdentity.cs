using SanteDB.Core.Model.Constants;
using SanteDB.Core.Security;
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
    /// Represents a claims identity which is based on a DbSecurityUser
    /// </summary>
    internal sealed class AdoUserIdentity : AdoIdentity
    {

        // The user which is stored in this identity
        private readonly DbSecurityUser m_securityUser;

        /// <summary>
        /// Creates a new user identity based on the user data
        /// </summary>
        /// <param name="userData">The user information from the authentication layer</param>
        /// <param name="authenticationMethod">The method used to authenticate (password, session, etc.)</param>
        internal AdoUserIdentity(DbSecurityUser userData, String authenticationMethod) : base(userData.UserName, authenticationMethod, true)
        {
            this.m_securityUser = userData;
            this.InitializeClaims();
        }

        /// <summary>
        /// The ADO user identity
        /// </summary>
        internal AdoUserIdentity(DbSecurityUser userData) : base(userData.UserName, null, false)
        {
            this.m_securityUser = userData;
            this.InitializeClaims();
        }

        /// <summary>
        /// Initialize the claims for this object
        /// </summary>
        private void InitializeClaims()
        {
            this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.NameIdentifier, this.m_securityUser.Key.ToString()));
            this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.Sid, this.m_securityUser.Key.ToString()));
            this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.Actor, this.m_securityUser.UserClass.ToString()));
            if (!String.IsNullOrEmpty(this.m_securityUser.Email)) {
                this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.Email, this.m_securityUser.Email));
            }
            if (!String.IsNullOrEmpty(this.m_securityUser.PhoneNumber)) {
                this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.Telephone, this.m_securityUser.PhoneNumber));
            }
        }

        /// <summary>
        /// Add role claims for the user authentication
        /// </summary>
        private void AddRoleClaims(IEnumerable<String> roleNames)
        {
            this.AddClaims(roleNames.Select(o=>new SanteDBClaim(SanteDBClaimTypes.DefaultRoleClaimType, o)));
        }

        /// <summary>
        /// Get the SID of this object
        /// </summary>
        internal override Guid Sid => this.m_securityUser.Key;

    }
}
