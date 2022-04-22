﻿using SanteDB.Core.i18n;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Exceptions;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace SanteDB.Persistence.Data.Security
{
    /// <summary>
    /// An ADO Device Identity
    /// </summary>
    internal class AdoDeviceIdentity : AdoIdentity, IDeviceIdentity
    {
        // Application
        private readonly DbSecurityDevice m_device;

        /// <summary>
        /// Create a new application identity
        /// </summary>
        internal AdoDeviceIdentity(DbSecurityDevice device, String authenticationMethod) : base(device.PublicId, authenticationMethod, true)
        {
            // Has the user been locked since the session was established?
            if (device.Lockout > DateTimeOffset.Now)
            {
                throw new LockedIdentityAuthenticationException();
            }
            else if (device.ObsoletionTime.HasValue)
            {
                throw new InvalidIdentityAuthenticationException();
            }

            this.m_device = device;
            this.m_device.DeviceSecret = null;
            this.InitializeClaims();
        }

        /// <summary>
        /// Create a new un-authenticated application identity
        /// </summary>
        internal AdoDeviceIdentity(DbSecurityDevice device) : base(device.PublicId, null, false)
        {
            this.m_device = device;
            this.InitializeClaims();
        }

        /// <summary>
        /// Initialize claims
        /// </summary>
        private void InitializeClaims()
        {
            this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.Sid, this.m_device.Key.ToString()));
            this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.SanteDBDeviceIdentifierClaim, this.m_device.Key.ToString()));
            this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.Actor, ActorTypeKeys.Device.ToString()));
        }

        /// <summary>
        /// Get the SID of this object
        /// </summary>
        internal override Guid Sid => this.m_device.Key;
    }
}