/*
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
 * Date: 2018-10-24
 */
using MohawkCollege.Util.Console.Parameters;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Messaging.AMI.Client;
using SanteDB.Tools.AdminConsole.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace SanteDB.Tools.AdminConsole.Shell.CmdLets
{
    /// <summary>
    /// Commandlet for user commands
    /// </summary>
    [AdminCommandlet]
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
        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationServiceContext.Current.GetRestClient(Core.Interop.ServiceEndpointType.AdministrationIntegrationService));

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
        [AdminCommand("useradd", "Adds a user to the SanteDB instance")]
        [Description("This command add the specified user to the SanteDB IMS instance")]
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateIdentity)]
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
                m_client.CreateUser(new Core.Model.AMI.Auth.SecurityUserInfo()
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

        #endregion

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
        [AdminCommand("userdel", "De-activates a user to the SanteDB instance")]
        [Description("This command change the obsoletion time of the user effectively de-activating it")]
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterIdentity)]
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
        [AdminCommand("userundel", "Re-activates a user to the SanteDB instance")]
        [Description("This command will undo a de-activation and will reset the user's obsoletion time")]
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterIdentity)]
        internal static void Userudel(GenericUserParms parms)
        {
            if (parms.UserName == null)
                throw new InvalidOperationException("Must specify a user");

            foreach (var un in parms.UserName)
            {
                var user = m_client.GetUsers(o => o.UserName == un).CollectionItem.FirstOrDefault() as SecurityUserInfo;
                if (user == null)
                    throw new KeyNotFoundException($"User {un} not found");

                user.Entity.Lockout = DateTime.MinValue;
                user.Entity.ObsoletionTime = null;
                user.Entity.ObsoletedBy = null;

                m_client.UpdateUser(user.Entity.Key.Value, user);
            }
        }

        /// <summary>
        /// Useradd parameters
        /// </summary>
        [AdminCommand("userlock", "Engages or disengages the user lock")]
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterIdentity)]
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

                if(parms.Locked)
                    m_client.Client.Lock<SecurityUserInfo>($"SecurityUser/{user.Key}");
                else
                    m_client.Client.Unlock<SecurityUserInfo>($"SecurityUser/{user.Key}");
            }
        }

        #endregion

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
            [Description("Filter on active status")]
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
        [AdminCommand("userlist", "Lists users in the SanteDB instance")]
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
                users = m_client.GetUsers(o => o.UserName != null);

            if (parms.Active)
                users.CollectionItem = users.CollectionItem.OfType<SecurityUserInfo>().Where(o => !o.Entity.ObsoletionTime.HasValue).OfType<object>().ToList();
            if (parms.Human)
                users.CollectionItem = users.CollectionItem.OfType<SecurityUserInfo>().Where(o => o.Entity.UserClass == UserClassKeys.HumanUser).OfType<object>().ToList();
            else if (parms.System)
                users.CollectionItem = users.CollectionItem.OfType<SecurityUserInfo>().Where(o => o.Entity.UserClass != UserClassKeys.HumanUser).OfType<object>().ToList();

            Console.WriteLine("SID{0}UserName{1}Last Lgn{2}Lockout{2} ILA  A", new String(' ', 37), new String(' ', 32), new String(' ', 13));
            foreach (var usr in users.CollectionItem.OfType<SecurityUserInfo>())
            {
                Console.WriteLine("{0}{1}{2}{3}{4}{5}{6}{5}{7}{8}{9}",
                    usr.Entity.Key.Value.ToString("B"),
                    new String(' ', 2),
                    usr.Entity.UserName.Length > 38 ? usr.Entity.UserName.Substring(0, 38) : usr.Entity.UserName,
                    new String(' ', usr.Entity.UserName.Length > 38 ? 2 : 40 - usr.Entity.UserName.Length),
                    usr.Entity.LastLoginTime.HasValue ? usr.Entity.LastLoginTime?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss") : new String(' ', 19),
                    "  ",
                    usr.Entity.Lockout.HasValue ? usr.Entity.Lockout?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss") : new string(' ', 19),
                    usr.Entity.InvalidLoginAttempts,
                    new String(' ', 4 - usr.Entity.InvalidLoginAttempts.ToString().Length),
                    usr.Entity.ObsoletionTime.HasValue ? "  " : " *"
                    );
            }
        }
        #endregion

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
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterIdentity)]
        [AdminCommand("chrole", "Change roles for a user")]
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
        #endregion

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
        [AdminCommand("passwd", "Changes a users password")]
        [Description("This command will change the specified user's password the specified password. The server will reject this command if the password does not meet complixity requirements")]
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ChangePassword)]
        internal static void SetPassword(UserPasswordParms parms)
        {
            if (parms.UserName == null)
                throw new InvalidOperationException("Must specify a user");

            foreach (var un in parms.UserName)
            {
                var user = m_client.GetUsers(o => o.UserName == un).CollectionItem.FirstOrDefault() as SecurityUserInfo;
                if (user == null)
                    throw new KeyNotFoundException($"User {un} not found");

                user.Entity.Password = parms.Password;
                user.PasswordOnly = true;
                m_client.UpdateUser(user.Entity.Key.Value, user);
                break;
            }
        }

        /// <summary>
        /// User information
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterIdentity)]
        [AdminCommand("userinfo", "Displays detailed information about the user")]
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

                Console.WriteLine("User: {0}", user.Entity.UserName);
                Console.WriteLine("\tSID: {0}", user.Entity.Key);
                Console.WriteLine("\tEmail: {0}", user.Entity.Email);
                Console.WriteLine("\tPhone: {0}", user.Entity.PhoneNumber);
                Console.WriteLine("\tInvalid Logins: {0}", user.Entity.InvalidLoginAttempts);
                Console.WriteLine("\tLockout: {0}", user.Entity.Lockout);
                Console.WriteLine("\tLast Login: {0}", user.Entity.LastLoginTime);
                Console.WriteLine("\tCreated: {0} ({1})", user.Entity.CreationTime, m_client.GetUser(m_client.GetProvenance(user.Entity.CreatedByKey.Value).UserKey.Value).Entity.UserName);
                if (user.Entity.UpdatedTime.HasValue)
                    Console.WriteLine("\tLast Updated: {0} ({1})", user.Entity.UpdatedTime, m_client.GetUser(m_client.GetProvenance(user.Entity.UpdatedByKey.Value).UserKey.Value).Entity.UserName);
                if (user.Entity.ObsoletionTime.HasValue)
                    Console.WriteLine("\tDeActivated: {0} ({1})", user.Entity.ObsoletionTime, m_client.GetUser(m_client.GetProvenance(user.Entity.ObsoletedByKey.Value).UserKey.Value).Entity.UserName);
                Console.WriteLine("\tGroups: {0}", String.Join(";", user.Roles));

                List<SecurityPolicyInfo> policies = m_client.GetPolicies(o => o.ObsoletionTime == null).CollectionItem.OfType<SecurityPolicy>().OrderBy(o => o.Oid).Select(o=>new SecurityPolicyInfo(o)).ToList();
                policies.ForEach(o => o.Grant = (PolicyGrantType)10);
                foreach (var rol in user.Roles)
                    foreach (var pol in m_client.GetRole(rol).Policies)
                    {
                        var existing = policies.FirstOrDefault(o => o.Oid == pol.Oid);
                        if (pol.Grant < existing.Grant)
                            existing.Grant = pol.Grant;
                    }

                Console.WriteLine("\tEffective Policies:");
                foreach (var itm in policies)
                {
                    Console.Write("\t\t{0} : ", itm.Name);
                    if (itm.Grant == (PolicyGrantType)10) // Lookup parent
                    {
                        var parent = policies.LastOrDefault(o => itm.Oid.StartsWith(o.Oid + ".") && itm.Oid != o.Oid);
                        if (parent != null && parent.Grant <= PolicyGrantType.Grant)
                            Console.WriteLine("{0} (inherited from {1})", parent.Grant, parent.Name);
                        else
                            Console.WriteLine("Deny (automatic)");
                    }
                    else
                        Console.WriteLine("{0} (explicit)", itm.Grant);
                }
            }
        }

    }
}
