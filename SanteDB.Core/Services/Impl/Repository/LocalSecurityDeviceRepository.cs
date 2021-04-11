﻿/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using SanteDB.Core;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using System;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Local security device repository
    /// </summary>
    public class LocalSecurityDeviceRepository : GenericLocalSecurityRepository<SecurityDevice>
    {

        // ID Provider
        private IDeviceIdentityProviderService m_identityProvider;

        /// <summary>
        /// Security device repository
        /// </summary>
        public LocalSecurityDeviceRepository(IPrivacyEnforcementService privacyService, IDeviceIdentityProviderService identityProvider) : base(privacyService)
        {
            this.m_identityProvider = identityProvider;
        }

        protected override string WritePolicy => PermissionPolicyIdentifiers.CreateDevice;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.CreateDevice;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.CreateDevice;

        /// <summary>
        /// Insert the device
        /// </summary>
        public override SecurityDevice Insert(SecurityDevice data)
        {

            // Create the identity
            var id = this.m_identityProvider.CreateIdentity(data.Name, data.DeviceSecret, AuthenticationContext.Current.Principal);

            // Now ensure local db record exists
            Guid sid = Guid.Empty;
            if (id is IClaimsIdentity cIdentity)
            {
                var sidClaim = cIdentity.FindFirst(SanteDBClaimTypes.Sid);
                Guid.TryParse(sidClaim.Value, out sid);
            }
            else
            {
                sid = this.m_identityProvider.GetSid(id.Name);
            }

            data.Key = sid;
            data.DeviceSecret = null;
            return base.Save(data);
        }

        /// <summary>
        /// Save the security device
        /// </summary>
        public override SecurityDevice Save(SecurityDevice data)
        {
            if (!String.IsNullOrEmpty(data.DeviceSecret))
            {
                this.m_identityProvider.ChangeSecret(data.Name, data.DeviceSecret, AuthenticationContext.Current.Principal);
                data.DeviceSecret = null;
            }
            return base.Save(data);
        }
    }
}
