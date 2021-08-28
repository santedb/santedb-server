/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * User: fyfej
 * Date: 2021-8-27
 */
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
