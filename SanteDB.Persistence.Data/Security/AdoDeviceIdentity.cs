/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * User: fyfej
 * Date: 2022-9-7
 */
using SanteDB.Core.i18n;
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
                throw new LockedIdentityAuthenticationException(device.Lockout.Value);
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