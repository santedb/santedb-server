/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Security.Principal;

namespace SanteDB.Core.Security
{
    /// <summary>
    /// Local policy decision service
    /// </summary>
    [ServiceProvider("Default PDP Service")]
    public class DefaultPolicyDecisionService : IPolicyDecisionService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public String ServiceName => "Default PDP Decision Service";

        /// <summary>
        /// Get a policy decision 
        /// </summary>
        public PolicyDecision GetPolicyDecision(IPrincipal principal, object securable)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));
            else if (securable == null)
                throw new ArgumentNullException(nameof(securable));

            // We need to get the active policies for this
            var pip = ApplicationServiceContext.Current.GetService<IPolicyInformationService>();
            IEnumerable<IPolicyInstance> securablePolicies = pip.GetActivePolicies(securable),
                principalPolicies = pip.GetActivePolicies(principal);

            List<PolicyDecisionDetail> details = new List<PolicyDecisionDetail>();
            var retVal = new PolicyDecision(securable, details);

            foreach (var pol in securablePolicies)
            {
                // Get most restrictive from principal
                var rules = principalPolicies.Where(p => p.Policy.Oid.StartsWith(pol.Policy.Oid)).Select(o => o.Rule);
                PolicyGrantType rule = PolicyGrantType.Deny;
                if(rules.Any())
                    rule = rules.Min();

                // Rule for elevate can only be made when the policy allows for it & the principal is allowed
                if (rule == PolicyGrantType.Elevate &&
                    (!pol.Policy.CanOverride ||
                    principalPolicies.Any(o => o.Policy.Oid == PermissionPolicyIdentifiers.ElevateClinicalData && o.Rule == PolicyGrantType.Grant)))
                    rule = PolicyGrantType.Deny;

                details.Add(new PolicyDecisionDetail(pol.Policy.Oid, rule));
            }

            return retVal;
        }

        /// <summary>
        /// Get a policy outcome
        /// </summary>
        public PolicyGrantType GetPolicyOutcome(IPrincipal principal, string policyId)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));
            else if (String.IsNullOrEmpty(policyId))
                throw new ArgumentNullException(nameof(policyId));

            // Can we make this decision based on the claims? 
            if (principal is IClaimsPrincipal && (principal as IClaimsPrincipal).HasClaim(c => c.Type == SanteDBClaimTypes.SanteDBGrantedPolicyClaim && policyId.StartsWith(c.Value)))
                return PolicyGrantType.Grant;

            // Get the user object from the principal
            var pip = ApplicationServiceContext.Current.GetService<IPolicyInformationService>();

            // Policies
            var activePolicies = pip.GetActivePolicies(principal).Where(o => policyId.StartsWith(o.Policy.Oid));
            // Most restrictive
            IPolicyInstance policyInstance = null;
            foreach (var pol in activePolicies)
                if (policyInstance == null)
                    policyInstance = pol;
                else if (pol.Rule < policyInstance.Rule || // More restrictive
                    pol.Policy.Oid.Length > policyInstance.Policy.Oid.Length // More specific
                    )
                    policyInstance = pol;

            if (policyInstance == null)
            {
                // TODO: Configure OptIn or OptOut
                return PolicyGrantType.Deny;
            }
            else if (!policyInstance.Policy.CanOverride && policyInstance.Rule == PolicyGrantType.Elevate)
                return PolicyGrantType.Deny;
            else if (!policyInstance.Policy.IsActive)
                return PolicyGrantType.Grant;
            else if ((policyInstance.Policy as ILocalPolicy)?.Handler != null)
            {
                var policy = policyInstance.Policy as ILocalPolicy;
                if (policy != null)
                    return policy.Handler.GetPolicyDecision(principal, policy, null).Outcome;

            }
            return policyInstance.Rule;

        }
    }
}
