using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Provides operations for managing organizations.
    /// </summary>
    public class LocalProviderRepository : GenericLocalNullifiedRepository<Provider>
    {
        protected override string QueryPolicy => PermissionPolicyIdentifiers.ReadMetadata;
        protected override string ReadPolicy => PermissionPolicyIdentifiers.ReadMetadata;
        protected override string WritePolicy => PermissionPolicyIdentifiers.UnrestrictedMetadata;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.UnrestrictedMetadata;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.UnrestrictedMetadata;

    }
}
