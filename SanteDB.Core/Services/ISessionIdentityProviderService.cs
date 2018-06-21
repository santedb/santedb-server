using MARC.HI.EHRS.SVC.Core.Services.Security;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Services
{
    /// <summary>
    /// Represents a session identity service that can provide identities
    /// </summary>
    public interface ISessionIdentityProviderService : IIdentityProviderService
    {

        /// <summary>
        /// Authenticate based on session
        /// </summary>
        /// <param name="session">The session which is being sought for authentication</param>
        /// <returns>The authenticated principal</returns>
        IPrincipal Authenticate(ISession session);

    }
}
