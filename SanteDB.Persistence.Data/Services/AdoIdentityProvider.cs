/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-9-7
 */
using SanteDB.Core;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Configuration;
using SanteDB.Persistence.Data.Model.Security;
using SanteDB.Persistence.Data.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Authentication;
using System.Security.Principal;
using System.Text;
using SanteDB.Core.Model;
using SanteDB.Persistence.Data.Exceptions;

namespace SanteDB.Persistence.Data.Services
{
    /// <summary>
    /// An identity provider implemented for .NET
    /// </summary>
    public class AdoIdentityProvider : IIdentityProviderService
    {
        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(AdoIdentityProvider));

        // Session configuration
        private readonly AdoPersistenceConfigurationSection m_configuration;

        // Hashing service
        private readonly IPasswordHashingService m_passwordHashingService;

        // Security configuration
        private readonly SecurityConfigurationSection m_securityConfiguration;

        // PEP
        private readonly IPolicyEnforcementService m_pepService;

        // TFA generator
        private readonly ITfaRelayService m_tfaRelay;

        // The password validator
        private readonly IPasswordValidatorService m_passwordValidator;

        // Localization service
        private readonly ILocalizationService m_localizationService;

        /// <summary>
        /// Creates a new ADO session identity provider with injected configuration manager
        /// </summary>
        public AdoIdentityProvider(IConfigurationManager configuration,
            ILocalizationService localizationService,
            IPasswordHashingService passwordHashingService,
            IPolicyEnforcementService policyEnforcementService,
            IPasswordValidatorService passwordValidator,
            ITfaRelayService twoFactorSecretGenerator = null)
        {
            this.m_configuration = configuration.GetSection<AdoPersistenceConfigurationSection>();
            this.m_securityConfiguration = configuration.GetSection<SecurityConfigurationSection>();
            this.m_passwordHashingService = passwordHashingService;
            this.m_pepService = policyEnforcementService;
            this.m_tfaRelay = twoFactorSecretGenerator;
            this.m_passwordValidator = passwordValidator;
            this.m_localizationService = localizationService;
        }

        /// <summary>
        /// Gets the service name of the identity provider
        /// </summary>
        public string ServiceName => "Data-Based Identity Provider";

        /// <summary>
        /// Fired when the identity provider is authenticating a principal
        /// </summary>
        public event EventHandler<AuthenticatingEventArgs> Authenticating;

        /// <summary>
        /// Fired when an identity provider has authenticated the principal
        /// </summary>
        public event EventHandler<AuthenticatedEventArgs> Authenticated;

