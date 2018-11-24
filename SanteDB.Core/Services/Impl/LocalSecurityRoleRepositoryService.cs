using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Represents a local security role service
    /// </summary>
    public class LocalSecurityRoleRepositoryService : GenericLocalSecurityRepository<SecurityRole>
    {
        protected override string WritePolicy => PermissionPolicyIdentifiers.CreateRoles;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.AlterRoles;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.AlterRoles;


    }
}
