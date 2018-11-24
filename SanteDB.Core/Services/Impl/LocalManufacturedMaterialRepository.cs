using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Local material persistence service
    /// </summary>
    public class LocalManufacturedMaterialRepository : GenericLocalEntityRepository<ManufacturedMaterial>
    {
        protected override string QueryPolicy => PermissionPolicyIdentifiers.QueryMaterials;
        protected override string ReadPolicy => PermissionPolicyIdentifiers.ReadMaterials;
        protected override string WritePolicy => PermissionPolicyIdentifiers.WriteMaterials;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.DeleteMaterials;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.WriteMaterials;
    }
}