        /// <summary>
        /// Adds a claim to the specified user account
        /// </summary>
        /// <param name="userName">The user for which the claim is to be persisted</param>
        /// <param name="claim">The claim which is to be persisted</param>
        /// <param name="principal">The principal which is adding the claim (the authority under which the claim is being added)</param>
        /// <param name="expiry">The expiration time for the claim</param>
        /// <param name="protect">True if the claim should be protected in the database via a hash</param>
        public void AddClaim(string userName, IClaim claim, IPrincipal principal, TimeSpan? expiry = null)
        {
            if (String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            this.m_pepService.Demand(PermissionPolicyIdentifiers.AlterIdentity, principal);

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    var dbUser = context.Query<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userName.ToLowerInvariant() && o.ObsoletionTime == null).Select(o => o.Key).FirstOrDefault();
                    if (dbUser == Guid.Empty)
                    {
                        throw new KeyNotFoundException(String.Format(this.m_localizationService.GetString(ErrorMessageStrings.USR_INVALID, userName)));
                    }

                    var dbClaim = context.FirstOrDefault<DbUserClaim>(o => o.SourceKey == dbUser);

                    // Current claim in DB? Update
                    if (dbClaim == null)
                    {
                        dbClaim = new DbUserClaim()
                        {
                            SourceKey = dbUser
                        };
                    }
                    dbClaim.ClaimType = claim.Type;
                    dbClaim.ClaimValue = claim.Value;

                    if (expiry.HasValue)
                    {
                        dbClaim.ClaimExpiry = DateTimeOffset.Now.Add(expiry.Value).DateTime;
                    }

                    dbClaim = context.InsertOrUpdate(dbClaim);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error adding claim to {0} - {1}", userName, e);
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.USER_CLAIM_GEN_ERR), e);
                }
            }
        }

        /// <summary>
        /// Authenticate the specified user with the specified password
        /// </summary>
        /// <param name="userName">The name of the user which is to eb authenticated</param>
        /// <param name="password">The password for the user</param>
        /// <returns>The authenticated principal</returns>
        public IPrincipal Authenticate(string userName, string password)
        {
            if (String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (String.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            return this.AuthenticateInternal(userName, password, null);
        }

        /// <summary>
        /// Authenticate the user with the user name password and TFA secret
        /// </summary>
        /// <param name="userName">The user name of the user to be authenticated</param>
        /// <param name="password">The password of the user</param>
        /// <param name="tfaSecret">The TFA secret provided by the user</param>
        /// <returns>The authentcated principal</returns>
        public IPrincipal Authenticate(string userName, string password, string tfaSecret)
        {
            if (String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (String.IsNullOrEmpty(tfaSecret))
            {
                throw new ArgumentNullException(nameof(tfaSecret), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            return this.AuthenticateInternal(userName, password, tfaSecret);
        }

        /// <summary>
        /// Perform internal authentication routine
        /// </summary>
        /// <param name="userName">The user to authentcate</param>
        /// <param name="password">If provided, the password to authenticated</param>
        /// <param name="tfaSecret">If provided the TFA challenge response</param>
        /// <returns>The authenticated principal</returns>
        private IPrincipal AuthenticateInternal(String userName, String password, String tfaSecret)
        {
            // Allow cancellation
            var preEvtArgs = new AuthenticatingEventArgs(userName);
            this.Authenticating?.Invoke(this, preEvtArgs);
            if (preEvtArgs.Cancel)
            {
                this.m_tracer.TraceWarning("Pre-Authenticate trigger signals cancel");
                if (preEvtArgs.Success)
                {
                    return preEvtArgs.Principal;
                }
                else
                {
                    throw new AuthenticationException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_CANCELLED));
                }
            }

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    using (var tx = context.BeginTransaction())
                    {
                        var dbUser = context.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userName.ToLowerInvariant() && o.ObsoletionTime == null);

                        // Perform authentication
                        try
                        {
                            if (dbUser == null)
                            {
                                throw new InvalidIdentityAuthenticationException();
                            }
                            else if (dbUser.Lockout.GetValueOrDefault() > DateTimeOffset.Now)
                            {
                                throw new LockedIdentityAuthenticationException(dbUser.Lockout.Value);
                            }

                            // Claims to add to the principal
                            var claims = context.Query<DbUserClaim>(o => o.SourceKey == dbUser.Key && o.ClaimExpiry < DateTimeOffset.Now).ToList();
                            claims.RemoveAll(o => o.ClaimType == SanteDBClaimTypes.SanteDBOTAuthCode);

                            if (!String.IsNullOrEmpty(password))
                            {
                                if (dbUser.PasswordExpiration.HasValue && dbUser.PasswordExpiration.Value < DateTimeOffset.Now)
                                {
                                    claims.Add(new DbUserClaim() { ClaimType = SanteDBClaimTypes.PurposeOfUse, ClaimValue = PurposeOfUseKeys.SecurityAdmin.ToString() });
                                    claims.Add(new DbUserClaim() { ClaimType = SanteDBClaimTypes.SanteDBScopeClaim, ClaimValue = PermissionPolicyIdentifiers.LoginPasswordOnly });
                                    claims.Add(new DbUserClaim() { ClaimType = SanteDBClaimTypes.SanteDBScopeClaim, ClaimValue = PermissionPolicyIdentifiers.ReadMetadata });
                                }
                                else
                                {
                                    // Peppered authentication
                                    var pepperSecret = this.m_configuration.GetPepperCombos(password).Select(o => this.m_passwordHashingService.ComputeHash(o));
                                    // Pepper authentication
                                    if (!context.Any<DbSecurityUser>(a => a.UserName.ToLowerInvariant() == userName.ToLower() && pepperSecret.Contains(a.Password)))
                                    {
                                        throw new InvalidIdentityAuthenticationException();
                                    }
                                }
                            }
                            else if (String.IsNullOrEmpty(password) && !claims.Any(c => c.ClaimType == SanteDBClaimTypes.SanteDBCodeAuth && "true".Equals(c.ClaimValue, StringComparison.OrdinalIgnoreCase)))
                            {
                                throw new InvalidIdentityAuthenticationException();
                            }

                            // User requires TFA but the secret is empty
                            if (dbUser.TwoFactorEnabled && String.IsNullOrEmpty(tfaSecret) &&
                                dbUser.TwoFactorMechnaismKey.HasValue)
                            {
                                this.m_tfaRelay.SendSecret(dbUser.TwoFactorMechnaismKey.Value, new AdoUserIdentity(dbUser));
                                throw new AuthenticationException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_USR_TFA_REQ));
                            }

                            // TFA supplied?
                            if (dbUser.TwoFactorEnabled && !this.m_tfaRelay.ValidateSecret(dbUser.TwoFactorMechnaismKey.Value, new AdoUserIdentity(dbUser), tfaSecret))
                            {
                                throw new InvalidIdentityAuthenticationException();
                            }

                            // Reset invalid logins
                            dbUser.InvalidLoginAttempts = 0;
                            dbUser.LastLoginTime = DateTimeOffset.Now;
                            dbUser.Password = this.m_passwordHashingService.ComputeHash(this.m_configuration.AddPepper(password));

                            dbUser = context.Update(dbUser);

                            // Establish ID
                            var identity = new AdoUserIdentity(dbUser, "LOCAL");

                            // Establish role
                            var roleSql = context.CreateSqlStatement<DbSecurityRole>()
                                .SelectFrom()
                                .InnerJoin<DbSecurityUserRole>(o => o.Key, o => o.RoleKey)
                                .Where<DbSecurityUserRole>(o => o.UserKey == dbUser.Key);
                            identity.AddRoleClaims(context.Query<DbSecurityRole>(roleSql).Select(o => o.Name));

                            // Establish additional claims
                            identity.AddClaims(claims.Select(o => new SanteDBClaim(o.ClaimType, o.ClaimValue)));

                            // Create principal
                            var retVal = new AdoClaimsPrincipal(identity);
                            this.m_pepService.Demand(PermissionPolicyIdentifiers.Login, retVal);

                            // Fire authentication
                            this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(userName, retVal, true));
                            return retVal;
                        }
                        catch (LockedIdentityAuthenticationException e)
                        {
                            throw new AuthenticationException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_USR_LOCKED), e);
                        }
                        catch (InvalidIdentityAuthenticationException) when (dbUser != null)
                        {
                            dbUser.InvalidLoginAttempts = dbUser.InvalidLoginAttempts.GetValueOrDefault() + 1;
                            if (dbUser.InvalidLoginAttempts > this.m_securityConfiguration.GetSecurityPolicy<Int32>(SecurityPolicyIdentification.MaxInvalidLogins, 5))
                            {
                                var lockoutSlide = 30 * dbUser.InvalidLoginAttempts.Value;
                                if (DateTimeOffset.Now < DateTimeOffset.MaxValue.AddSeconds(-lockoutSlide))
                                {
                                    dbUser.Lockout = DateTimeOffset.Now.AddSeconds(lockoutSlide);
                                }
                            }
                            context.Update(dbUser);
                            throw new AuthenticationException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_USR_INVALID));
                        }
                        catch (InvalidIdentityAuthenticationException)
                        {
                            throw new AuthenticationException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_USR_INVALID));
                        }
                        finally
                        {
                            tx.Commit();
                        }
                    }
                }
                catch (AuthenticationException)
                {
                    this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(userName, null, false));
                    throw;
                }
                catch (Exception e)
                {
                    this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(userName, null, false));
                    this.m_tracer.TraceError("Could not authenticate user {0}- {1]", userName, e);
                    throw new AuthenticationException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_USR_GENERAL), e);
                }
            }
        }

        /// <summary>
        /// Change the specified user password
        /// </summary>
        /// <param name="userName">The user who's password is being changed</param>
        /// <param name="newPassword">The new password to set</param>
        /// <param name="principal">The principal which is setting the password</param>
        public void ChangePassword(string userName, string newPassword, IPrincipal principal)
        {
            if (String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (String.IsNullOrEmpty(newPassword))
            {
                throw new ArgumentNullException(nameof(userName), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (!this.m_passwordValidator.Validate(newPassword))
            {
                throw new SecurityException(this.m_localizationService.GetString(ErrorMessageStrings.USR_PWD_COMPLEXITY));
            }
            else if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            // The user changing the password must be the user or an administrator
            if (!principal.Identity.Name.Equals(userName, StringComparison.OrdinalIgnoreCase))
            {
                this.m_pepService.Demand(PermissionPolicyIdentifiers.ChangePassword, principal);
            }

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    using (var tx = context.BeginTransaction())
                    {
                        // Get the user
                        var dbUser = context.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userName.ToLowerInvariant() && o.ObsoletionTime == null);
                        if (dbUser == null)
                        {
                            throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.USR_INVALID));
                        }

                        // Password reuse policy?
                        if (this.m_securityConfiguration.GetSecurityPolicy<bool>(SecurityPolicyIdentification.PasswordHistory) && this.m_configuration.GetPepperCombos(newPassword).Any(o => this.m_passwordHashingService.ComputeHash(o) == dbUser.Password))
                        {
                            throw new SecurityException(this.m_localizationService.GetString(ErrorMessageStrings.USR_PWD_HISTORY));
                        }
                        dbUser.Password = this.m_passwordHashingService.ComputeHash(this.m_configuration.AddPepper(newPassword));
                        dbUser.UpdatedByKey = context.EstablishProvenance(principal, null);
                        dbUser.UpdatedTime = DateTimeOffset.Now;

                        // Password expire policy
                        var pwdExpire = this.m_securityConfiguration.GetSecurityPolicy<TimeSpan>(SecurityPolicyIdentification.MaxPasswordAge);
                        if (pwdExpire != default(TimeSpan))
                        {
                            dbUser.PasswordExpiration = DateTimeOffset.Now.Add(pwdExpire);
                        }

                        // Abandon all sessions for this user
                        foreach (var ses in context.Query<DbSession>(o => o.UserKey == dbUser.Key))
                        {
                            ses.NotAfter = DateTimeOffset.Now;
                            context.Update(ses);
                        }

                        // Save user
                        dbUser = context.Update(dbUser);

                        tx.Commit();
                    }
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error updating user password - {0}", e);
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.USR_PWD_GEN_ERR, new { userName = userName }), e);
                }
            }
        }

        /// <summary>
        /// Create a security identity for the specified
        /// </summary>
        /// <param name="userName">The name of the user identity which is to be created</param>
        /// <param name="password">The initial password to set for the principal</param>
        /// <param name="principal">The principal which is creating the identity</param>
        /// <returns>The created identity</returns>
        public IIdentity CreateIdentity(string userName, string password, IPrincipal principal)
        {
            if (String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (String.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(userName), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (!this.m_passwordValidator.Validate(password))
            {
                throw new SecurityException(this.m_localizationService.GetString(ErrorMessageStrings.USR_PWD_COMPLEXITY));
            }
            else if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            // Validate create permission
            if (ApplicationServiceContext.Current.HostType == SanteDBHostType.Server)
            {
                this.m_pepService.Demand(PermissionPolicyIdentifiers.CreateIdentity, principal);
            }
            else
            {
                this.m_pepService.Demand(PermissionPolicyIdentifiers.CreateLocalIdentity, principal);
            }

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    using (var tx = context.BeginTransaction())
                    {
                        // Construct the request
                        var newIdentity = new DbSecurityUser()
                        {
                            UserName = userName,
                            Password = this.m_passwordHashingService.ComputeHash(this.m_configuration.AddPepper(password)),
                            SecurityHash = this.m_passwordHashingService.ComputeHash(userName + password),
                            UserClass = ActorTypeKeys.HumanUser,
                            InvalidLoginAttempts = 0,
                            CreatedByKey = context.EstablishProvenance(principal, null),
                            CreationTime = DateTimeOffset.Now
                        };

                        var expirePwd = this.m_securityConfiguration.GetSecurityPolicy<TimeSpan>(SecurityPolicyIdentification.MaxPasswordAge);
                        if (expirePwd != default(TimeSpan))
                        {
                            newIdentity.PasswordExpiration = DateTimeOffset.Now.Add(expirePwd);
                        }

                        newIdentity = context.Insert(newIdentity);

                        // Register the group
                        context.InsertAll(context.Query<DbSecurityRole>(context.CreateSqlStatement<DbSecurityRole>()
                            .SelectFrom()
                            .Where<DbSecurityRole>(o => o.Name == "USERS"))
                            .ToArray()
                            .Select(o => new DbSecurityUserRole()
                            {
                                RoleKey = o.Key,
                                UserKey = newIdentity.Key
                            }));

                        tx.Commit();
                        return new AdoUserIdentity(newIdentity);
                    }
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error creating identity {0} - {1}", userName, e.Message);
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.USR_CREATE_GEN, new { userName = userName }), e);
                }
            }
        }

        /// <summary>
        /// Delete the specified identity
        /// </summary>
        public void DeleteIdentity(string userName, IPrincipal principal)
        {
            if (String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            if (ApplicationServiceContext.Current.HostType == SanteDBHostType.Server)
            {
                this.m_pepService.Demand(PermissionPolicyIdentifiers.AlterIdentity, principal);
            }
            else
            {
                this.m_pepService.Demand(PermissionPolicyIdentifiers.AlterLocalIdentity, principal);
            }

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    // Obsolete user
                    var dbUser = context.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userName.ToLowerInvariant() && o.ObsoletionTime == null);
                    if (dbUser == null)
                    {
                        throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND));
                    }

                    dbUser.ObsoletionTime = DateTimeOffset.Now;
                    dbUser.ObsoletedByKey = context.EstablishProvenance(principal, null);
                    context.Update(dbUser);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Could not delete identity {0} - {1}", userName, e.Message);
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.USR_DEL_ERR, new { userName = userName }), e);
                }
            }
        }

        /// <summary>
        /// Get an unauthenticated identity for the specified username
        /// </summary>
        /// <param name="userName">The user to fetch the identity for</param>
        /// <returns>The un-authenticated identity</returns>
        public IIdentity GetIdentity(string userName)
        {
            if (String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            using (var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();

                    var dbUser = context.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userName.ToLowerInvariant() && o.ObsoletionTime == null);
                    if (dbUser == null)
                    {
                        return null;
                    }

                    var dbClaims = context.Query<DbUserClaim>(o => o.SourceKey == dbUser.Key &&
                        (o.ClaimExpiry == null || o.ClaimExpiry > DateTimeOffset.Now));
                    var retVal = new AdoUserIdentity(dbUser);
                    retVal.AddClaims(dbClaims.ToArray().Select(o => new SanteDBClaim(o.ClaimType, o.ClaimValue)));

                    // Establish role
                    var roleSql = context.CreateSqlStatement<DbSecurityRole>()
                                .SelectFrom()
                                .InnerJoin<DbSecurityUserRole>(o => o.Key, o => o.RoleKey)
                                .Where<DbSecurityUserRole>(o => o.UserKey == dbUser.Key);
                    retVal.AddRoleClaims(context.Query<DbSecurityRole>(roleSql).Select(o => o.Name));

                    return retVal;
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error fetching user identity {0} - {1}", userName, e.Message);
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.USR_GEN_ERR, new { userName = userName }), e);
                }
            }
        }

        /// <summary>
        /// Get the user identity by security ID
        /// </summary>
        public IIdentity GetIdentity(Guid sid)
        {
            if (sid == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(sid), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            using (var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();

                    var dbUser = context.FirstOrDefault<DbSecurityUser>(o => o.Key == sid && o.ObsoletionTime == null);
                    if (dbUser == null)
                    {
                        return null;
                    }

                    var retVal = new AdoUserIdentity(dbUser);
                    var claims = context.Query<DbUserClaim>(o => o.SourceKey == dbUser.Key && o.ClaimExpiry < DateTimeOffset.Now).ToList();
                    retVal.AddClaims(claims.Select(o => new SanteDBClaim(o.ClaimType, o.ClaimValue)));
                    return retVal;
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error fetching user identity {0} - {1}", sid, e.Message);
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.USR_GEN_ERR, new { sid = sid }), e);
                }
            }
        }

        /// <summary>
        /// Gets the user sid by user name
        /// </summary>
        public Guid GetSid(string userName)
        {
            if (String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            using (var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();

                    var dbUser = context.Query<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userName.ToLowerInvariant() && o.ObsoletionTime == null)
                        .Select(o => o.Key).FirstOrDefault();
                    if (dbUser == null)
                    {
                        return Guid.Empty;
                    }

                    return dbUser;
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error fetching user identity {0} - {1}", userName, e.Message);
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.USR_GEN_ERR, new { userName = userName }), e);
                }
            }
        }

        /// <summary>
        /// Remove a claim from the specified user profile
        /// </summary>
        public void RemoveClaim(string userName, string claimType, IPrincipal principal)
        {
            if (String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (String.IsNullOrEmpty(claimType))
            {
                throw new ArgumentNullException(nameof(claimType), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            this.m_pepService.Demand(PermissionPolicyIdentifiers.AlterIdentity, principal);

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    var dbUser = context.Query<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userName.ToLowerInvariant() && o.ObsoletionTime == null).Select(o => o.Key).FirstOrDefault();
                    if (dbUser == Guid.Empty)
                    {
                        throw new KeyNotFoundException(String.Format(this.m_localizationService.GetString(ErrorMessageStrings.USR_INVALID, userName)));
                    }

                    context.DeleteAll<DbUserClaim>(o => o.SourceKey == dbUser && o.ClaimType.ToLowerInvariant() == claimType.ToLowerInvariant());
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error removing claim to {0} - {1}", userName, e);
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.USER_CLAIM_GEN_ERR), e);
                }
            }
        }

        /// <summary>
        /// Set the lockout status of the user
        /// </summary>
        /// <param name="userName">The user to set the lockout status for</param>
        /// <param name="lockout">The lockout status</param>
        /// <param name="principal">The principal which is performing the lockout</param>
        public void SetLockout(string userName, bool lockout, IPrincipal principal)
        {
            if (String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            if (ApplicationServiceContext.Current.HostType == SanteDBHostType.Server)
            {
                this.m_pepService.Demand(PermissionPolicyIdentifiers.AlterIdentity, principal);
            }
            else
            {
                this.m_pepService.Demand(PermissionPolicyIdentifiers.AlterLocalIdentity, principal);
            }

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    var dbUser = context.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userName.ToLowerInvariant() && o.ObsoletionTime == null);
                    if (dbUser == null)
                    {
                        throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND));
                    }

                    dbUser.UpdatedByKey = context.EstablishProvenance(principal, null);
                    dbUser.UpdatedTime = DateTimeOffset.Now;
                    dbUser.Lockout = lockout ? (DateTimeOffset?)DateTimeOffset.MaxValue.ToLocalTime() : null;
                    dbUser.LockoutSpecified = true;

                    context.Update(dbUser);
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }
    }
}