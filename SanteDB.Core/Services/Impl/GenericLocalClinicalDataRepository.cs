using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Security;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Represents generic local clinical data repository
    /// </summary>
    public class GenericLocalClinicalDataRepository<TModel> : GenericLocalNullifiedRepository<TModel> where TModel : IdentifiedData, IHasState
    {
        protected override string QueryPolicy => PermissionPolicyIdentifiers.QueryClinicalData;
        protected override string ReadPolicy => PermissionPolicyIdentifiers.ReadClinicalData;
        protected override string WritePolicy => PermissionPolicyIdentifiers.WriteClinicalData;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.DeleteClinicalData;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.WriteClinicalData;
    }
}