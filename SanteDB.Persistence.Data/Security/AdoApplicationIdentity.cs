using SanteDB.Core.i18n;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using SanteDB.Persistence.Data.Exceptions;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;

namespace SanteDB.Persistence.Data.Security
{
    /// <summary>
    /// Represents an identity for ADO authenticated application records
    /// </summary>
    internal sealed class AdoApplicationIdentity : AdoIdentity, IApplicationIdentity, IClaimsIdentity
    {
        // Application
        private readonly DbSecurityApplication m_application;

        /// <summary>
        /// Create a new application identity
        /// </summary>
        internal AdoApplicationIdentity(DbSecurityApplication application, String authenticationMethod) : base(application.PublicId, authenticationMethod, true)
        {
            // Has the user been locked since the session was established?
            if (application.Lockout > DateTimeOffset.Now)
            {
                throw new LockedIdentityAuthenticationException(application.Lockout.Value);
            }
            else if (application.ObsoletionTime.HasValue)
            {
                throw new InvalidIdentityAuthenticationException();
            }

            this.m_application = application;
            this.m_application.Secret = null;
            this.m_application.PublicSigningKey = null;
            this.InitializeClaims();
        }

        /// <summary>
        /// Create a new un-authenticated application identity
        /// </summary>
        internal AdoApplicationIdentity(DbSecurityApplication application) : base(application.PublicId, null, false)
        {
            this.m_application = application;
            this.InitializeClaims();
        }

        /// <summary>
        /// Initialize claims
        /// </summary>
        private void InitializeClaims()
        {
            this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.Sid, this.m_application.Key.ToString()));
            this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.SanteDBApplicationIdentifierClaim, this.m_application.Key.ToString()));
            this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.Actor, ActorTypeKeys.Application.ToString()));
        }

        /// <summary>
        /// Get the SID of this object
        /// </summary>
        internal override Guid Sid => this.m_application.Key;
    }
}