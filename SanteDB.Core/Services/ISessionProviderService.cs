using MARC.HI.EHRS.SVC.Core.Services.Security;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Services
{

    /// <summary>
    /// Represents a service which is responsible for the storage and retrieval of sessions
    /// </summary>
    public interface ISessionProviderService
    {

        /// <summary>
        /// Establishes a session for the specified principal
        /// </summary>
        /// <param name="principal">The principal for which the session is to be established</param>
        /// <param name="expiry">The time when the session is to expire</param>
        /// <param name="aud">The audience of the session</param>
        /// <returns>The session information that was established</returns>
        ISession Establish(ClaimsPrincipal principal, DateTimeOffset expiry, String aud);

        /// <summary>
        /// Authenticates the session identifier as evidence of session
        /// </summary>
        /// <param name="sessionToken">The session identiifer to be authenticated</param>
        /// <returns>The authenticated session from the session provider</returns>
        ISession Get(byte[] sessionToken);

        /// <summary>
        /// Extend the session with the specified refresh token
        /// </summary>
        /// <param name="refreshToken">The refresh token that will extend the session</param>
        /// <returns>The extended session</returns>
        ISession Extend(byte[] refreshToken);
    }
}
