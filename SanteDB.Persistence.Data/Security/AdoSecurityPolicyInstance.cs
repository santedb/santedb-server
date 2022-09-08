/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-9-7
 */
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Security
{
    /// <summary>
    /// Represents a security policy instance when constructed database
    /// </summary>
    internal sealed class AdoSecurityPolicyInstance : IPolicyInstance
    {

        /// <summary>
        /// ADO Policy instance
        /// </summary>
        public AdoSecurityPolicyInstance()
        {
        }

        /// <summary>
        /// Create a policy from policy instance
        /// </summary>
        public AdoSecurityPolicyInstance(IPolicy policyInstance, PolicyGrantType rule)
        {
            this.Policy = policyInstance;
            this.Rule = rule;
        }

        /// <summary>
        /// Local security policy instance
        /// </summary>
        public AdoSecurityPolicyInstance(DbSecurityPolicyActionableInstance policyInstance, DbSecurityPolicy policy, object securable)
        {
            this.Policy = new AdoSecurityPolicy(policy);
            this.Rule = (PolicyGrantType)policyInstance.GrantType;
            this.Securable = securable;
        }

        /// <summary>
        /// Local security policy instance
        /// </summary>
        public AdoSecurityPolicyInstance(DbSecurityDevicePolicy devicePolicy, DbSecurityPolicy policy, object securable)
        {
            this.Policy = new AdoSecurityPolicy(policy);
            this.Rule = (PolicyGrantType)devicePolicy.GrantType;
            this.Securable = securable;
        }

        /// <summary>
        /// Local security policy instance
        /// </summary>
        public AdoSecurityPolicyInstance(DbActSecurityPolicy actPolicy, DbSecurityPolicy policy, object securable)
        {
            this.Policy = new AdoSecurityPolicy(policy);
            // TODO: Configuration of the policy as opt-in / opt-out
            this.Rule = PolicyGrantType.Grant;
            this.Securable = securable;
        }

        /// <summary>
        /// Local security policy instance
        /// </summary>
        public AdoSecurityPolicyInstance(DbEntitySecurityPolicy entityPolicy, DbSecurityPolicy policy, object securable)
        {
            this.Policy = new AdoSecurityPolicy(policy);
            // TODO: Configuration of the policy as opt-in / opt-out
            this.Rule = PolicyGrantType.Grant;
            this.Securable = securable;
        }

        /// <summary>
        /// Restrict this policy rule if the otherPolicies enumeration has a more restrictive rule
        /// </summary>
        internal IPolicyInstance Restrict(IEnumerable<IPolicyInstance> otherPolicies)
        {
            this.Rule = otherPolicies.Where(o => this.Policy.Oid.StartsWith(o.Policy.Oid)).Min(o => o.Rule);
            return this;
        }

        /// <summary>
        /// The policy 
        /// </summary>
        public IPolicy Policy { get; private set; }

        /// <summary>
        /// Policy outcome
        /// </summary>
        public PolicyGrantType Rule { get; private set; }

        /// <summary>
        /// Securable
        /// </summary>
        public object Securable { get; private set; }

        /// <summary>
        /// Represent as string
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{this.Policy?.Oid} ({this.Policy?.Name}) => {this.Rule}";
    }
}
