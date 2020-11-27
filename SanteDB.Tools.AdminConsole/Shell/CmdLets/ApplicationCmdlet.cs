/*
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
    /// Represents an application commandlet for adding/removing/updating applications
    /// </summary>
    [AdminCommandlet]
    public static class ApplicationCmdlet
    {

        /// <summary>
        /// Base class for user operations
        /// </summary>
        internal class GenericApplicationParms
        {

            /// <summary>
            /// Gets or sets the username
            /// </summary>
            [Description("The identity of the application")]
            [Parameter("*")]
            public StringCollection ApplictionId { get; set; }

        }

        // Ami client
        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationContext.Current.GetRestClient(Core.Interop.ServiceEndpointType.AdministrationIntegrationService));

        #region Application Add 

        /// <summary>
        /// Parameters for adding applications
        /// </summary>
        internal class AddApplicationParms : GenericApplicationParms
        {
            /// <summary>
            /// The secret of the application
            /// </summary>
            [Description("The application secret to set")]
            [Parameter("s")]
            public string Secret { get; set; }

            /// <summary>
            /// The policies to add 
            /// </summary>
            [Description("The policies to grant deny application")]
            [Parameter("g")]
            public StringCollection GrantPolicies { get; set; }

            /// <summary>
            /// The policies to deny 
            /// </summary>
            [Description("The policies to deny the application")]
            [Parameter("d")]
            public StringCollection DenyPolicies { get; set; }

            /// <summary>
            /// The note
            /// </summary>
            [Description("A description/note to add to the application")]
            [Parameter("n")]
            public String Description { get; set; }
        }

        [AdminCommand("application.add", "Add security application")]
        [Description("This command will create a new security application which can be used to access the SanteDB instance")]
        // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateApplication)]
        internal static void AddApplication(AddApplicationParms parms)
        {
            var policies = new List<SecurityPolicyInfo>();

            if (parms.GrantPolicies?.Count > 0)
                policies = parms.GrantPolicies.OfType<String>().Select(o => m_client.GetPolicies(r => r.Oid == o).CollectionItem.FirstOrDefault()).OfType<SecurityPolicy>().Select(o => new SecurityPolicyInfo(o)).ToList();
            if (parms.DenyPolicies?.Count > 0)
                policies = policies.Union(parms.DenyPolicies.OfType<String>().Select(o => m_client.GetPolicies(r => r.Oid == o).CollectionItem.FirstOrDefault()).OfType<SecurityPolicy>().Select(o => new SecurityPolicyInfo(o))).ToList();

            policies.ForEach(o => o.Grant = parms.GrantPolicies?.Contains(o.Oid) == true ? Core.Model.Security.PolicyGrantType.Grant : PolicyGrantType.Deny);

            if (policies.Count != (parms.DenyPolicies?.Count ?? 0) + (parms.GrantPolicies?.Count ?? 0))
                throw new InvalidOperationException("Could not find one or more policies");

            if (String.IsNullOrEmpty(parms.Secret))
            {
                parms.Secret = BitConverter.ToString(Guid.NewGuid().ToByteArray()).Replace("-","").Substring(0, 12);
                Console.WriteLine("Application secret: {0}", parms.Secret);
            }

            m_client.CreateApplication(new SecurityApplicationInfo()
            {
                Policies = policies,
                Entity = new Core.Model.Security.SecurityApplication()
                {
                    Name = parms.ApplictionId.OfType<String>().First(),
                    ApplicationSecret = parms.Secret
                }
            });
            Console.WriteLine("CREATE {0}", parms.ApplictionId[0]);

        }

        /// <summary>
        /// User list parameters
        /// </summary>
        internal class ApplicationListParams
        {

            /// <summary>
            /// Locked
            /// </summary>
            [Description("Filter on locked status")]
            [Parameter("l")]
            public bool Locked { get; set; }

            /// <summary>
            /// Locked
            /// </summary>
            [Description("Include non-active application")]
            [Parameter("a")]
            public bool Active { get; set; }


        }


        [AdminCommand("application.list", "List Security Applications")]
        [Description("This command will list all security applications in the SanteDB instance")]
        // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        internal static void ListRoles(ApplicationListParams parms)
        {
            AmiCollection list = null;
            int tr = 0;
            if (parms.Active)
                list = m_client.Query<SecurityApplication>(o => o.ObsoletionTime.HasValue, 0, 100, out tr);
            else if (parms.Locked)
                list = m_client.Query<SecurityApplication>(o => o.Lockout.HasValue, 0, 100, out tr);
            else
                list = m_client.Query<SecurityApplication>(o => o.ObsoletionTime == null, 0, 100, out tr);

            DisplayUtil.TablePrint(list.CollectionItem.OfType<SecurityApplicationInfo>(),
                new String[] { "SID", "Name", "Last Auth.", "Lockout", "ILA", "A" },
                new int[] { 38, 24, 22, 22, 4, 2 },
                o => o.Entity.Key,
                o => o.Entity.Name,
                o => o.Entity.LastAuthenticationXml,
                o => o.Entity.LockoutXml,
                o => o.Entity.InvalidAuthAttempts ?? 0,
                o => !o.Entity.ObsoletionTime.HasValue ? "*" : null);
        }

        /// <summary>
        /// Detail security information
        /// </summary>
        // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateApplication)]
        [AdminCommand("application.info", "Displays detailed information about the application")]
        [Description("This command will display detailed information about the specified security application account. It will status, and effective policies")]
        internal static void ApplicationInfo(GenericApplicationParms parms)
        {

            if (parms.ApplictionId == null)
                throw new InvalidOperationException("Must specify a application");

            foreach (var un in parms.ApplictionId)
            {
                var device = m_client.GetApplications(o => o.Name == un).CollectionItem.FirstOrDefault() as SecurityApplicationInfo;
                if (device == null)
                    throw new KeyNotFoundException($"Application {un} not found");

                DisplayUtil.PrintPolicies(device,
                    new string[] { "Name", "SID", "Invalid Auth", "Lockout", "Last Auth", "Created", "Updated", "De-Activated" },
                    u => u.Name,
                    u => u.Key,
                    u => u.InvalidAuthAttempts,
                    u => u.LockoutXml,
                    u => u.LastAuthenticationXml,
                    u => String.Format("{0} ({1})", u.CreationTimeXml, m_client.GetUser(m_client.GetProvenance(u.CreatedByKey.Value).UserKey.Value).Entity.UserName),
                    u => String.Format("{0} ({1})", u.UpdatedTimeXml, m_client.GetUser(m_client.GetProvenance(u.UpdatedByKey.Value).UserKey.Value).Entity.UserName),
                    u => String.Format("{0} ({1})", u.ObsoletionTimeXml, m_client.GetUser(m_client.GetProvenance(u.ObsoletedByKey.Value).UserKey.Value).Entity.UserName)
                );

            }
        }
        #endregion
    }
}
