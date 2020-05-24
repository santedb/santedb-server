/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using MohawkCollege.Util.Console.Parameters;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Messaging.AMI.Client;
using SanteDB.Tools.AdminConsole.Attributes;
using SanteDB.Tools.AdminConsole.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace SanteDB.Tools.AdminConsole.Shell.CmdLets
{
    /// <summary>
    /// Commandlets for roles
    /// </summary>
    [AdminCommandlet]
    public static class RoleCmdlet
    {

        /// <summary>
        /// Base class for user operations
        /// </summary>
        internal class GenericRoleParms
        {

            /// <summary>
            /// Gets or sets the username
            /// </summary>
            [Description("The name of the role")]
            [Parameter("r")]
            public String RoleName { get; set; }

        }

        // Ami client
        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationContext.Current.GetRestClient(Core.Interop.ServiceEndpointType.AdministrationIntegrationService));


        #region Add Role

        /// <summary>
        /// Add role parameters
        /// </summary>
        internal class AddRoleParms : GenericRoleParms
        {

            /// <summary>
            /// Gets or sets the policies
            /// </summary>
            [Description("The policies to grant the role")]
            [Parameter("g")]
            public StringCollection GrantPolicies { get; set; }

            /// <summary>
            /// Gets or sets the policies
            /// </summary>
            [Description("The policies to deny the role")]
            [Parameter("d")]
            public StringCollection DenyPolicies { get; set; }

            /// <summary>
            /// Gets or sets the description
            /// </summary>
            [Description("Set a note/description of the role")]
            [Parameter("n")]
            public String Description { get; set; }

        }

        /// <summary>
        /// Add a role
        /// </summary>
        // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateRoles)]
        [AdminCommand("role.add", "Adds a role to the current SanteDB instance")]
        internal static void AddRole(AddRoleParms parms)
        {
            var policies = new List<SecurityPolicyInfo>();

            if (parms.GrantPolicies?.Count > 0)
                policies = parms.GrantPolicies.OfType<String>().Select(o => m_client.GetPolicies(r => r.Oid == o).CollectionItem.FirstOrDefault()).OfType<SecurityPolicy>().Select(o => new SecurityPolicyInfo(o)).ToList();
            if (parms.DenyPolicies?.Count > 0)
                policies = policies.Union(parms.DenyPolicies.OfType<String>().Select(o => m_client.GetPolicies(r => r.Oid == o).CollectionItem.FirstOrDefault()).OfType<SecurityPolicy>().Select(o => new SecurityPolicyInfo(o))).ToList();

            policies.ForEach(o => o.Grant = parms.GrantPolicies?.Contains(o.Oid) == true ? Core.Model.Security.PolicyGrantType.Grant : PolicyGrantType.Deny);

            m_client.CreateRole(new Core.Model.AMI.Auth.SecurityRoleInfo()
            {
                Policies = policies,
                Entity = new Core.Model.Security.SecurityRole()
                {
                    Name = parms.RoleName,
                    Description = parms.Description
                }
            });

        }


        /// <summary>
        /// User list parameters
        /// </summary>
        internal class RoleListParams
        {

            /// <summary>
            /// Locked
            /// </summary>
            [Description("Include non-active roles")]
            [Parameter("a")]
            public bool Active { get; set; }


        }


        [AdminCommand("role.list", "List Security Roles")]
        [Description("This command will list all security roles in the SanteDB instance")]
        // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        internal static void ListRoles(RoleListParams parms)
        {
            AmiCollection list = null;
            int tr = 0;
            if (parms.Active)
                list = m_client.Query<SecurityRole>(o => o.ObsoletionTime != null, 0, 100, out tr);
            else
                list = m_client.Query<SecurityRole>(o => o.ObsoletionTime == null, 0, 100, out tr);

            DisplayUtil.TablePrint(list.CollectionItem.OfType<SecurityRoleInfo>(),
                new String[] { "SID", "Name", "Description", "A" },
                new int[] { 38, 20, 48, 2 },
                o => o.Entity.Key,
                o => o.Entity.Name,
                o => o.Entity.Description,
                o => !o.Entity.ObsoletionTime.HasValue ? "*" : null);
        }

        /// <summary>
        /// User information
        /// </summary>
        // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterRoles)]
        [AdminCommand("role.info", "Displays detailed information about the role")]
        [Description("This command will display detailed information about the specified security role")]
        internal static void RoleInfo(GenericRoleParms parms)
        {

            if (parms.RoleName == null)
                throw new InvalidOperationException("Must specify a role");

            var role = m_client.GetRoles(o => o.Name == parms.RoleName).CollectionItem.FirstOrDefault() as SecurityRoleInfo;
            if (role == null)
                throw new KeyNotFoundException($"Role {parms.RoleName} not found");

            DisplayUtil.PrintPolicies(role,
                new string[] { "Name", "SID", "Description", "Created", "Updated", "De-Activated" },
                u => u.Name,
                u => u.Key,
                u => u.Description,
                u => String.Format("{0} ({1})", u.CreationTimeXml, m_client.GetUser(m_client.GetProvenance(u.CreatedByKey.Value).UserKey.Value).Entity.UserName),
                u => String.Format("{0} ({1})", u.UpdatedTimeXml, m_client.GetUser(m_client.GetProvenance(u.UpdatedByKey.Value).UserKey.Value).Entity.UserName),
                u => String.Format("{0} ({1})", u.ObsoletionTimeXml, m_client.GetUser(m_client.GetProvenance(u.ObsoletedByKey.Value).UserKey.Value).Entity.UserName)
            );
        }
        #endregion


    }
}
