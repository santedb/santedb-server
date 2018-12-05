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
using SanteDB.Core;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
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
using System.Linq;
using System.Security;

using System.Security.Principal;

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
        private TraceSource m_traceSource = new TraceSource(AdoDataConstants.IdentityTraceSourceName);

        // Configuration
        private AdoPersistenceConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AdoPersistenceConfigurationSection>();

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
                this.m_traceSource.TraceEvent(TraceEventType.Verbose, e.HResult, "Invalid credentials : {0}/{1}", userName, password);

                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(userName, null, false));
                throw;
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

                            var claims = dataContext.Query<DbUserClaim>(o => o.SourceKey == user.Key && (o.ClaimType == SanteDBClaimTypes.SanteDBTfaSecretClaim || o.ClaimType == SanteDBClaimTypes.SanteDBTfaSecretExpiry || o.ClaimType == SanteDBClaimTypes.SanteDBPasswordlessAuth));
                            DbUserClaim tfaClaim = claims.FirstOrDefault(o => o.ClaimType == SanteDBClaimTypes.SanteDBTfaSecretClaim),
                                tfaExpiry = claims.FirstOrDefault(o => o.ClaimType == SanteDBClaimTypes.SanteDBTfaSecretExpiry),
                                noPassword = claims.FirstOrDefault(o => o.ClaimType == SanteDBClaimTypes.SanteDBPasswordlessAuth);

                            if (tfaClaim == null || tfaExpiry == null)
                                throw new InvalidOperationException("Cannot find appropriate claims for TFA");

                            // Expiry check
                            IClaimsPrincipal retVal = null;
                            DateTime expiryDate = DateTime.Parse(tfaExpiry.ClaimValue);
                            if (expiryDate < DateTime.Now)
                                throw new SecurityException("TFA secret expired");
                            else if (String.IsNullOrEmpty(password) &&
                                Boolean.Parse(noPassword?.ClaimValue ?? "false") &&
                                tfaSecret == tfaClaim.ClaimValue) // Last known password hash sent as password, this is a password reset token - It will be set to expire ASAP
                            {
                                retVal = AdoClaimsIdentity.Create(user, true, "Tfa+LastPasswordHash").CreateClaimsPrincipal();
                                (retVal.Identity as IClaimsIdentity).AddClaim(new SanteDBClaim(SanteDBClaimTypes.SanteDBGrantedPolicyClaim, PermissionPolicyIdentifiers.ChangePassword));
                                (retVal.Identity as IClaimsIdentity).RemoveClaim(retVal.FindFirst(SanteDBClaimTypes.Expiration));
                                // TODO: Add to configuration
                                (retVal.Identity as IClaimsIdentity).AddClaim(new SanteDBClaim(SanteDBClaimTypes.Expiration, DateTime.Now.AddMinutes(5).ToString("o")));
                            }
                            else if (!String.IsNullOrEmpty(password))
                                retVal = this.Authenticate(userName, password) as IClaimsPrincipal;
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
                this.m_traceSource.TraceEvent(TraceEventType.Verbose, e.HResult, "Invalid credentials : {0}/{1}", userName, password);
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(userName, null, false));
                throw;
            }
        }

        /// <summary>
        /// Change the user's password
        /// </summary>
        public void ChangePassword(string userName, string newPassword, IPrincipal principal)
        {
            if (!AuthenticationContext.Current.Principal.Identity.IsAuthenticated)
                throw new SecurityException("Principal must be authenticated");
            // Password failed validation
            if (ApplicationServiceContext.Current.GetService<IPasswordValidatorService>()?.Validate(newPassword) == false)
                throw new DetectedIssueException(new DetectedIssue(DetectedIssuePriorityType.Error, "err.password", DetectedIssueKeys.SecurityIssue));

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

                            var pdpOutcome = policyDecisionService?.GetPolicyOutcome(AuthenticationContext.Current.Principal, PermissionPolicyIdentifiers.ChangePassword);
                            if (userName != AuthenticationContext.Current.Principal.Identity.Name &&
                                pdpOutcome.HasValue &&
                                pdpOutcome != PolicyGrantType.Grant)
                                throw new PolicyViolationException(AuthenticationContext.Current.Principal, PermissionPolicyIdentifiers.ChangePassword, pdpOutcome.Value);

                            user.Password = passwordHashingService.ComputeHash(newPassword);
                            user.SecurityHash = Guid.NewGuid().ToString();
                            user.UpdatedByKey = dataContext.EstablishProvenance(principal, null); 

                            dataContext.Update(user);
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
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Generate and store the TFA secret
        /// </summary>
        public string GenerateTfaSecret(string userName)
        {
            // This is a simple TFA generator
            var secret = ApplicationServiceContext.Current.GetService<ITwoFactorSecretGenerator>().GenerateTfaSecret();
            var hashingService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();

            this.AddClaim(userName, new SanteDBClaim(SanteDBClaimTypes.SanteDBTfaSecretClaim, hashingService.ComputeHash(secret)), AuthenticationContext.SystemPrincipal);
            this.AddClaim(userName, new SanteDBClaim(SanteDBClaimTypes.SanteDBTfaSecretExpiry, DateTime.Now.AddMinutes(5).ToString("o")), AuthenticationContext.SystemPrincipal);

            return secret;
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

            this.m_traceSource.TraceInformation("Creating identity {0} ({1})", userName, AuthenticationContext.Current.Principal);

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
                            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.CreateIdentity).Demand();

                            // Does this principal have the ability to 
                            DbSecurityUser newIdentityUser = new DbSecurityUser()
                            {
                                UserName = userName,
                                Password = hashingService.ComputeHash(password),
                                SecurityHash = Guid.NewGuid().ToString(),
                                UserClass = UserClassKeys.HumanUser
                            };
                            newIdentityUser.CreatedByKey = dataContext.EstablishProvenance(principal, null);

                            dataContext.Insert(newIdentityUser);
                            var retVal = AdoClaimsIdentity.Create(newIdentityUser);
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
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                throw;
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

            this.m_traceSource.TraceInformation("Delete identity {0}", userName);
            try
            {
                // submit the changes
                using (var dataContext = this.m_configuration.Provider.GetWriteConnection())
                {
                    dataContext.Open();
                    new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.UnrestrictedAdministration).Demand();

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
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                throw;
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

            this.m_traceSource.TraceInformation("Lockout identity {0} = {1}", userName, lockout);
            try
            {
                // submit the changes
                using (var dataContext = this.m_configuration.Provider.GetWriteConnection())
                {
                    dataContext.Open();
                    new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.UnrestrictedAdministration).Demand();

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

	                var securityUser = new SecurityUserPersistenceService().ToModelInstance(updatedUser, dataContext);
					ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Add(securityUser);
                }

            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Add a claim to the specified user
        /// </summary>
        public void AddClaim(string userName, IClaim claim, IPrincipal principal)
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

                        // Set the secret
                        if (existingClaim == null)
                        {
                            existingClaim = new DbUserClaim()
                            {
                                ClaimType = claim.Type,
                                ClaimValue = claim.Value,
                                SourceKey = user.Key
                            };
                            dataContext.Insert(existingClaim);
                        }
                        else
                        {
                            existingClaim.ClaimValue = claim.Value;
                            dataContext.Update(existingClaim);
                        }

                    }
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                throw;
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
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                throw;
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
                        .Where<DbSession>(s => s.Key == sessionId && s.NotAfter > DateTimeOffset.Now);

                    var auth = context.FirstOrDefault<CompositeResult<DbSession, DbSecurityApplication, DbSecurityUser, DbSecurityDevice>>(sql);

                    // Identities
                    List<IClaimsIdentity> identities = new List<IClaimsIdentity>(3);
                    if (auth.Object2?.Key != null)
                        identities.Add(new Core.Security.ApplicationIdentity(auth.Object2.Key, auth.Object2.PublicId, true));
                    if (auth.Object1.DeviceKey.HasValue)
                        identities.Add(new DeviceIdentity(auth.Object4.Key, auth.Object4.PublicId, true));
                    identities.First().AddClaim(new SanteDBClaim(SanteDBClaimTypes.AuthenticationInstant, session.NotBefore.ToString("o")));
                    identities.First().AddClaim(new SanteDBClaim(SanteDBClaimTypes.Expiration, session.NotAfter.ToString("o")));
                    var principal = auth.Object1.UserKey.GetValueOrDefault() == Guid.Empty ?
                        new SanteDBClaimsPrincipal(identities) : AdoClaimsIdentity.Create(auth.Object3, true, "SESSION", session).CreateClaimsPrincipal(identities);
                    
                    // TODO: Load additional claims made about the user on the session
                    return principal;
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Verbose, e.HResult, "Invalid session auth: {0}", e.Message);
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(null, null, false));
                throw;
            }
        }

        /// <summary>
        /// Verify principal
        /// </summary>
        private void VerifyPrincipal(IPrincipal principal, String policyId)
        {
            if (principal is null)
                throw new ArgumentNullException(nameof(principal));

            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, policyId, principal).Demand();
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
    }
}
