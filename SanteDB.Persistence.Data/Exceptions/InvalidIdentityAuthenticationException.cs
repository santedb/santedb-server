using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Exceptions
{
    /// <summary>
    /// Indicates a device is not active
    /// </summary>
    internal sealed class InvalidIdentityAuthenticationException : System.Security.Authentication.AuthenticationException
    {
    }
}