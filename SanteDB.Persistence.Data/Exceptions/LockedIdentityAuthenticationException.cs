using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Exceptions
{
    /// <summary>
    /// Indicates an identity is locked
    /// </summary>
    internal sealed class LockedIdentityAuthenticationException : System.Security.Authentication.AuthenticationException
    {
    }
}