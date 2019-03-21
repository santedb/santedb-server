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
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Configuration;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security;
using System.Security.Principal;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Local role provider
    /// </summary>
    [ServiceProvider("ADO.NET Role Provider Service")]
    public class AdoRoleProvider : IRoleProviderService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public String ServiceName => "ADO.NET Role Provider Service";

        // Tracer
        private Tracer m_tracer = new Tracer(AdoDataConstants.IdentityTraceSourceName);

        /// <summary>
        /// Configuration 
        /// </summary>
        protected AdoPersistenceConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AdoPersistenceConfigurationSection>();

        /// <summary>
        /// Verify principal
        /// </summary>
        private void VerifyPrincipal(IPrincipal principal, String policyId)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));
            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, policyId).Demand();

        }

        /// <summary>
        /// Adds the specified users to the specified roles
        /// </summary>
        public void AddUsersToRoles(string[] users,  string[] roles, IPrincipal principal)
        {
            this.VerifyPrincipal(principal, PermissionPolicyIdentifiers.AlterRoles);

            // Add users to role
            using (DataContext dataContext = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    dataContext.Open();
                    using (var tx = dataContext.BeginTransaction())
                    {
                        try
                        {
                            foreach (var un in users)
                            {
                                DbSecurityUser user = dataContext.SingleOrDefault<DbSecurityUser>(u => u.UserName.ToLower() == un.ToLower());
                                if (user == null)
                                    throw new KeyNotFoundException(String.Format("Could not locate user {0}", un));
                                foreach (var rol in roles)
                                {
                                    DbSecurityRole role = dataContext.SingleOrDefault<DbSecurityRole>(r => r.Name == rol);
                                    if (role == null)
                                        throw new KeyNotFoundException(String.Format("Could not locate role {0}", rol));
                                    if (!dataContext.Any<DbSecurityUserRole>(o => o.RoleKey == role.Key && o.UserKey == user.Key))
                                    {
                                        // Insert
                                        dataContext.Insert(new DbSecurityUserRole() { UserKey = user.Key, RoleKey = role.Key });
                                    }
                                }
                            }
                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error,  "Error adding {0} to {1} : {2}", String.Join(",", users), String.Join(",", roles), e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Create a role
        /// </summary>
        public void CreateRole(string roleName, IPrincipal principal)
        {

            this.VerifyPrincipal(principal, PermissionPolicyIdentifiers.CreateRoles);

            // Add users to role
            using (DataContext dataContext = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    dataContext.Open();
                    dataContext.EstablishProvenance(principal, null);
                    using (var tx = dataContext.BeginTransaction())
                    {
                        try
                        {
                            DbSecurityUser user = dataContext.SingleOrDefault<DbSecurityUser>(u => u.UserName.ToLower() == principal.Identity.Name.ToLower());

                            // Insert
                            dataContext.Insert(new DbSecurityRole()
                            {
                                CreatedByKey = dataContext.ContextId,
                                Name = roleName
                            });
                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error,  "Error creating role {0} : {1}", roleName, e);
                    throw;
                }
            }

        }

        /// <summary>
        /// Find all users in a role
        /// </summary>
        public string[] FindUsersInRole(string role)
        {
            using (DataContext dataContext = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    dataContext.Open();
                    var securityRole = dataContext.SingleOrDefault<DbSecurityRole>(r => r.Name == role);
                    if (securityRole == null)
                        throw new KeyNotFoundException(String.Format("Role {0} not found", role));

                    var query = dataContext.CreateSqlStatement<DbSecurityUserRole>().SelectFrom()
                        .AutoJoin<DbSecurityUser, DbSecurityUserRole>()
                        .Where(o => o.RoleKey == securityRole.Key);

                    return dataContext.Query<DbSecurityUser>(query).Select(o => o.UserName).ToArray();
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error,  "Error finding users for role {0} : {1}", role, e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Get all roles
        /// </summary>
        /// <returns></returns>
        public string[] GetAllRoles()
        {
            using (var dataContext = this.m_configuration.Provider.GetReadonlyConnection())
                try
                {
                    dataContext.Open();
                    return dataContext.Query<DbSecurityRole>(o => o.ObsoletionTime != null).Select(o => o.Name).ToArray();
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error,  "Error executing GetAllRoles() : {0}", e);
                    throw;
                }
        }

        /// <summary>
        /// Get all rolesfor user
        /// </summary>
        /// <returns></returns>
        public string[] GetAllRoles(String userName)
        {
            using (var dataContext = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    dataContext.Open();
                    var securityUser = dataContext.SingleOrDefault<DbSecurityUser>(u => u.UserName.ToLower() == userName.ToLower());
                    if (securityUser == null)
                        throw new KeyNotFoundException(String.Format("User {0} not found", userName));

                    var query = dataContext.CreateSqlStatement<DbSecurityUserRole>().SelectFrom(typeof(DbSecurityRole), typeof(DbSecurityUserRole))
                        .AutoJoin<DbSecurityRole, DbSecurityUserRole>()
                        .Where(o => o.UserKey == securityUser.Key);

                    return dataContext.Query<DbSecurityRole>(query).Select(o => o.Name).ToArray();
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error,  "Error executing getting roles for {0} : {1}", userName, e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Determine if the user is in the specified role
        /// </summary>
        public bool IsUserInRole(IPrincipal principal, string roleName)
        {
            return this.IsUserInRole(principal.Identity.Name, roleName);
        }

        /// <summary>
        /// Determine if user is in role
        /// </summary>
        public bool IsUserInRole(string userName, string roleName)
        {
            using (var dataContext = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    dataContext.Open();
                    DbSecurityUser user = dataContext.SingleOrDefault<DbSecurityUser>(u => u.UserName.ToLower() == userName.ToLower());
                    if (user == null)
                        throw new KeyNotFoundException(String.Format("Could not locate user {0}", userName));
                    DbSecurityRole role = dataContext.SingleOrDefault<DbSecurityRole>(r => r.Name == roleName);
                    if (role == null)
                        throw new KeyNotFoundException(String.Format("Could not locate role {0}", roleName));

                    // Select
                    return dataContext.Any<DbSecurityUserRole>(o => o.UserKey == user.Key && o.RoleKey == role.Key);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error,  "Error determining role membership of user {0} in {1} : {2}", userName, roleName, e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Remove users from roles
        /// </summary>
        public void RemoveUsersFromRoles(string[] users, string[] roles, IPrincipal principal)
        {
            this.VerifyPrincipal(principal, PermissionPolicyIdentifiers.AlterRoles);

            using (DataContext dataContext = this.m_configuration.Provider.GetWriteConnection())
                try
                {
                    dataContext.Open();
                    using (var tx = dataContext.BeginTransaction())
                    {
                        try
                        {
                            foreach (var un in users)
                            {
                                DbSecurityUser user = dataContext.SingleOrDefault<DbSecurityUser>(u => u.UserName.ToLower() == un.ToLower());
                                if (user == null)
                                    throw new KeyNotFoundException(String.Format("Could not locate user {0}", un));
                                foreach (var rol in roles)
                                {
                                    DbSecurityRole role = dataContext.SingleOrDefault<DbSecurityRole>(r => r.Name == rol);
                                    if (role == null)
                                        throw new KeyNotFoundException(String.Format("Could not locate role {0}", rol));

                                    // Insert
                                    dataContext.Delete<DbSecurityUserRole>(o => o.UserKey == user.Key && o.RoleKey == role.Key);
                                }
                            }
                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }

                    }
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error,  "Error removing {0} from {1} : {2}", String.Join(",", users), String.Join(",", roles), e);
                    throw;
                }
        }
    }
}
