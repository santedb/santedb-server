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
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Server.Core.Security.Attribute;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Configuration;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using SanteDB.Persistence.Data.ADO.Security;
using SanteDB.Persistence.Data.ADO.Services.Persistence;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security;
using System.Security.Authentication;
using System.Security.Principal;
using SanteDB.Server.Core.Configuration;
using SanteDB.Server.Core.Security;
using SanteDB.Server.Core.Services;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Identity provider service
    /// </summary>
    [ServiceProvider("ADO.NET Identity Provider")]
    public sealed class AdoIdentityProvider : ISessionIdentityProviderService, IIdentityProviderService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public String ServiceName => "ADO.NET Identity Provider";

        // Sync lock
        private Object m_syncLock = new object();
        
        // Trace source
        private Tracer m_traceSource = new Tracer(AdoDataConstants.IdentityTraceSourceName);

        // Configuration
        private AdoPersistenceConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AdoPersistenceConfigurationSection>();

        // Security configuration
        private SecurityConfigurationSection m_securityConfiguration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SecurityConfigurationSection>();

        // Policy enforcement
        private IPolicyEnforcementService m_policyEnforcement;

        /// <summary>
        /// DI constructor
        /// </summary>
        public AdoIdentityProvider(IPolicyEnforcementService policyEnforcement)
        {
            this.m_policyEnforcement = policyEnforcement;
        }

        /// <summary>
        /// Fired prior to an authentication request being made
        /// </summary>
        public event EventHandler<AuthenticatingEventArgs> Authenticating;

        /// <summary>
        /// Fired after an authentication request has been made
        /// </summary>
        public event EventHandler<AuthenticatedEventArgs> Authenticated;


        /// <summary>
        /// Authenticate the user
        /// </summary>
        public IPrincipal Authenticate(string userName, string password)
        {
            var evt = new AuthenticatingEventArgs(userName);
            this.Authenticating?.Invoke(this, evt);
            if (evt.Cancel)
                throw new SecurityException("Authentication cancelled");

            try
            {
                var principal = AdoClaimsIdentity.Create(userName, password).CreateClaimsPrincipal();
                
                this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(userName, principal, true));
                return principal;
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Verbose, "Invalid credentials : {0}/{1}", userName, password);

                this.m_traceSource.TraceEvent(EventLevel.Error,  e.ToString());
                this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(userName, null, false));
                throw new AuthenticationException($"General exception authenticating {userName}", e);
            }
        }

        /// <summary>
        /// Gets an un-authenticated identity
        /// </summary>
        public IIdentity GetIdentity(string userName)
        {
            return AdoClaimsIdentity.Create(userName);
        }

        /// <summary>
        /// Gets an un-authenticated identity
        /// </summary>
        public IIdentity GetIdentity(Guid sid)
        {
            try
            {
                using (var dataContext = this.m_configuration.Provider.GetReadonlyConnection())
                {
                    dataContext.Open();
                    var name = dataContext.FirstOrDefault<DbSecurityUser>(o => o.Key == sid)?.UserName;
                    if (name == null)
                        return null;
                    else
                        return AdoClaimsIdentity.Create(name);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Authenticate the user using a TwoFactorAuthentication secret
        /// </summary>
        public IPrincipal Authenticate(string userName, string password, string tfaSecret)
        {
            // First, let's verify the TFA
            if (String.IsNullOrEmpty(userName))
                throw new ArgumentNullException(nameof(userName));
            else if (String.IsNullOrEmpty(tfaSecret))
                throw new ArgumentNullException(nameof(tfaSecret));

            // Authentication event args
            var evt = new AuthenticatingEventArgs(userName);
            this.Authenticating?.Invoke(this, evt);
            if (evt.Cancel)
                throw new SecurityException("Authentication cancelled");

            // Password hasher
            var hashingService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();
            tfaSecret = hashingService.ComputeHash(tfaSecret);

            // Try to authenticate
            try
            {
                using (var dataContext = this.m_configuration.Provider.GetWriteConnection())
                {
                    dataContext.Open();

                    using (var tx = dataContext.BeginTransaction())
                    {
                        try
                        {
                            var user = dataContext.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLower() == userName.ToLower());
                            if (user == null)
                                throw new KeyNotFoundException(userName);

                            var claims = dataContext.Query<DbUserClaim>(o => o.SourceKey == user.Key && (o.ClaimType == SanteDBClaimTypes.SanteDBOTAuthCode || o.ClaimType == SanteDBClaimTypes.SanteDBCodeAuth) && (!o.ClaimExpiry.HasValue || o.ClaimExpiry > DateTime.Now));
                            DbUserClaim tfaClaim = claims.FirstOrDefault(o => o.ClaimType == SanteDBClaimTypes.SanteDBOTAuthCode),
                                noPassword = claims.FirstOrDefault(o => o.ClaimType == SanteDBClaimTypes.SanteDBCodeAuth);

                            if (tfaClaim == null)
                                throw new InvalidOperationException("Cannot find appropriate claims for TFA");

                            // Expiry check
                            IClaimsPrincipal retVal = null;
                            if (String.IsNullOrEmpty(password) &&
                                Boolean.Parse(noPassword?.ClaimValue ?? "false") &&
                                tfaSecret == tfaClaim.ClaimValue) // TFA password hash sent as password, this is a password reset token - It will be set to expire ASAP
                            {
                                retVal = AdoClaimsIdentity.Create(dataContext, user, true, "Tfa+LastPasswordHash").CreateClaimsPrincipal();
                                if (retVal.Identity is IClaimsIdentity ici)
                                {
                                    ici.AddClaim(new SanteDBClaim(SanteDBClaimTypes.PurposeOfUse, PurposeOfUseKeys.SecurityAdmin.ToString()));
                                    ici.AddClaim(new SanteDBClaim(SanteDBClaimTypes.SanteDBScopeClaim, PermissionPolicyIdentifiers.LoginPasswordOnly));
                                    ici.AddClaim(new SanteDBClaim(SanteDBClaimTypes.SanteDBScopeClaim, PermissionPolicyIdentifiers.ReadMetadata));
                                    ici.RemoveClaim(retVal.FindFirst(SanteDBClaimTypes.Expiration));
                                    ici.AddClaim(new SanteDBClaim(SanteDBClaimTypes.Expiration, DateTime.Now.AddMinutes(5).ToString("o"))); // Special case, password
                                }
                            }
                            else if (!String.IsNullOrEmpty(password))
                            {
                                if (!user.TwoFactorEnabled || tfaSecret == tfaClaim.ClaimValue)
                                    retVal = this.Authenticate(userName, password) as IClaimsPrincipal;
                                else if(user.TwoFactorEnabled && user.TwoFactorMechnaismKey.HasValue)
                                {
                                    var suser = ApplicationServiceContext.Current.GetService<AdoPersistenceService>().GetMapper().MapDomainInstance<DbSecurityUser, SecurityUser>(user, true);
                                    var secretResponse = ApplicationServiceContext.Current.GetService<ITfaRelayService>()?.SendSecret(user.TwoFactorMechnaismKey.Value, suser);
                                    throw new AuthenticationException($"TFA_MISMATCH:{secretResponse}");
                                }
                            }
                            else
                                throw new PolicyViolationException(new GenericPrincipal(new GenericIdentity(userName), new string[0]), PermissionPolicyIdentifiers.Login, PolicyGrantType.Deny);

                            // Now we want to fire authenticated
                            this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(userName, retVal, true));

                            tx.Commit();
                            return retVal;
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Verbose, "Invalid credentials : {0}/{1}", userName, password);
                this.m_traceSource.TraceEvent(EventLevel.Error,  e.ToString());
                this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(userName, null, false));
                throw new AuthenticationException($"Invalid credentials for {userName}", e);
            }
        }

        /// <summary>
        /// Change the user's password
        /// </summary>
        public void ChangePassword(string userName, string newPassword, IPrincipal principal)
        {
            if (!principal.Identity.IsAuthenticated)
                throw new SecurityException("Principal must be authenticated");
            // Password failed validation
            if (ApplicationServiceContext.Current.GetService<IPasswordValidatorService>()?.Validate(newPassword) == false)
                throw new DetectedIssueException(new DetectedIssue(DetectedIssuePriorityType.Error, "err.password.complexity", "Password does not meet complexity requirements", DetectedIssueKeys.SecurityIssue));

            try
            {
                // Create the hasher and load the user
                using (var dataContext = this.m_configuration.Provider.GetWriteConnection())
                {
                    dataContext.Open();

                    using (var tx = dataContext.BeginTransaction())
                        try
                        {

                            var user = dataContext.SingleOrDefault<DbSecurityUser>(u => u.UserName.ToLower() == userName.ToLower() && !u.ObsoletionTime.HasValue);
                            if (user == null)
                                throw new InvalidOperationException(String.Format("Cannot locate user {0}", userName));

                            // Security check
                            var policyDecisionService = ApplicationServiceContext.Current.GetService<IPolicyDecisionService>();
                            var passwordHashingService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();

                            var pdpOutcome = policyDecisionService?.GetPolicyOutcome(principal, PermissionPolicyIdentifiers.ChangePassword);
                            if (!userName.Equals(principal.Identity.Name, StringComparison.OrdinalIgnoreCase) &&
                                pdpOutcome.HasValue &&
                                pdpOutcome != PolicyGrantType.Grant)
                                throw new PolicyViolationException(principal, PermissionPolicyIdentifiers.ChangePassword, pdpOutcome.Value);

                            var newPasswordHash = passwordHashingService.ComputeHash(newPassword + AdoDataConstants.PEPPER_CHARS[DateTime.Now.Ticks % AdoDataConstants.PEPPER_CHARS.Length]);
                            if (this.m_securityConfiguration == null ||
                                !this.m_securityConfiguration.GetSecurityPolicy<Boolean>(SecurityPolicyIdentification.PasswordHistory, false) ||
                                !dataContext.Any<DbSecurityUser>(o=>o.Key == user.Key && o.Password == newPasswordHash))
                                user.Password = newPasswordHash;
                            else
                                throw new DetectedIssueException(new DetectedIssue(DetectedIssuePriorityType.Error, "err.password.history", "Password does not meet complexity requirements", DetectedIssueKeys.SecurityIssue));


                            user.SecurityHash = Guid.NewGuid().ToString();
                            user.UpdatedByKey = dataContext.EstablishProvenance(principal, null);
                            user.UpdatedTime = DateTimeOffset.Now;

                            // Set expiration
                            var passwordAge = this.m_securityConfiguration?.GetSecurityPolicy<Int32>(SecurityPolicyIdentification.MaxPasswordAge, 3650);
                            if(passwordAge.HasValue)
                                user.PasswordExpiration = DateTime.Now.AddDays(passwordAge.Value);
                            dataContext.Update(user);
                            tx.Commit();
                        }
                        catch(Exception e)
                        {
                            tx.Rollback();
                            throw new DataPersistenceException("Error updating data store", e);
                        }

                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error,  e.ToString());
                throw new DataPersistenceException("Error changing password", e);
            }
        }

        /// <summary>
        /// Create a basic user
        /// </summary>
        public IIdentity CreateIdentity(string userName,  string password, IPrincipal principal)
        {

            this.VerifyPrincipal(principal, PermissionPolicyIdentifiers.CreateIdentity);

            if (String.IsNullOrEmpty(userName))
                throw new ArgumentNullException(nameof(userName));
            else if (String.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            this.m_traceSource.TraceInfo("Creating identity {0} ({1})", userName, principal);

            try
            {
                using (var dataContext = this.m_configuration.Provider.GetWriteConnection())
                {
                    dataContext.Open();

                    using (var tx = dataContext.BeginTransaction())
                        try
                        {
                            
                            var hashingService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();
                            var pdpService = ApplicationServiceContext.Current.GetService<IPolicyDecisionService>();

                            // Demand create identity
                            this.m_policyEnforcement.Demand(PermissionPolicyIdentifiers.CreateIdentity);

                            // Does this principal have the ability to 
                            DbSecurityUser newIdentityUser = new DbSecurityUser()
                            {
                                UserName = userName,
                                Password = hashingService.ComputeHash(password + AdoDataConstants.PEPPER_CHARS[DateTime.Now.Ticks % AdoDataConstants.PEPPER_CHARS.Length]),
                                SecurityHash = Guid.NewGuid().ToString(),
                                UserClass = UserClassKeys.HumanUser,
                                InvalidLoginAttempts = 0
                            };
                            newIdentityUser.CreatedByKey = dataContext.EstablishProvenance(principal, null);

                            dataContext.Insert(newIdentityUser);
                            var retVal = AdoClaimsIdentity.Create(dataContext, newIdentityUser);
                            tx.Commit();
                            return retVal;
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
                this.m_traceSource.TraceEvent(EventLevel.Error,  e.ToString());
                throw new DataPersistenceException($"Error creating identity {userName}", e);
            }

        }

        /// <summary>
        /// Delete the specified identity
        /// </summary>
        public void DeleteIdentity(string userName, IPrincipal principal)
        {
            if (String.IsNullOrEmpty(userName))
                throw new ArgumentNullException(nameof(userName));

            this.VerifyPrincipal(principal, PermissionPolicyIdentifiers.CreateIdentity);

            this.m_traceSource.TraceInfo("Delete identity {0}", userName);
            try
            {
                // submit the changes
                using (var dataContext = this.m_configuration.Provider.GetWriteConnection())
                {
                    dataContext.Open();
                    this.m_policyEnforcement.Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration);

                    var user = dataContext.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLower() == userName.ToLower());
                    if (user == null)
                        throw new KeyNotFoundException("Specified user does not exist!");

                    // Obsolete
                    user.ObsoletionTime = DateTimeOffset.Now;
                    user.ObsoletedByKey = dataContext.EstablishProvenance(principal, null);
                    user.SecurityHash = Guid.NewGuid().ToString();

                    dataContext.Update(user);
                }

            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error,  e.ToString());
                throw new DataPersistenceException($"Error deleting identity {userName}", e);
            }
        }

        /// <summary>
        /// Set the lockout status
        /// </summary>
        public void SetLockout(string userName, bool lockout, IPrincipal principal)
        {
            if (String.IsNullOrEmpty(userName))
                throw new ArgumentNullException(nameof(userName));

            this.VerifyPrincipal(principal, PermissionPolicyIdentifiers.AlterIdentity);

            this.m_traceSource.TraceInfo("Lockout identity {0} = {1}", userName, lockout);
            try
            {
                // submit the changes
                using (var dataContext = this.m_configuration.Provider.GetWriteConnection())
                {
                    dataContext.Open();
                    this.m_policyEnforcement.Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration);

                    var user = dataContext.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLower() == userName.ToLower());
                    if (user == null)
                        throw new KeyNotFoundException("Specified user does not exist!");

                    // Obsolete
	                if (lockout)
		                user.Lockout = DateTime.MaxValue.AddDays(-10);
	                else
		                user.Lockout = null;

                    user.LockoutSpecified = true;
                    user.ObsoletionTime = null;
                    user.ObsoletionTimeSpecified = true;
                    user.ObsoletedByKey = null;
                    user.ObsoletedByKeySpecified = true;

                    user.UpdatedByKey = dataContext.EstablishProvenance(principal, null);
                    user.UpdatedTime = DateTimeOffset.Now;
                    user.SecurityHash = Guid.NewGuid().ToString();

                    var updatedUser = dataContext.Update(user);

	                var securityUser = new SecurityUserPersistenceService(ApplicationServiceContext.Current.GetService<AdoPersistenceService>()).ToModelInstance(updatedUser, dataContext);
					ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Add(securityUser);
                }

            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error,  e.ToString());
                throw new DataPersistenceException($"Error setting lockout for {userName}");
            }
        }

        /// <summary>
        /// Add a claim to the specified user
        /// </summary>
        public void AddClaim(string userName, IClaim claim, IPrincipal principal, TimeSpan? expire = null)
        {
            if (userName == null)
                throw new ArgumentNullException(nameof(userName));
            else if (claim == null)
                throw new ArgumentNullException(nameof(claim));

            this.VerifyPrincipal(principal, PermissionPolicyIdentifiers.AlterIdentity);
           
            try
            {
                using (var dataContext = this.m_configuration.Provider.GetWriteConnection())
                {
                    dataContext.Open();

                    var user = dataContext.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLower() == userName.ToLower());
                    if (user == null)
                        throw new KeyNotFoundException(userName);

                    lock (this.m_syncLock)
                    {
                        var existingClaim = dataContext.FirstOrDefault<DbUserClaim>(o => o.ClaimType == claim.Type && o.SourceKey == user.Key);

                        var claimValue = claim.Value;
                        if (claim.Type == SanteDBClaimTypes.SanteDBOTAuthCode)
                            claimValue = ApplicationServiceContext.Current.GetService<IPasswordHashingService>().ComputeHash(claim.Value); // OT Password claim is secret

                        // Set the secret
                        if (existingClaim == null)
                        {
                            existingClaim = new DbUserClaim()
                            {
                                ClaimType = claim.Type,
                                ClaimValue = claimValue,
                                SourceKey = user.Key
                            };
                            if (expire.HasValue)
                                existingClaim.ClaimExpiry = DateTime.Now.Add(expire.Value);
                            dataContext.Insert(existingClaim);
                        }
                        else
                        {
                            existingClaim.ClaimValue = claimValue;
                            if (expire.HasValue)
                                existingClaim.ClaimExpiry = DateTime.Now.Add(expire.Value);
                            dataContext.Update(existingClaim);
                        }

                    }
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error,  e.ToString());
                throw new DataPersistenceException($"Error adding claim to {userName}", e);
            }
        }

        /// <summary>
        /// Remove the specified claim
        /// </summary>
        public void RemoveClaim(string userName, string claimType, IPrincipal principal)
        {
            if (userName == null)
                throw new ArgumentNullException(nameof(userName));
            else if (claimType == null)
                throw new ArgumentNullException(nameof(claimType));

            this.VerifyPrincipal(principal, PermissionPolicyIdentifiers.AlterIdentity);
            
            try
            {
                using (var dataContext = this.m_configuration.Provider.GetWriteConnection())
                {
                    dataContext.Open();

                    var user = dataContext.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLower() == userName.ToLower());
                    if (user == null)
                        throw new KeyNotFoundException(userName);

                    dataContext.Delete<DbUserClaim>(o => o.ClaimType == claimType && o.SourceKey == user.Key);
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error,  e.ToString());
                throw new DataPersistenceException($"Error removing claim for {userName}", e);
            }
        }

        /// <summary>
        /// Authenticate this user based on the session
        /// </summary>
        /// <param name="session">The session for which authentication is being saught</param>
        /// <returns>The authenticated principal</returns>
        public IPrincipal Authenticate(ISession session)
        {
            try
            {
                using (var context = this.m_configuration.Provider.GetReadonlyConnection())
                {
                    context.Open();
                    var sessionId = new Guid(session.Id.Take(16).ToArray());

                    var sql = context.CreateSqlStatement<DbSession>().SelectFrom(typeof(DbSession), typeof(DbSecurityApplication), typeof(DbSecurityUser), typeof(DbSecurityDevice))
                        .InnerJoin<DbSecurityApplication>(o => o.ApplicationKey, o => o.Key)
                        .Join<DbSession, DbSecurityUser>("LEFT", o => o.UserKey, o => o.Key)
                        .Join<DbSession, DbSecurityDevice>("LEFT", o=>o.DeviceKey, o=>o.Key)
                        .Where<DbSession>(s => s.Key == sessionId);

                    var auth = context.FirstOrDefault<CompositeResult<DbSession, DbSecurityApplication, DbSecurityUser, DbSecurityDevice>>(sql);

                    // Identities
                    List<IClaimsIdentity> identities = new List<IClaimsIdentity>(3);
                    if (auth.Object1.NotAfter < DateTime.Now)
                        throw new SecuritySessionException(SessionExceptionType.Expired, "Session expired", null);
                    else if(auth.Object1.NotBefore > DateTime.Now)
                        throw new SecuritySessionException(SessionExceptionType.NotYetValid, "Session not yet valid", null);

                    if (auth.Object2?.Key != null)
                        identities.Add(new Server.Core.Security.ApplicationIdentity(auth.Object2.Key, auth.Object2.PublicId, true));
                    if (auth.Object1.DeviceKey.HasValue)
                        identities.Add(new DeviceIdentity(auth.Object4.Key, auth.Object4.PublicId, true));
                    
                    var principal = auth.Object1.UserKey.GetValueOrDefault() == Guid.Empty ?
                        new SanteDBClaimsPrincipal(identities) : AdoClaimsIdentity.Create(context, auth.Object3, true, "SESSION").CreateClaimsPrincipal(identities);
                    
                    identities.First().AddClaim(new SanteDBClaim(SanteDBClaimTypes.AuthenticationInstant, session.NotBefore.ToString("o")));
                    identities.First().AddClaim(new SanteDBClaim(SanteDBClaimTypes.Expiration, session.NotAfter.ToString("o")));
                    identities.First().AddClaim(new SanteDBClaim(SanteDBClaimTypes.SanteDBSessionIdClaim, auth.Object1.Key.ToString()));

                    // Add claims from session
                    foreach (var clm in session.Claims)
                        identities.First().AddClaim(clm);
                    
                    // TODO: Load additional claims made about the user on the session
                    return principal;
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Verbose, "Invalid session auth: {0}", e.Message);
                this.m_traceSource.TraceEvent(EventLevel.Error,  e.ToString());
                this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(null, null, false));
                throw new SecuritySessionException(SessionExceptionType.Other, $"Could not perform session authentication", e);
            }
        }

        /// <summary>
        /// Verify principal
        /// </summary>
        private void VerifyPrincipal(IPrincipal principal, String policyId)
        {
            if (principal is null)
                throw new ArgumentNullException(nameof(principal));

            this.m_policyEnforcement.Demand(policyId, principal);
        }

        /// <summary>
        /// Re-Authenticates the principal (extending its time)
        /// </summary>
        public IPrincipal ReAuthenticate(IPrincipal principal)
        {
            if (!principal.Identity.IsAuthenticated)
                throw new InvalidOperationException("Cannot re-authenticate this principal");
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the specified identities from the session
        /// </summary>
        public IIdentity[] GetIdentities(ISession session)
        {
            try
            {

                using(var context = this.m_configuration.Provider.GetReadonlyConnection())
                {
                    context.Open();
                    var sessionId = new Guid(session.Id.Take(16).ToArray());
                    var sql = context.CreateSqlStatement<DbSession>().SelectFrom(typeof(DbSession), typeof(DbSecurityApplication), typeof(DbSecurityDevice), typeof(DbSecurityUser))
                            .Join<DbSession, DbSecurityDevice>("LEFT", o => o.DeviceKey, o => o.Key)
                            .Join<DbSession, DbSecurityApplication>("LEFT", o => o.ApplicationKey, o => o.Key)
                            .Join<DbSession, DbSecurityUser>("LEFT", o => o.UserKey, o => o.Key)
                            .Where<DbSession>(o => o.Key == sessionId);

                    var sessionData = context.FirstOrDefault<CompositeResult<DbSession, DbSecurityDevice, DbSecurityApplication, DbSecurityUser>>(sql);
                    if (sessionData == null)
                        throw new KeyNotFoundException($"Session {sessionId} not found");
                    else
                    {
                        List<IIdentity> retVal = new List<IIdentity>(4);
                        retVal.Add(new Server.Core.Security.ApplicationIdentity(sessionData.Object1.ApplicationKey, sessionData.Object3.PublicId, false));
                        if (sessionData.Object1.DeviceKey.HasValue)
                            retVal.Add(new DeviceIdentity(sessionData.Object2.Key, sessionData.Object2.PublicId, false));
                        if (sessionData.Object1.UserKey.HasValue)
                            retVal.Add(AdoClaimsIdentity.Create(context, sessionData.Object4, false));
                        return retVal.ToArray();
                    }

                }
            }
            catch(Exception e)
            {
                this.m_traceSource.TraceError("Error getting identities for session {0}", session.Id);
                throw new DataPersistenceException($"Error getting identities for session {BitConverter.ToString(session.Id)}");
            }
        }
    }
}
