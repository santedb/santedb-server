using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services.Security;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            this.m_traceSource.TraceEvent(TraceEventType.Verbose, 0, "Creating user {0}", data);

            var iids = ApplicationContext.Current.GetService<IIdentityProviderService>();

            // Create the identity
            var id = iids.CreateIdentity(data.UserName, data.Password, AuthenticationContext.Current.Principal);

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
    }
}
