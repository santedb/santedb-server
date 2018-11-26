using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Alter policies
    /// </summary>
    public class LocalSecurityPolicyRepository : GenericLocalSecurityRepository<SecurityPolicy>
    {

        protected override string WritePolicy => PermissionPolicyIdentifiers.AlterPolicy;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.AlterPolicy;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.AlterPolicy;

    }
}
