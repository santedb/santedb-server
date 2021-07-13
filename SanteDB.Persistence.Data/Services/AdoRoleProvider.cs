﻿using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Configuration;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Persistence.Data.Services
{
    /// <summary>
    /// A role provider which uses the ADO.NET classes
    /// </summary>
    public class AdoRoleProvider : IRoleProviderService
    {
        /// <summary>
        /// Get the service name
        /// </summary>
        public string ServiceName => "ADO.NET Role Provider Service";

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(AdoRoleProvider));

        // Policy enforcement
        private IPolicyEnforcementService m_policyEnforcement;

        // Configuration
        private AdoPersistenceConfigurationSection m_configuration;

        /// <summary>
        /// ADO Role Provider
        /// </summary>
        public AdoRoleProvider(IPolicyEnforcementService policyEnforcementService, IConfigurationManager configuration)
        {
            this.m_policyEnforcement = policyEnforcementService;
            this.m_configuration = configuration.GetSection<AdoPersistenceConfigurationSection>();
        }

        /// <summary>
        /// Adds the specifed <paramref name="users"/> to the <paramref name="roles"/>
        /// </summary>
        public void AddUsersToRoles(string[] users, string[] roles, IPrincipal principal)
        {
            if(users == null)
            {
                throw new ArgumentNullException(nameof(users), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(roles == null)
            {
                throw new ArgumentNullException(nameof(roles), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            this.m_policyEnforcement.Demand(PermissionPolicyIdentifiers.AlterRoles, principal);

            using(var context= this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();
                    using(var tx = context.BeginTransaction())
                    {
                        string[] lroles = roles.Select(o => o.ToLowerInvariant()).ToArray(), lusers = users.Select(o => o.ToLowerInvariant()).ToArray();

                        var roleIds = context.Query<DbSecurityRole>(o => lroles.Contains(o.Name.ToLowerInvariant())).Select(o=>o.Key);
                        var userIds = context.Query<DbSecurityUser>(o => lusers.Contains(o.UserName.ToLowerInvariant())).Select(o => o.Key);

                        // Add 
                        foreach(var rol in roleIds.SelectMany(r=>userIds.ToArray().Select(u=>new { U = u, R = r })))
                        {
                            if (!context.Any<DbSecurityUserRole>(r => r.RoleKey == rol.R && r.UserKey == rol.U))
                                context.Insert(new DbSecurityUserRole()
                                {
                                    RoleKey = rol.R,
                                    UserKey = rol.U
                                });
                        }

                        tx.Commit();
                    }
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error assigning users to roles: {0}", e);
                    throw new DataPersistenceException(ErrorMessages.ERR_SEC_ROL_ASSIGN, e);
                }
            }
        }

        /// <summary>
        /// Creates a new role in the role provider
        /// </summary>
        public void CreateRole(string roleName, IPrincipal principal)
        {
            if(String.IsNullOrEmpty(roleName))
            {
                throw new ArgumentNullException(nameof(roleName), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            this.m_policyEnforcement.Demand(PermissionPolicyIdentifiers.CreateRoles, principal);

            using(var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    var dbRole = new DbSecurityRole()
                    {
                        CreatedByKey = context.EstablishProvenance(principal, null),
                        CreationTime = DateTimeOffset.Now,
                        Name = roleName
                    };

                    dbRole = context.Insert(dbRole);

                }
                catch(Exception e)
                {
                    this.m_tracer.TraceError("Error creating role: {0}", e);
                    throw new DataPersistenceException(ErrorMessages.ERR_ROL_CREATE_GEN, e);
                }
            }
        }
        
        /// <summary>
        /// Finds a list of all users in the role
        /// </summary>
        public string[] FindUsersInRole(string role)
        {
            if(String.IsNullOrEmpty(role))
            {
                throw new ArgumentException(nameof(role), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            using(var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();

                    return context.Query<DbSecurityUser>(context.CreateSqlStatement<DbSecurityUser>()
                        .SelectFrom()
                        .InnerJoin<DbSecurityUserRole>(o => o.Key, o => o.UserKey)
                        .InnerJoin<DbSecurityUserRole, DbSecurityRole>(o => o.RoleKey, o => o.Key)
                        .Where<DbSecurityRole>(o => o.Name.ToLowerInvariant() == role.ToLowerInvariant())
                        ).Select(o=>o.UserName).ToArray();
                }
                catch(Exception e)
                {
                    this.m_tracer.TraceError("Error querying users in role: {0}", e);
                    throw new DataPersistenceException(ErrorMessages.ERR_ROL_QUERY, e);
                }
            }
        }

        /// <summary>
        /// Get all roles
        /// </summary>
        public string[] GetAllRoles()
        {
            using(var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();

                    return context.Query<DbSecurityRole>(o => o.ObsoletionTime == null).Select(o => o.Name).ToArray();
                }
                catch(Exception e)
                {
                    this.m_tracer.TraceError("Error quering roles : {0}", e);
                    throw new DataPersistenceException(ErrorMessages.ERR_ROL_QUERY, e);
                }
            }
        }

        /// <summary>
        /// Get all roles for the specified user
        /// </summary>
        public string[] GetAllRoles(string userName)
        {
            if (String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            using(var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();

                    return context.Query<DbSecurityRole>(context.CreateSqlStatement<DbSecurityRole>()
                        .SelectFrom()
                        .InnerJoin<DbSecurityUserRole>(o => o.Key, o => o.RoleKey)
                        .InnerJoin<DbSecurityUserRole, DbSecurityUser>(o => o.UserKey, o => o.Key)
                        .Where<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userName.ToLowerInvariant() && o.ObsoletionTime == null)
                        ).Select(o => o.Name).ToArray();
                }
                catch(Exception e)
                {
                    this.m_tracer.TraceError("Error getting roles for user: {0}", e);
                    throw new DataPersistenceException(ErrorMessages.ERR_ROL_QUERY, e);
                }
            } 
        }

        /// <summary>
        /// Determine if the specified user is in the specified role
        /// </summary>
        public bool IsUserInRole(string userName, string roleName)
        {
            if(String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(String.IsNullOrEmpty(roleName))
            {
                throw new ArgumentNullException(nameof(roleName), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            using(var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();

                    return context.Any(context.CreateSqlStatement<DbSecurityUserRole>()
                        .SelectFrom()
                        .InnerJoin<DbSecurityUserRole, DbSecurityUser>(o => o.UserKey, o => o.Key)
                        .InnerJoin<DbSecurityUserRole, DbSecurityRole>(o => o.RoleKey, o => o.Key)
                        .Where<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userName.ToLowerInvariant())
                        .And<DbSecurityRole>(o => o.Name.ToLowerInvariant() == roleName.ToLowerInvariant())
                        );
                }
                catch(Exception e)
                {
                    this.m_tracer.TraceError("Error fetching roles: {0}", e);
                    throw new DataPersistenceException(ErrorMessages.ERR_ROL_QUERY, e);
                }
            }
        }

        /// <summary>
        /// Removes the specified users from the specified roles
        /// </summary>
        public void RemoveUsersFromRoles(string[] users, string[] roles, IPrincipal principal)
        {
            
            if(users == null)
            {
                throw new ArgumentNullException(nameof(users), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(roles == null)
            {
                throw new ArgumentNullException(nameof(roles), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            using(var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    using (var tx = context.BeginTransaction()) {

                        string[] lroles = roles.Select(o => o.ToLowerInvariant()).ToArray(), lusers = users.Select(o => o.ToLowerInvariant()).ToArray();

                        var roleIds = context.Query<DbSecurityRole>(o => lroles.Contains(o.Name.ToLowerInvariant())).Select(o => o.Key);
                        var userIds = context.Query<DbSecurityUser>(o => lusers.Contains(o.UserName.ToLowerInvariant())).Select(o => o.Key);

                        // Add 
                        foreach (var rol in roleIds.SelectMany(r => userIds.ToArray().Select(u => new { U = u, R = r })))
                        {
                            context.Delete<DbSecurityUserRole>(o => o.UserKey == rol.U && o.RoleKey == rol.R);
                        }

                        tx.Commit();
                    }
                }
                catch(Exception e)
                {
                    throw new DataPersistenceException(ErrorMessages.ERR_ROL_UNASSOC, e);
                }
            }
        }
    }
}