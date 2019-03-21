/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Security.Services;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Security user repository
    /// </summary>
    public class LocalSecurityUserRepositoryService : GenericLocalSecurityRepository<SecurityUser>
    {

        protected override string WritePolicy => PermissionPolicyIdentifiers.CreateIdentity;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.AlterIdentity;

        /// <summary>
        /// Demand altering
        /// </summary>
        /// <param name="data"></param>
        public override void DemandAlter(object data)
        {
            var su = data as SecurityUser;
            if (!su.UserName.Equals(AuthenticationContext.Current.Principal.Identity.Name, StringComparison.OrdinalIgnoreCase))
                new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.AlterIdentity);
        }

        /// <summary>
        /// Insert the user
        /// </summary>
        public override SecurityUser Insert(SecurityUser data)
        {
            this.m_traceSource.TraceEvent(EventLevel.Verbose, "Creating user {0}", data);

            var iids = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();

            // Create the identity
            var id = iids.CreateIdentity(data.UserName,  data.Password, AuthenticationContext.Current.Principal);

            // Now ensure local db record exists
            int tr = 0;
            var retVal = this.FindFast(o => o.UserName == data.UserName, 0, 1, out tr, Guid.Empty).FirstOrDefault();
            if (retVal == null)
            {
                throw new InvalidOperationException("Could not find created user from identity provider");
            }
            else
            {
                // The identity provider only creates a minimal identity, let's beef it up
                retVal.Email = data.Email;
                retVal.EmailConfirmed = data.EmailConfirmed;
                retVal.InvalidLoginAttempts = data.InvalidLoginAttempts;
                retVal.LastLoginTime = data.LastLoginTime;
                retVal.Lockout = data.Lockout;
                retVal.PhoneNumber = data.PhoneNumber;
                retVal.PhoneNumberConfirmed = data.PhoneNumberConfirmed;
                retVal.SecurityHash = data.SecurityHash;
                retVal.TwoFactorEnabled = data.TwoFactorEnabled;
                retVal.UserPhoto = data.UserPhoto;
                retVal.UserClass = data.UserClass;
                base.Save(retVal);
            }
            
            return retVal;
        }

        /// <summary>
        /// Save the user credential
        /// </summary>
        public override SecurityUser Save(SecurityUser data)
        {
            if (!String.IsNullOrEmpty(data.Password))
            {
                ApplicationServiceContext.Current.GetService<IIdentityProviderService>().ChangePassword(data.UserName, data.Password, AuthenticationContext.Current.Principal);
            }
            return base.Save(data);
        }
    }
}
