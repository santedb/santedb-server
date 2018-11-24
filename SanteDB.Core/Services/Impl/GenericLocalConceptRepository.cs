using SanteDB.Core.Model;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Generic local concept repository with sufficient permissions
    /// </summary>
    public class GenericLocalConceptRepository<TModel> : GenericLocalMetadataRepository<TModel>
        where TModel : IdentifiedData
    {
        
        protected override string QueryPolicy => PermissionPolicyIdentifiers.ReadMetadata;
        protected override string ReadPolicy => PermissionPolicyIdentifiers.ReadMetadata;
        protected override string WritePolicy => PermissionPolicyIdentifiers.AdministerConceptDictionary;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.AdministerConceptDictionary;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.AdministerConceptDictionary;

    }
}
