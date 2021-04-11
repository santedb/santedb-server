using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Security
{
    /// <summary>
    /// Represents an ADO Session
    /// </summary>
    internal class AdoSecuritySession : GenericSession, ISession
    {

        /// <summary>
        /// Represents the session key
        /// </summary>
        internal Guid Key { get; private set; }

        /// <summary>
        /// Creates a new ADO Session
        /// </summary>
        internal AdoSecuritySession(Guid key, byte[] id, byte[] refreshToken, DateTimeOffset notBefore, DateTimeOffset notAfter, IClaim[] claims) : base(id, refreshToken, notBefore, notAfter, claims)
        {
            this.Key = key;
        }

        /// <summary>
        /// Create security session
        /// </summary>
        internal AdoSecuritySession(DbSession dbSession) : this(dbSession.Key, null, null, dbSession.NotBefore, dbSession.NotAfter, null)
        {
        }
    }
}
