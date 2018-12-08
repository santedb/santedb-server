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
 * Date: 2018-6-22
 */
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Represents a security repository service that uses the direct local services
    /// </summary>
    public class LocalSecurityRepositoryService : ISecurityRepositoryService, 
        ISecurityAuditEventSource
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Local Security Repository Service";

        private TraceSource m_traceSource = new TraceSource(SanteDBConstants.ServiceTraceSourceName);

        /// <summary>
        /// Indicates security attributes have changed
        /// </summary>
        public event EventHandler<SecurityAuditDataEventArgs> SecurityAttributesChanged;
        public event EventHandler<SecurityAuditDataEventArgs> SecurityResourceCreated;
        public event EventHandler<SecurityAuditDataEventArgs> SecurityResourceDeleted;

        /// <summary>
        /// Changes a user's password.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="password">The new password of the user.</param>
        /// <returns>Returns the updated user.</returns>
        public SecurityUser ChangePassword(Guid userId, string password)
		{
            this.m_traceSource.TraceEvent(TraceEventType.Verbose, 0, "Changing user password");
			var securityUser = ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityUser>>()?.Get(userId);
			if (securityUser == null)
				throw new KeyNotFoundException("Cannot locate security user");
			var iids = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
			if (iids == null) throw new InvalidOperationException("Cannot find identity provider service");
			iids.ChangePassword(securityUser.UserName, password, AuthenticationContext.Current.Principal);
            this.SecurityAttributesChanged?.Invoke(this, new SecurityAuditDataEventArgs(securityUser, "Password"));
			return securityUser;
		}

        /// <summary>
        /// Change password
        /// </summary>
        public void ChangePassword(string userName, string password)
        {
            ApplicationServiceContext.Current.GetService<IIdentityProviderService>().ChangePassword(userName, password, AuthenticationContext.Current.Principal);
            this.SecurityAttributesChanged?.Invoke(this, new SecurityAuditDataEventArgs(userName, "Password"));
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
			this.m_traceSource.TraceEvent(TraceEventType.Verbose, 0, "Locking user {0}", userId);

			var iids = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
			if (iids == null)
				throw new InvalidOperationException("Missing identity provider service");

			var securityUser = ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityUser>>()?.Get(userId);
            if (securityUser == null)
                throw new KeyNotFoundException(userId.ToString());
			iids.SetLockout(securityUser.UserName, true, AuthenticationContext.Current.Principal);
            this.SecurityAttributesChanged?.Invoke(this, new SecurityAuditDataEventArgs(securityUser, "Lockout=True"));
		}

     
		/// <summary>
		/// Unlocks a specific user.
		/// </summary>
		/// <param name="userId">The id of the user to be unlocked.</param>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterIdentity)]
		public void UnlockUser(Guid userId)
		{
			this.m_traceSource.TraceEvent(TraceEventType.Verbose, 0, "Unlocking user {0}", userId);

			var iids = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
			if (iids == null)
				throw new InvalidOperationException("Missing identity provider service");

			var securityUser = ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityUser>>()?.Get(userId);
            if (securityUser == null)
                throw new KeyNotFoundException(userId.ToString());
			iids.SetLockout(securityUser.UserName, false, AuthenticationContext.Current.Principal);
            this.SecurityAttributesChanged?.Invoke(this, new SecurityAuditDataEventArgs(securityUser, "Lockout=False"));

        }

    }
}