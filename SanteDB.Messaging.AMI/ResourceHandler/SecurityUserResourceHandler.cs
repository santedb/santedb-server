using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services.Security;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Messaging.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.AMI.ResourceHandler
{
    /// <summary>
    /// Represents a resource handler that can handle security users
    /// </summary>
    public class SecurityUserResourceHandler : SecurityEntityResourceHandler<SecurityUser>, ILockableResourceHandler
    {

        /// <summary>
        /// Gets the type of object that is expected
        /// </summary>
        public override Type Type => typeof(SecurityUserInfo);


        /// <summary>
        /// Creates the specified user
        /// </summary>
        public override object Create(object data, bool updateIfExists)
        {
            var td = data as SecurityUserInfo;

            // Insert the user
            var retVal = base.Create(data, updateIfExists) as SecurityUserInfo;

            // User information to roles
            if (td.Roles.Count > 0)
                ApplicationContext.Current.GetService<IRoleProviderService>().AddUsersToRoles(new string[] { retVal.Entity.UserName }, td.Roles.ToArray(), AuthenticationContext.Current.Principal);

            return new SecurityUserInfo(retVal.Entity)
            {
                Roles = td.Roles
            };
        }

        /// <summary>
        /// Lock the specified user
        /// </summary>
        public object Lock(object key)
        {
            ApplicationContext.Current.GetService<ISecurityRepositoryService>().LockUser((Guid)key);
            return this.Get(key, Guid.Empty);
        }

        /// <summary>
        /// Unlock user
        /// </summary>
        public object Unlock(object key)
        {
            ApplicationContext.Current.GetService<ISecurityRepositoryService>().UnlockUser((Guid)key);
            return this.Get(key, Guid.Empty);
        }

        /// <summary>
        /// Override the update function
        /// </summary>
        public override object Update(object data)
        {
            var td = data as SecurityUserInfo;

            // Update the user
            if (td.PasswordOnly)
            {
                ApplicationContext.Current.GetService<IIdentityProviderService>().ChangePassword(td.Entity.UserName, td.Entity.Password, AuthenticationContext.Current.Principal);
                return null;
            }
            else
            {
                td.Entity.Password = null;
                var retVal = base.Update(data) as SecurityUserInfo;

                // Roles? We want to update
                if (td.Roles.Count > 0)
                {
                    var irps = ApplicationContext.Current.GetService<IRoleProviderService>();
                    // Remove the user from all roles
                    irps.RemoveUsersFromRoles(new string[] { retVal.Entity.UserName }, irps.GetAllRoles(), AuthenticationContext.Current.Principal);
                    irps.AddUsersToRoles(new string[] { retVal.Entity.UserName }, td.Roles.ToArray(), AuthenticationContext.Current.Principal);
                }

                return new SecurityUserInfo(retVal.Entity)
                {
                    Roles = td.Roles
                };

            }

        }
    }
}
