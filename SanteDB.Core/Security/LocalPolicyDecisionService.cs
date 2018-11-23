﻿/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: justin
 * Date: 2018-6-22
 */
using MARC.HI.EHRS.SVC.Core.Services.Policy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;
using MARC.HI.EHRS.SVC.Core;
using SanteDB.Core.Model.Security;
using MARC.HI.EHRS.SVC.Core.Services;
using System.Security.Claims;
using SanteDB.Core.Security.Claims;

namespace SanteDB.Core.Security
{
    /// <summary>
    /// Local policy decision service
    /// </summary>
    public class LocalPolicyDecisionService : IPolicyDecisionService
    {

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
            var pip = ApplicationContext.Current.GetService<IPolicyInformationService>();
            IEnumerable<IPolicyInstance> securablePolicies = pip.GetActivePolicies(securable),
                principalPolicies = pip.GetActivePolicies(principal);

            var retVal = new PolicyDecision(securable);

            foreach (var pol in securablePolicies)
            {
                // Get most restrictive from principal
                var rules = principalPolicies.Where(p => p.Policy.Oid.StartsWith(pol.Policy.Oid)).Select(o => o.Rule);
                PolicyDecisionOutcomeType rule = PolicyDecisionOutcomeType.Deny;
                if(rules.Any())
                    rule = rules.Min();

                // Rule for elevate can only be made when the policy allows for it & the principal is allowed
                if (rule == PolicyDecisionOutcomeType.Elevate &&
                    (!pol.Policy.CanOverride ||
                    principalPolicies.Any(o => o.Policy.Oid == PermissionPolicyIdentifiers.ElevateClinicalData && o.Rule == PolicyDecisionOutcomeType.Grant)))
                    rule = PolicyDecisionOutcomeType.Deny;

                retVal.Details.Add(new PolicyDecisionDetail(pol.Policy.Oid, rule));
            }

            return retVal;
        }

        /// <summary>
        /// Get a policy outcome
        /// </summary>
        public PolicyDecisionOutcomeType GetPolicyOutcome(IPrincipal principal, string policyId)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));
            else if (String.IsNullOrEmpty(policyId))
                throw new ArgumentNullException(nameof(policyId));

            // Can we make this decision based on the claims? 
            if (principal is ClaimsPrincipal && (principal as ClaimsPrincipal).HasClaim(c => c.Type == SanteDBClaimTypes.SanteDBGrantedPolicyClaim && policyId.StartsWith(c.Value)))
                return PolicyDecisionOutcomeType.Grant;

            // Get the user object from the principal
            var pip = ApplicationContext.Current.GetService<IPolicyInformationService>();

            // Policies
            var activePolicies = pip.GetActivePolicies(principal).Where(o => policyId.StartsWith(o.Policy.Oid));
            // Most restrictive
            IPolicyInstance policyInstance = null;
            foreach (var pol in activePolicies)
                if (policyInstance == null)
                    policyInstance = pol;
                else if (pol.Rule < policyInstance.Rule)
                    policyInstance = pol;

            if (policyInstance == null)
            {
                // TODO: Configure OptIn or OptOut
                return PolicyDecisionOutcomeType.Deny;
            }
            else if (!policyInstance.Policy.CanOverride && policyInstance.Rule == PolicyDecisionOutcomeType.Elevate)
                return PolicyDecisionOutcomeType.Deny;
            else if (!policyInstance.Policy.IsActive)
                return PolicyDecisionOutcomeType.Grant;
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
