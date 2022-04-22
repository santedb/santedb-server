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
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Server.Core.Security.Attribute;
using SanteDB.Messaging.AMI.Client;
using SanteDB.Server.AdminConsole.Attributes;
using SanteDB.Server.AdminConsole.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SanteDB.Core.Interop;

namespace SanteDB.Server.AdminConsole.Shell.CmdLets
{
    /// <summary>
    /// Commandlet for user commands
    /// </summary>
    [AdminCommandlet]
    [ExcludeFromCodeCoverage]
    public static class UserCmdlet
    {
        /// <summary>
        /// Base class for user operations
        /// </summary>
        internal class GenericUserParms
        {
            /// <summary>
            /// Gets or sets the username
            /// </summary>
            [Description("The username of the user")]
            [Parameter("*")]
            [Parameter("u")]
            public StringCollection UserName { get; set; }
        }

        // Ami client
        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationContext.Current.GetRestClient(ServiceEndpointType.AdministrationIntegrationService));

        #region User Add

        internal class UseraddParms : GenericUserParms
        {
            /// <summary>
            /// Gets or sets the password
            /// </summary>
            [Description("The password of the user")]
            [Parameter("p")]
            public String Password { get; set; }

            /// <summary>
            /// Gets or sets the roles
            /// </summary>
            [Description("Adds the user to the specified role")]
            [Parameter("r")]
            public StringCollection Roles { get; set; }

            /// <summary>
            /// The name of the user to add
            /// </summary>
            [Description("The email of the user to create")]
            [Parameter("e")]
            public string Email { get; set; }
        }

        /// <summary>
        /// Useradd parameters
        /// </summary>
        [AdminCommand("user.add", "Adds a user to the SanteDB instance")]
        [Description("This command add the specified user to the SanteDB IMS instance")]
        // // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateIdentity)]
        internal static void Useradd(UseraddParms parms)
        {
            var roles = new List<SecurityRoleInfo>();
            if (parms.Roles?.Count > 0)
                roles = parms.Roles.OfType<String>().Select(o => m_client.GetRoles(r => r.Name == o).CollectionItem.FirstOrDefault()).OfType<SecurityRoleInfo>().ToList();
            if (roles.Count == 0)
                throw new InvalidOperationException("The specified roles do not exit. Roles are case sensitive"); ;

            if (roles.Count == 0)
                throw new InvalidOperationException("The specified roles do not exit. Roles are case sensitive"); ;
            if (parms.UserName == null)
                throw new InvalidOperationException("Must specify a user");

            foreach (var un in parms.UserName)
                m_client.CreateUser(new SecurityUserInfo()
                {
                    Entity = new SecurityUser()
                    {
                        UserName = un,
                        Password = parms.Password ?? "Mohawk123",
                        Email = parms.Email,
                    },
                    Roles = parms.Roles.OfType<String>().ToList()
                });
        }

        #endregion User Add

        #region User Delete/Lock Commands

        /// <summary>
        /// User locking parms
        /// </summary>
        internal class UserLockParms : GenericUserParms
        {
            /// <summary>
            /// The time to set the lock util
            /// </summary>
            [Description("Whether or not to set the lock")]
            [Parameter("l")]
            public bool Locked { get; set; }
        }

        /// <summary>
        /// Useradd parameters
        /// </summary>
        [AdminCommand("user.del", "De-activates a user to the SanteDB instance")]
        [Description("This command change the obsoletion time of the user effectively de-activating it")]
        // // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterIdentity)]
        internal static void Userdel(GenericUserParms parms)
        {
            if (parms.UserName == null)
                throw new InvalidOperationException("Must specify a user");

            foreach (var un in parms.UserName)
            {
                var user = m_client.GetUsers(o => o.UserName == un).CollectionItem.FirstOrDefault() as SecurityUserInfo;
                if (user == null)
                    throw new KeyNotFoundException($"User {un} not found");

                m_client.DeleteUser(user.Entity.Key.Value);
            }
        }

        /// <summary>
        /// Useradd parameters
        /// </summary>
        [AdminCommand("user.undel", "Re-activates a user to the SanteDB instance")]
        [Description("This command will undo a de-activation and will reset the user's obsoletion time")]
        // // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterIdentity)]
        internal static void Userudel(GenericUserParms parms)
        {
            if (parms.UserName == null)
                throw new InvalidOperationException("Must specify a user");

            foreach (var un in parms.UserName)
            {
                var user = m_client.GetUsers(o => o.UserName == un).CollectionItem.FirstOrDefault() as SecurityUserInfo;
                if (user == null)
                    throw new KeyNotFoundException($"User {un} not found");

                var patch = new Patch()
                {
                    AppliesTo = new PatchTarget(user.Entity),
                    Operation = new List<PatchOperation>()
                    {
                        new PatchOperation(PatchOperationType.Remove, "obsoletedBy", null),
                        new PatchOperation(PatchOperationType.Remove, "obsoletionTime", null)
                    }
                };
                m_client.Client.Patch($"SecurityUser/{user.Key}", user.Tag, patch);
            }
        }

