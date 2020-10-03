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
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Represents a security repository service that uses the direct local services
    /// </summary>
    public class LocalSecurityRepositoryService : ISecurityRepositoryService
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Local Security Repository Service";

        private Tracer m_traceSource = new Tracer(SanteDBConstants.ServiceTraceSourceName);

        /// <summary>
        /// Changes a user's password.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="password">The new password of the user.</param>
        /// <returns>Returns the updated user.</returns>
        public SecurityUser ChangePassword(Guid userId, string password)
		{
            this.m_traceSource.TraceEvent(EventLevel.Verbose, "Changing user password");
			var securityUser = ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityUser>>()?.Get(userId);
			if (securityUser == null)
				throw new KeyNotFoundException("Cannot locate security user");
			var iids = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
			if (iids == null) throw new InvalidOperationException("Cannot find identity provider service");
			iids.ChangePassword(securityUser.UserName, password, AuthenticationContext.Current.Principal);
			return securityUser;
		}

        /// <summary>
        /// Change password
        /// </summary>
        public void ChangePassword(string userName, string password)
        {
            ApplicationServiceContext.Current.GetService<IIdentityProviderService>().ChangePassword(userName, password, AuthenticationContext.Current.Principal);
        }
        
		/// <summary>
		/// Creates a user with a specified password.
		/// </summary>
		/// <param name="userInfo">The security user.</param>
		/// <param name="password">The password.</param>
		/// <returns>Returns the newly created user.</returns>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateIdentity)]
		public SecurityUser CreateUser(SecurityUser userInfo, string password)
		{
            userInfo.Password = password;
            return ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityUser>>().Insert(userInfo);
		}
        
        /// <summary>
        /// Get the policy information in the model format
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        public SecurityPolicy GetPolicy(string policyOid)
        {
            int tr = 0;
            return ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityPolicy>>().Find(o => o.Oid == policyOid, 0, 1, out tr).SingleOrDefault();
        }


        /// <summary>
        /// Get the security provenance 
        /// </summary>
        public SecurityProvenance GetProvenance(Guid provenanceId)
        {
            return ApplicationServiceContext.Current.GetService<IDataPersistenceService<SecurityProvenance>>().Get(provenanceId, null, true, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Get the specified role 
        /// </summary>
        public SecurityRole GetRole(string roleName)
        {
            int tr = 0;
            return ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityRole>>()?.Find(o => o.Name == roleName, 0, 1, out tr).SingleOrDefault();
        }
        
        /// <summary>
        /// Gets a specific user.
        /// </summary>
        /// <param name="userName">The id of the user to retrieve.</param>
        /// <returns>Returns the user.</returns>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        public SecurityUser GetUser(String userName)
        {
            int tr = 0;
            // As the identity service may be LDAP, best to call it to get an identity name
            return ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityUser>>().Find(u => u.UserName == userName, 0, 1, out tr).FirstOrDefault();
        }

        /// <summary>
        /// Get the specified user based on identity
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
		public SecurityUser GetUser(IIdentity identity)
		{
            return this.GetUser(identity.Name);
		}

		/// <summary>
		/// Get user entity from identity
		/// </summary>
		public UserEntity GetUserEntity(IIdentity identity)
		{
            int t = 0;
            return ApplicationServiceContext.Current.GetService<IRepositoryService<UserEntity>>()?.Find(o=>o.SecurityUser.UserName == identity.Name, 0, 1, out t).FirstOrDefault();
		}
        

        /// <summary>
        /// Locks a specific user.
        /// </summary>
        /// <param name="userId">The id of the user to lock.</param>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterIdentity)]
		public void LockUser(Guid userId)
		{
			this.m_traceSource.TraceEvent(EventLevel.Verbose, "Locking user {0}", userId);

			var iids = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
			if (iids == null)
				throw new InvalidOperationException("Missing identity provider service");

			var securityUser = ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityUser>>()?.Get(userId);
            if (securityUser == null)
                throw new KeyNotFoundException(userId.ToString());
			iids.SetLockout(securityUser.UserName, true, AuthenticationContext.Current.Principal);
		}

     
		/// <summary>
		/// Unlocks a specific user.
		/// </summary>
		/// <param name="userId">The id of the user to be unlocked.</param>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterIdentity)]
		public void UnlockUser(Guid userId)
		{
			this.m_traceSource.TraceEvent(EventLevel.Verbose, "Unlocking user {0}", userId);

			var iids = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
			if (iids == null)
				throw new InvalidOperationException("Missing identity provider service");

			var securityUser = ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityUser>>()?.Get(userId);
            if (securityUser == null)
                throw new KeyNotFoundException(userId.ToString());
			iids.SetLockout(securityUser.UserName, false, AuthenticationContext.Current.Principal);

        }
        
        /// <summary>
        /// Get the specified provider entity
        /// </summary>
        public Provider GetProviderEntity(IIdentity identity)
        {
            int t;
            return ApplicationServiceContext.Current.GetService<IRepositoryService<Provider>>()
                .Find(o => o.Relationships.Where(r => r.RelationshipType.Mnemonic == "AssignedEntity").Any(r => (r.SourceEntity as UserEntity).SecurityUser.UserName == identity.Name), 0, 1, out t).FirstOrDefault();
        }

        /// <summary>
        /// Set the user's roles to only those in the roles array
        /// </summary>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterRoles)]
        public void SetUserRoles(SecurityUser user, string[] roles)
        {
            var irps = ApplicationServiceContext.Current.GetService<IRoleProviderService>();
            if (irps == null)
                throw new InvalidOperationException("Cannot find role provider service");

            if (!user.Key.HasValue)
                user = this.GetUser(user.UserName);
            irps.RemoveUsersFromRoles(new String[] { user.UserName }, irps.GetAllRoles().Where(o => !roles.Contains(o)).ToArray(), AuthenticationContext.Current.Principal);
            irps.AddUsersToRoles(new string[] { user.UserName }, roles, AuthenticationContext.Current.Principal);
        }


        /// <summary>
        /// Lock a device
        /// </summary>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateDevice)]
        public void LockDevice(Guid key)
        {

            this.m_traceSource.TraceWarning("Locking device {0}", key);

            var iids = ApplicationContext.Current.GetService<IDeviceIdentityProviderService>();
            if (iids == null)
                throw new InvalidOperationException("Missing identity provider service");

            var securityDevice = ApplicationContext.Current.GetService<IRepositoryService<SecurityDevice>>()?.Get(key);
            if (securityDevice == null)
                throw new KeyNotFoundException(key.ToString());

            iids.SetLockout(securityDevice.Name, true, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Locks the specified application
        /// </summary>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateApplication)]
        public void LockApplication(Guid key)
        {
            this.m_traceSource.TraceWarning("Locking application {0}", key);

            var iids = ApplicationContext.Current.GetService<IApplicationIdentityProviderService>();
            if (iids == null)
                throw new InvalidOperationException("Missing identity provider service");

            var securityApplication = ApplicationContext.Current.GetService<IRepositoryService<SecurityApplication>>()?.Get(key);
            if (securityApplication == null)
                throw new KeyNotFoundException(key.ToString());

            iids.SetLockout(securityApplication.Name, true, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Unlocks the specified device
        /// </summary>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateDevice)]
        public void UnlockDevice(Guid key)
        {
            this.m_traceSource.TraceWarning("Unlocking device {0}", key);

            var iids = ApplicationContext.Current.GetService<IDeviceIdentityProviderService>();
            if (iids == null)
                throw new InvalidOperationException("Missing identity provider service");

            var securityDevice = ApplicationContext.Current.GetService<IRepositoryService<SecurityDevice>>()?.Get(key);
            if (securityDevice == null)
                throw new KeyNotFoundException(key.ToString());

            iids.SetLockout(securityDevice.Name, false, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Unlock the specified application
        /// </summary>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateApplication)]
        public void UnlockApplication(Guid key)
        {
            this.m_traceSource.TraceWarning("Unlocking application {0}", key);

            var iids = ApplicationContext.Current.GetService<IApplicationIdentityProviderService>();
            if (iids == null)
                throw new InvalidOperationException("Missing identity provider service");

            var securityApplication = ApplicationContext.Current.GetService<IRepositoryService<SecurityApplication>>()?.Get(key);
            if (securityApplication == null)
                throw new KeyNotFoundException(key.ToString());

            iids.SetLockout(securityApplication.Name, false, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Find provenance
        /// </summary>
        public IEnumerable<SecurityProvenance> FindProvenance(Expression<Func<SecurityProvenance, bool>> query, int offset, int? count, out int totalResults, Guid queryId, params ModelSort<SecurityProvenance>[] orderBy)
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<SecurityProvenance>>();
            if (persistenceService is IStoredQueryDataPersistenceService<SecurityProvenance>)
                return (persistenceService as IStoredQueryDataPersistenceService<SecurityProvenance>).Query(query, queryId, offset, count, out totalResults, AuthenticationContext.Current.Principal, orderBy);
            else
                return persistenceService.Query(query, offset, count, out totalResults, AuthenticationContext.Current.Principal, orderBy);
        }

        /// <summary>
        /// Get the security entity from the specified principal
        /// </summary>
        /// <param name="principal">The principal to be fetched</param>
        public SecurityEntity GetSecurityEntity(IPrincipal principal)
        {
            if (principal.Identity is DeviceIdentity deviceIdentity) // Device credential 
            {
                var sid = deviceIdentity.FindFirst(SanteDBClaimTypes.Sid)?.Value;
                if (!String.IsNullOrEmpty(sid))
                    return ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityDevice>>().Get(Guid.Parse(sid));
                else
                    return null;
            }
            else if (principal.Identity is Security.ApplicationIdentity applicationIdentity) //
            {
                var sid = applicationIdentity.FindFirst(SanteDBClaimTypes.Sid)?.Value;
                if (!String.IsNullOrEmpty(sid))
                    return ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityApplication>>().Get(Guid.Parse(sid));
                else
                    return null;
            }
            else
            {
                return this.GetUser(principal.Identity);
            }
        }
    }
}