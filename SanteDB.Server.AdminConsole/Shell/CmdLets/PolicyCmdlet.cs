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

using MohawkCollege.Util.Console.Parameters;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Security;
using SanteDB.Messaging.AMI.Client;
using SanteDB.Server.AdminConsole.Attributes;
using SanteDB.Server.AdminConsole.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace SanteDB.Server.AdminConsole.Shell.CmdLets
{
    /// <summary>
    /// Policy commandlet
    /// </summary>
    [AdminCommandlet]
    public static class PolicyCmdlet
    {
        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationContext.Current.GetRestClient(ServiceEndpointType.AdministrationIntegrationService));

        /// <summary>
        /// List policy parameters
        /// </summary>
        internal class ListPolicyParms
        {
            /// <summary>
            /// List all with name
            /// </summary>
            [Parameter("n")]
            [Description("List policies with specified name")]
            public String Name { get; set; }

            /// <summary>
            /// List with oid
            /// </summary>
            [Parameter("o")]
            [Description("List policies with specified OID pattern")]
            public String Oid { get; set; }
        }

        /// <summary>
        /// List all policies
        /// </summary>
        [AdminCommand("policy.list", "List security policies")]
        [Description("Lists all security policies configured on the server")]
        internal static void ListPolicies(ListPolicyParms parms)
        {
            IEnumerable<SecurityPolicy> policies = null;

            if (!String.IsNullOrEmpty(parms.Name))
                policies = m_client.GetPolicies(o => o.Name.Contains(parms.Name)).CollectionItem.OfType<SecurityPolicy>();
            else if (!String.IsNullOrEmpty(parms.Oid))
                policies = m_client.GetPolicies(o => o.Oid.Contains(parms.Oid)).CollectionItem.OfType<SecurityPolicy>();
            else
                policies = m_client.GetPolicies(o => true).CollectionItem.OfType<SecurityPolicy>();

            // Now output
            DisplayUtil.TablePrint(policies,
                new String[] { "SID", "Name", "Oid" },
                new int[] { 38, 38, 44 },
                p => p.Key,
                p => p.Name,
                p => p.Oid
            );
        }

        /// <summary>
        /// Assign policy parameters
        /// </summary>
        internal class PolicyAssignParms
        {
            /// <summary>
            /// Gets or sets the roles
            /// </summary>
            [Parameter("r")]
            [Parameter("role")]
            [Description("The role(s) to assign the policy to")]
            public StringCollection Role { get; set; }

            /// <summary>
            /// Gets or sets the application(s)
            /// </summary>
            [Parameter("a")]
            [Parameter("application")]
            [Description("The application(s) to assign the policy to")]
            public StringCollection Application { get; set; }

            /// <summary>
            /// Device
            /// </summary>
            [Parameter("d")]
            [Parameter("device")]
            [Description("The device(s) to assign the policy to")]
            public StringCollection Device { get; set; }

            /// <summary>
            /// The grant type
            /// </summary>
            [Parameter("e")]
            [Parameter("rule")]
            [Description("The action to take (0/deny, 1/elevate, 2/grant)")]
            public String GrantType { get; set; }

            /// <summary>
            /// The policies to apply
            /// </summary>
            [Parameter("p")]
            [Parameter("policy")]
            [Description("The policy(ies) to apply")]
            public StringCollection Policy { get; set; }
        }

        /// <summary>
        /// List all policies
        /// </summary>
        [AdminCommand("policy.assign", "Assign security policies to objects")]
        [Description("Assigns policies to devices, roles, and/or applications")]
        internal static void AssignPolicy(PolicyAssignParms parms)
        {
            // Identify the grant type
            PolicyGrantType grant = PolicyGrantType.Deny;
            if (!String.IsNullOrEmpty(parms.GrantType))
            {
                if (Int32.TryParse(parms.GrantType, out int grantParm))
                    grant = (PolicyGrantType)grantParm;
                else if (!Enum.TryParse<PolicyGrantType>(parms.GrantType, out grant))
                    throw new InvalidOperationException($"Can't understand {parms.GrantType}");
            }

            List<SecurityPolicyInfo> policies = null;
            if (parms.Policy?.Count > 0)
                policies = parms.Policy.OfType<String>().Select(o => m_client.GetPolicies(r => r.Oid == o).CollectionItem.FirstOrDefault()).OfType<SecurityPolicy>().Select(o => new SecurityPolicyInfo()
                {
                    CanOverride = false,
                    Grant = grant,
                    Oid = o.Oid,
                    Name = o.Name,
                    Policy = o
                }).ToList();

            if (parms.Policy == null || policies.Count() != parms.Policy.Count)
                throw new InvalidOperationException("Could not find one or more policies");

            var policyGrant = String.Join(";", policies.Select(o => o.Name));

            // Apply to roles?
            if (parms.Role != null)
            {
                foreach (var r in parms.Role)
                {
                    var role = m_client.Query<SecurityRole>(o => o.Name == r, 0, 1, out int _).CollectionItem.FirstOrDefault() as SecurityRoleInfo;
                    if (role == null)
                        throw new KeyNotFoundException($"Role {r} not found");
                    policies.ForEach(o => m_client.Client.Post<SecurityPolicyInfo, SecurityPolicyInfo>($"SecurityRole/{role.Entity.Key}/policy", o));
                    Console.WriteLine("{2}: {1} TO {0}", r, policyGrant, grant);
                }
            }

            // Apply to devices?
            if (parms.Device != null)
            {
                foreach (var d in parms.Device)
                {
                    var device = m_client.Query<SecurityDevice>(o => o.Name == d, 0, 1, out int _).CollectionItem.FirstOrDefault() as SecurityDeviceInfo;
                    if (device == null)
                        throw new KeyNotFoundException($"Device {d} not found");
                    policies.ForEach(o => m_client.Client.Post<SecurityPolicyInfo, SecurityPolicyInfo>($"SecurityDevice/{device.Entity.Key}/policy", o));
                    Console.WriteLine("{2}: {1} TO {0}", d, policyGrant, grant);
                }
            }
            // Apply to devices?
            if (parms.Application != null)
            {
                foreach (var a in parms.Application)
                {
                    var application = m_client.Query<SecurityApplication>(o => o.Name == a, 0, 1, out int _).CollectionItem.FirstOrDefault() as SecurityApplicationInfo;
                    if (application == null)
                        throw new KeyNotFoundException($"Application {a} not found");
                    policies.ForEach(o => m_client.Client.Post<SecurityPolicyInfo, SecurityPolicyInfo>($"SecurityApplication/{application.Entity.Key}/policy", o));
                    Console.WriteLine("{2}: {1} TO {0}", a, policyGrant, grant);
                }
            }
        }
    }
}