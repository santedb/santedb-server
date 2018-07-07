using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services.Security;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.AMI.ResourceHandler
{
    /// <summary>
    /// A resource handler which handles security roles
    /// </summary> 
    public class SecurityRoleResourceHandler : SecurityEntityResourceHandler<SecurityRole>
    {

        /// <summary>
        /// Get the type
        /// </summary>
        public override Type Type => typeof(SecurityRoleInfo);

        /// <summary>
        /// Create the specified security role
        /// </summary>
        public override object Create(object data, bool updateIfExists)
        {
            var retVal = base.Create(data, updateIfExists) as SecurityRoleInfo;
            var td = data as SecurityRoleInfo;
            
            if(td.Users.Count > 0)
            {
                ApplicationContext.Current.GetService<IRoleProviderService>().AddUsersToRoles(td.Users.ToArray(), new string[] { td.Entity.Name }, AuthenticationContext.Current.Principal);
            }
            return new SecurityRoleInfo(retVal.Entity);
        }

    }
}
