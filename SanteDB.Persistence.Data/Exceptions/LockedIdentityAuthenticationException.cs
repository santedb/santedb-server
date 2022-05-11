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

        /// <summary>
        /// Gets the time until lockout is cleared
        /// </summary>
        public TimeSpan TimeUntilLockout { get; }

        /// <summary>
        /// Create with time until lockout
        /// </summary>
        public LockedIdentityAuthenticationException(TimeSpan timeToUnlock) : base($"Account is locked for {timeToUnlock}")
        {
            this.TimeUntilLockout = timeToUnlock;
        }

        /// <summary>
        /// Create with absolute lockout time
        /// </summary>
        public LockedIdentityAuthenticationException(DateTimeOffset lockoutTime) : this(lockoutTime.Subtract( DateTimeOffset.Now))
        { }

    }
}