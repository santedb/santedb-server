using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Security
{
    /// <summary>
    /// Represents an ADO Session
    /// </summary>
    public class AdoSecuritySession : GenericSession, ISession
    {

        /// <summary>
        /// Represents the session key
        /// </summary>
        internal Guid Key { get; private set; }

        /// <summary>
        /// Creates a new ADO Session
        /// </summary>
        internal AdoSecuritySession(Guid key, byte[] id, byte[] refreshToken, DateTimeOffset notBefore, DateTimeOffset notAfter) : base(id, refreshToken, notBefore, notAfter)
        {
            this.Key = key;
        }
    }
}
