using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
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
            this.m_device = device;
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
            this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.Actor, "DEVICE"));
        }

        /// <summary>
        /// Get the SID of this object
        /// </summary>
        internal override Guid Sid => this.m_device.Key;

    }
}