        /// <summary>
        /// Useradd parameters
        /// </summary>
        [AdminCommand("user.lock", "Engages or disengages the user lock")]
        // // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterIdentity)]
        [Description("This command will change lock status of the user, either setting it or un-setting it")]
        internal static void Userlock(UserLockParms parms)
        {
            if (parms.UserName == null)
                throw new InvalidOperationException("Must specify a user");

            foreach (var un in parms.UserName)
            {
                var user = m_client.GetUsers(o => o.UserName == un).CollectionItem.FirstOrDefault() as SecurityUserInfo;
                if (user == null)
                    throw new KeyNotFoundException($"User {un} not found");

                user.Entity.Lockout = !parms.Locked ? null : (DateTime?)DateTime.MaxValue;

                if (parms.Locked)
                    m_client.Client.Lock<SecurityUserInfo>($"SecurityUser/{user.Key}");
                else
                    m_client.Client.Unlock<SecurityUserInfo>($"SecurityUser/{user.Key}");
            }
        }

        #endregion User Delete/Lock Commands

        #region User List

        /// <summary>
        /// User list parameters
        /// </summary>
        internal class UserListParms : GenericUserParms
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
            [Description("Show obsolete non-active status")]
            [Parameter("a")]
            public bool Active { get; set; }

            /// <summary>
            /// Locked
            /// </summary>
            [Description("Filter on human class only")]
            [Parameter("h")]
            public bool Human { get; set; }

