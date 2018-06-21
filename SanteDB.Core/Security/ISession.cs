using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Security
{
    /// <summary>
    /// Represents session information related to a user
    /// </summary>
    public interface ISession 
    {

        /// <summary>
        /// Gets the identifier of the session
        /// </summary>
        byte[] Id { get; }

        /// <summary>
        /// Gets the time the session was established
        /// </summary>
        DateTimeOffset NotBefore { get; }

        /// <summary>
        /// Gets the time the session expires
        /// </summary>
        DateTimeOffset NotAfter { get; }

        /// <summary>
        /// Gets the refresh token
        /// </summary>
        byte[] RefreshToken { get; }

    }

    /// <summary>
    /// Represents a generic session
    /// </summary>
    public class GenericSession : ISession
    {
        /// <summary>
        /// Creates a generic session for the user
        /// </summary>
        /// <param name="id">The token identifier for the session</param>
        /// <param name="refreshToken">The token which can be used to extend the session</param>
        /// <param name="notBefore">Indicates a not-before time</param>
        /// <param name="notAfter">Indicates a not-after time</param>
        public GenericSession(byte[] id, byte[] refreshToken, DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            this.Id = id;
            this.RefreshToken = refreshToken;
            this.NotBefore = notBefore;
            this.NotAfter = notAfter;
        }
        /// <summary>
        /// Gets the unique token identifier for the session
        /// </summary>
        public byte[] Id { get; private set; }

        /// <summary>
        /// Gets the issuance time
        /// </summary>
        public DateTimeOffset NotAfter { get; private set; }

        /// <summary>
        /// Get the expiration time
        /// </summary>
        public DateTimeOffset NotBefore { get; private set; }

        /// <summary>
        /// Gets the refresh token
        /// </summary>
        public byte[] RefreshToken { get; private set; }

    }
}
