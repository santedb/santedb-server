using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services.Security;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Local security application repository
    /// </summary>
    public class LocalSecurityApplicationRepository : GenericLocalSecurityRepository<SecurityApplication>
    {
        protected override string WritePolicy => PermissionPolicyIdentifiers.CreateApplication;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.CreateApplication;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.CreateApplication;

    }
}
