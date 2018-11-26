using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Local material persistence service
    /// </summary>
    public class LocalManufacturedMaterialRepository : GenericLocalNullifiedRepository<ManufacturedMaterial>
    {
        protected override string QueryPolicy => PermissionPolicyIdentifiers.QueryMaterials;
        protected override string ReadPolicy => PermissionPolicyIdentifiers.ReadMaterials;
        protected override string WritePolicy => PermissionPolicyIdentifiers.WriteMaterials;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.DeleteMaterials;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.WriteMaterials;
    }
}
