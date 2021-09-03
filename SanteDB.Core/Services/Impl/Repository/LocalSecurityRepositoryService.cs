/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2021-8-5
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Server.Core.Security;
using SanteDB.Server.Core.Security.Attribute;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;

namespace SanteDB.Server.Core.Services.Impl
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

        // Tracer for this service
        private Tracer m_traceSource = new Tracer(SanteDBConstants.ServiceTraceSourceName);

        // User repo
        private IRepositoryService<SecurityUser> m_userRepository;
        // App repo
        private IRepositoryService<SecurityApplication> m_applicationRepository;
        // Device repo
        private IRepositoryService<SecurityDevice> m_deviceRepository;
        // Policy repo
        private IRepositoryService<SecurityPolicy> m_policyRepository;
        // Role repository
        private IRepositoryService<SecurityRole> m_roleRepository;
        // User Entity repository
        private IRepositoryService<UserEntity> m_userEntityRepository;
        // Provenance 
        private IDataPersistenceService<SecurityProvenance> m_provenancePersistence;
        // IdP
        private IIdentityProviderService m_identityProviderService;
        // App IdP
        private IApplicationIdentityProviderService m_applicationIdentityProvider;
        // Dev IdP
        private IDeviceIdentityProviderService m_deviceIdentityProvider;
        // Role provider
        private IRoleProviderService m_roleProvider;

        /// <summary>
        /// Creates a new local security repository service
        /// </summary>
        public LocalSecurityRepositoryService(
            IRepositoryService<SecurityUser> userRepository,
            IRepositoryService<SecurityApplication> applicationRepository,
            IRepositoryService<SecurityRole> roleRepository,
            IRepositoryService<SecurityDevice> deviceRepository,
            IRepositoryService<SecurityPolicy> policyRepository,
            IRepositoryService<UserEntity> userEntityRepository,
            IDataPersistenceService<SecurityProvenance> provenanceRepository,
            IRoleProviderService roleProviderService,
            IIdentityProviderService identityProviderService,
            IApplicationIdentityProviderService applicationIdentityProvider,
            IDeviceIdentityProviderService deviceIdentityProvider)
        {
            this.m_userRepository = userRepository;
            this.m_applicationIdentityProvider = applicationIdentityProvider;
            this.m_applicationRepository = applicationRepository;
            this.m_identityProviderService = identityProviderService;
            this.m_provenancePersistence = provenanceRepository;
            this.m_deviceIdentityProvider = deviceIdentityProvider;
            this.m_deviceRepository = deviceRepository;
            this.m_policyRepository = policyRepository;
            this.m_roleRepository = roleRepository;
            this.m_userEntityRepository = userEntityRepository;
            this.m_roleProvider = roleProviderService;
        }

        /// <summary>
        /// Changes a user's password.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="password">The new password of the user.</param>
        /// <returns>Returns the updated user.</returns>
        public SecurityUser ChangePassword(Guid userId, string password)
        {
            this.m_traceSource.TraceEvent(EventLevel.Verbose, "Changing user password");
            var securityUser = this.m_userRepository?.Get(userId);
            if (securityUser == null)
                throw new KeyNotFoundException("Cannot locate security user");
            this.m_identityProviderService.ChangePassword(securityUser.UserName, password, AuthenticationContext.Current.Principal);
            return securityUser;
        }

        /// <summary>
        /// Change password
        /// </summary>
        public void ChangePassword(string userName, string password)
        {
            this.m_identityProviderService.ChangePassword(userName, password, AuthenticationContext.Current.Principal);
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
            return this.m_userRepository.Insert(userInfo);
        }

        /// <summary>
        /// Get the policy information in the model format
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        public SecurityPolicy GetPolicy(string policyOid)
        {
            int tr = 0;
            return this.m_policyRepository.Find(o => o.Oid == policyOid, 0, 1, out tr).SingleOrDefault();
        }


        /// <summary>
        /// Get the security provenance 
        /// </summary>
        public SecurityProvenance GetProvenance(Guid provenanceId)
        {
            return this.m_provenancePersistence.Get(provenanceId, null, true, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Get the specified role 
        /// </summary>
        public SecurityRole GetRole(string roleName)
        {
            int tr = 0;
            return this.m_roleRepository?.Find(o => o.Name == roleName, 0, 1, out tr).SingleOrDefault();
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
            return this.m_userRepository.Find(u => u.UserName == userName, 0, 1, out tr).FirstOrDefault();
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
            return this.m_userEntityRepository?.Find(o => o.SecurityUser.UserName == identity.Name, 0, 1, out t).FirstOrDefault();
        }


        /// <summary>
        /// Locks a specific user.
        /// </summary>
        /// <param name="userId">The id of the user to lock.</param>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterIdentity)]
        public void LockUser(Guid userId)
        {
            this.m_traceSource.TraceEvent(EventLevel.Verbose, "Locking user {0}", userId);

            var securityUser = this.m_userRepository.Get(userId);
            if (securityUser == null)
                throw new KeyNotFoundException(userId.ToString());
            this.m_identityProviderService.SetLockout(securityUser.UserName, true, AuthenticationContext.Current.Principal);
        }


        /// <summary>
        /// Unlocks a specific user.
        /// </summary>
        /// <param name="userId">The id of the user to be unlocked.</param>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterIdentity)]
        public void UnlockUser(Guid userId)
        {
            this.m_traceSource.TraceEvent(EventLevel.Verbose, "Unlocking user {0}", userId);

            var securityUser = this.m_userRepository?.Get(userId);
            if (securityUser == null)
                throw new KeyNotFoundException(userId.ToString());
            this.m_identityProviderService.SetLockout(securityUser.UserName, false, AuthenticationContext.Current.Principal);

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
            
            if (!user.Key.HasValue)
                user = this.GetUser(user.UserName);
            this.m_roleProvider.RemoveUsersFromRoles(new String[] { user.UserName }, this.m_roleProvider.GetAllRoles().Where(o => !roles.Contains(o)).ToArray(), AuthenticationContext.Current.Principal);
            this.m_roleProvider.AddUsersToRoles(new string[] { user.UserName }, roles, AuthenticationContext.Current.Principal);
        }


        /// <summary>
        /// Lock a device
        /// </summary>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateDevice)]
        public void LockDevice(Guid key)
        {
            this.m_traceSource.TraceWarning("Locking device {0}", key);

            var securityDevice = this.m_deviceRepository?.Get(key);
            if (securityDevice == null)
                throw new KeyNotFoundException(key.ToString());

            this.m_deviceIdentityProvider.SetLockout(securityDevice.Name, true, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Locks the specified application
        /// </summary>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateApplication)]
        public void LockApplication(Guid key)
        {
            this.m_traceSource.TraceWarning("Locking application {0}", key);
            var securityApplication = this.m_applicationRepository?.Get(key);
            if (securityApplication == null)
                throw new KeyNotFoundException(key.ToString());

            this.m_applicationIdentityProvider.SetLockout(securityApplication.Name, true, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Unlocks the specified device
        /// </summary>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateDevice)]
        public void UnlockDevice(Guid key)
        {
            this.m_traceSource.TraceWarning("Unlocking device {0}", key);

            var securityDevice = this.m_deviceRepository?.Get(key);
            if (securityDevice == null)
                throw new KeyNotFoundException(key.ToString());

            this.m_deviceIdentityProvider.SetLockout(securityDevice.Name, false, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Unlock the specified application
        /// </summary>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateApplication)]
        public void UnlockApplication(Guid key)
        {
            this.m_traceSource.TraceWarning("Unlocking application {0}", key);

            var securityApplication = this.m_applicationRepository?.Get(key);
            if (securityApplication == null)
                throw new KeyNotFoundException(key.ToString());

            this.m_applicationIdentityProvider.SetLockout(securityApplication.Name, false, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Find provenance
        /// </summary>
        public IEnumerable<SecurityProvenance> FindProvenance(Expression<Func<SecurityProvenance, bool>> query, int offset, int? count, out int totalResults, Guid queryId, params ModelSort<SecurityProvenance>[] orderBy)
        {
            if (this.m_provenancePersistence is IStoredQueryDataPersistenceService<SecurityProvenance> isq)
                return isq.Query(query, queryId, offset, count, out totalResults, AuthenticationContext.Current.Principal, orderBy);
            else
                return this.m_provenancePersistence.Query(query, offset, count, out totalResults, AuthenticationContext.Current.Principal, orderBy);
        }

        /// <summary>
        /// Get the security entity from the specified principal
        /// </summary>
        /// <param name="principal">The principal to be fetched</param>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        public SecurityEntity GetSecurityEntity(IPrincipal principal)
        {
            if (principal.Identity is DeviceIdentity deviceIdentity) // Device credential 
            {
                return this.GetDevice(deviceIdentity);
            }
            else if (principal.Identity is Security.ApplicationIdentity applicationIdentity) //
            {
                return this.GetApplication(applicationIdentity);
            }
            else
            {
                return this.GetUser(principal.Identity);
            }
        }

        /// <summary>
        /// Get device from name
        /// </summary>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        public SecurityDevice GetDevice(string deviceName)
        {
            if (String.IsNullOrEmpty(deviceName))
            {
                throw new ArgumentNullException(nameof(deviceName));
            }
            return this.m_deviceRepository.Find(o => o.Name == deviceName, 0, 1, out int _).FirstOrDefault();
        }

        /// <summary>
        /// Get application from name
        /// </summary>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        public SecurityApplication GetApplication(string applicationName)
        {
            if (String.IsNullOrEmpty(applicationName))
            {
                throw new ArgumentNullException(nameof(applicationName));
            }

            return this.m_applicationRepository.Find(o => o.Name == applicationName, 0, 1, out int _).FirstOrDefault();
        }

        /// <summary>
        /// Get device 
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        public SecurityDevice GetDevice(IIdentity identity)
        {
            if(identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            return this.GetDevice(identity.Name);
        }

        /// <summary>
        /// Get application
        /// </summary>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        public SecurityApplication GetApplication(IIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            return this.GetApplication(identity.Name);
        }
    }
}