            /// <summary>
            /// Locked
            /// </summary>
            [Description("Filter on system class only")]
            [Parameter("s")]
            public bool System { get; set; }
        }

        /// <summary>
        /// List users
        /// </summary>
        [AdminCommand("user.list", "Lists users in the SanteDB instance")]
        [Description("This command lists all users in the user database regardless of their status, or class. To filter use the filter parameters listed.")]
        internal static void Userlist(UserListParms parms)
        {
            var un = parms.UserName?.OfType<String>()?.FirstOrDefault();
            AmiCollection users = null;
            if (parms.Locked && !String.IsNullOrEmpty(un))
                users = m_client.GetUsers(o => o.UserName.Contains(un) && o.Lockout.HasValue);
            else if (!String.IsNullOrEmpty(un))
                users = m_client.GetUsers(o => o.UserName.Contains(un));
            else
                users = m_client.GetUsers(o => o.ObsoletionTime == null);

            if (parms.Active)
                users.CollectionItem = users.CollectionItem.OfType<SecurityUserInfo>().Where(o => o.Entity.ObsoletionTime.HasValue).OfType<object>().ToList();
            if (parms.Human)
                users.CollectionItem = users.CollectionItem.OfType<SecurityUserInfo>().Where(o => o.Entity.UserClass == ActorTypeKeys.HumanUser).OfType<object>().ToList();
            else if (parms.System)
                users.CollectionItem = users.CollectionItem.OfType<SecurityUserInfo>().Where(o => o.Entity.UserClass != ActorTypeKeys.HumanUser).OfType<object>().ToList();
            DisplayUtil.TablePrint(users.CollectionItem.OfType<SecurityUserInfo>(),
                new String[] { "SID", "Name", "Last Auth", "Lockout", "ILA", "A" },
                new int[] { 38, 24, 22, 22, 4, 2 },
                o => o.Entity.Key,
                o => o.Entity.UserName,
                o => o.Entity.LastLoginTimeXml,
                o => o.Entity.LockoutXml,
                o => o.Entity.InvalidLoginAttempts,
                o => o.Entity.ObsoletionTime.HasValue ? null : "*"
            );
        }

        #endregion User List

        #region Change Roles

        internal class ChangeRoleParms : GenericUserParms
        {
            /// <summary>
            /// Gets or sets roles
            /// </summary>
            [Description("The roles to assign to the user")]
            [Parameter("r")]
            public StringCollection Roles { get; set; }
        }

        /// <summary>
        /// Gets or sets roles
        /// </summary>
        // // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterIdentity)]
        [AdminCommand("user.roles", "Change roles for a user")]
        [Description("This command is used to assign roles for the specified user to the specified roles. Note that the role list provided replaces the current role list of the user")]
        internal static void ChangeRoles(ChangeRoleParms parms)
        {
            if (parms.UserName == null)
                throw new InvalidOperationException("Must specify a user");

            foreach (var un in parms.UserName)
            {
                var user = m_client.GetUsers(o => o.UserName == un).CollectionItem.FirstOrDefault() as SecurityUserInfo;
                if (user == null)
                    throw new KeyNotFoundException($"User {un} not found");

                var roles = new List<SecurityRoleInfo>();
                if (parms.Roles?.Count > 0)
                    roles = parms.Roles.OfType<String>().Select(o => m_client.GetRoles(r => r.Name == o).CollectionItem.FirstOrDefault()).OfType<SecurityRoleInfo>().ToList();
                if (roles.Count == 0)
                    throw new InvalidOperationException("The specified roles do not exit. Roles are case sensitive"); ;

                user.Roles = parms.Roles.OfType<String>().ToList();
                m_client.UpdateUser(user.Entity.Key.Value, user);
            }
        }

        #endregion Change Roles

        /// <summary>
        /// Gets or sets the password
        /// </summary>
        internal class UserPasswordParms : GenericUserParms
        {
            /// <summary>
            /// Gets or sets the password
            /// </summary>
            [Description("The password to set on the user")]
            [Parameter("p")]
            public String Password { get; set; }
        }

        /// <summary>
        /// Set user password
        /// </summary>
        [AdminCommand("user.password", "Changes a users password")]
        [Description("This command will change the specified user's password the specified password. The server will reject this command if the password does not meet complixity requirements")]
        internal static void SetPassword(UserPasswordParms parms)
        {
            if (parms.UserName == null)
                throw new InvalidOperationException("Must specify a user");

            foreach (var un in parms.UserName)
            {
                var user = m_client.GetUsers(o => o.UserName == un).CollectionItem.FirstOrDefault() as SecurityUserInfo;
                if (user == null)
                    throw new KeyNotFoundException($"User {un} not found");

                if (String.IsNullOrEmpty(parms.Password))
                {
                    var passwd = DisplayUtil.PasswordPrompt($"NEW Password for {user.Entity.UserName}:");
                    if (String.IsNullOrEmpty(passwd))
                    {
                        Console.WriteLine("Aborted");
                        continue;
                    }
                    else if (passwd != DisplayUtil.PasswordPrompt($"CONFIRM Password for {user.Entity.UserName}:"))
                    {
                        Console.WriteLine("Passwords do not match!");
                        continue;
                    }
                    user.Entity.Password = passwd;
                }
                else
                    user.Entity.Password = parms.Password;
                user.PasswordOnly = true;
                m_client.UpdateUser(user.Entity.Key.Value, user);

                break;
            }
        }

        /// <summary>
        /// User information
        /// </summary>
        [AdminCommand("user.info", "Displays detailed information about the user")]
        [Description("This command will display detailed information about the specified security user account. It will show groups, status, and effective policies")]
        internal static void UserInfo(GenericUserParms parms)
        {
            if (parms.UserName == null)
                throw new InvalidOperationException("Must specify a user");

            foreach (var un in parms.UserName)
            {
                var user = m_client.GetUsers(o => o.UserName == un).CollectionItem.FirstOrDefault() as SecurityUserInfo;
                if (user == null)
                    throw new KeyNotFoundException($"User {un} not found");

                DisplayUtil.PrintPolicies(user,
                    new string[] { "Name", "SID", "Email", "Phone", "Invalid Logins", "Lockout", "Last Login", "Created", "Updated", "De-Activated", "Roles" },
                    u => u.UserName,
                    u => u.Key,
                    u => u.Email,
                    u => u.PhoneNumber,
                    u => u.InvalidLoginAttempts,
                    u => u.LockoutXml,
                    u => u.LastLoginTimeXml,
                    u => String.Format("{0} ({1})", u.CreationTimeXml, m_client.GetUser(m_client.GetProvenance(u.CreatedByKey.Value).UserKey.Value).Entity.UserName),
                    u => String.Format("{0} ({1})", u.UpdatedTimeXml, m_client.GetUser(m_client.GetProvenance(u.UpdatedByKey.Value).UserKey.Value).Entity.UserName),
                    u => String.Format("{0} ({1})", u.ObsoletionTimeXml, m_client.GetUser(m_client.GetProvenance(u.ObsoletedByKey.Value).UserKey.Value).Entity.UserName),
                    u => String.Join(" , ", user.Roles)
                );
            }
        }
    }
}