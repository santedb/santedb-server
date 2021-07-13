using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Server.Core.Security.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Policy enforcement service
    /// </summary>
    public class DefaultPolicyEnforcementService : IPolicyEnforcementService
    {

        /// <summary>
        /// Default policy enforcement
        /// </summary>
        public string ServiceName => "Default Policy Enforcement Service";

        /// <summary>
        /// Demand the policy
        /// </summary>
        public void Demand(string policyId)
        {
            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, policyId, AuthenticationContext.Current.Principal).Demand();
        }

        /// <summary>
        /// Demand policy enforcement
        /// </summary>
        public void Demand(string policyId, IPrincipal principal)
        {
            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, policyId, principal).Demand();
        }

        /// <summary>
        /// Soft demand
        /// </summary>
        public bool SoftDemand(string policyId, IPrincipal principal)
        {
            return new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, policyId).DemandSoft() == SanteDB.Core.Model.Security.PolicyGrantType.Grant;

        }
    }
}
