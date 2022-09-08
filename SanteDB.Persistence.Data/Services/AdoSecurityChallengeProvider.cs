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
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Configuration;
using SanteDB.Persistence.Data.Exceptions;
using SanteDB.Persistence.Data.Model.Security;
using SanteDB.Persistence.Data.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Authentication;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Persistence.Data.Services
{
    /// <summary>
    /// Represents a callenge service which uses the ADO.NET tables
    /// </summary>
    public class AdoSecurityChallengeProvider : ISecurityChallengeService, ISecurityChallengeIdentityService
    {

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(AdoSecurityChallengeProvider));

        // Configuration section
        private AdoPersistenceConfigurationSection m_configuration;

        // Security Configuration section
        private SecurityConfigurationSection m_securityConfiguration;

        // Localization service
        private readonly ILocalizationService m_localizationService;
        private readonly IPasswordHashingService m_passwordHashingService;
        private readonly ITfaRelayService m_tfaRelay;

        // Policy enforcement
        private IPolicyEnforcementService m_policyEnforcementService;

        // The randomizer
        private Random m_random = new Random();

        /// <summary>
        /// DI constructor for ADO CHallenge
        /// </summary>
        public AdoSecurityChallengeProvider(IConfigurationManager configurationManager, IPolicyEnforcementService pepService, ILocalizationService localizationService,
            IPasswordHashingService passwordHashingService, ITfaRelayService tfaRelayService)
        {
            this.m_policyEnforcementService = pepService;
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
            this.m_securityConfiguration = configurationManager.GetSection<SecurityConfigurationSection>();
            this.m_localizationService = localizationService;
            this.m_passwordHashingService = passwordHashingService;
            this.m_tfaRelay = tfaRelayService;
        }

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "ADO.NET Challenge Service";

        /// <summary>
        /// Service is authenticating
        /// </summary>
        public event EventHandler<AuthenticatingEventArgs> Authenticating;

        /// <summary>
        /// Authentication has succeeded
        /// </summary>
        public event EventHandler<AuthenticatedEventArgs> Authenticated;

        /// <summary>
        /// Authenticate a user using their challenge response
        /// </summary>
        /// <param name="challengeKey">The challenge which was answered</param>
        /// <param name="response">The response to the challenge</param>
        /// <param name="tfaSecret">The TFA one time password to fulfill the authentication request</param>
        /// <param name="userName">The name of the user which is being authenticated</param>
        public IPrincipal Authenticate(string userName, Guid challengeKey, string response, String tfaSecret)
        {

            if (String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), ErrorMessages.ARGUMENT_NULL);
            }
            else if (challengeKey == default(Guid))
            {
                throw new ArgumentOutOfRangeException(nameof(challengeKey), ErrorMessages.ARGUMENT_NULL);
            }
            else if (String.IsNullOrEmpty(response))
            {
                throw new ArgumentNullException(nameof(response), ErrorMessages.ARGUMENT_NULL);
            }
            

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

            var pepperResponses = this.m_configuration.GetPepperCombos(response).Select(o => this.m_passwordHashingService.ComputeHash(o));

            try
            {
                // Connection to perform auth
                using (var context = this.m_configuration.Provider.GetWriteConnection())
                {
                    DbSecurityUser dbUser = null;
                    try
                    {
                        context.Open();
                        dbUser = context.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userName.ToLowerInvariant() && o.ObsoletionTime == null);

                        // User found?
                        if (dbUser == null)
                        {
                            throw new InvalidIdentityAuthenticationException();
                        }
                        else if (dbUser.Lockout.GetValueOrDefault() > DateTimeOffset.Now)
                        {
                            throw new LockedIdentityAuthenticationException(dbUser.Lockout.Value);
                        }

                        // TFA required? Or not supplied?
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

                        if (!context.Any<DbSecurityUserChallengeAssoc>(c => pepperResponses.Contains(c.ChallengeResponse) && c.ChallengeKey == challengeKey && c.ExpiryTime > DateTimeOffset.Now)) // Increment invalid
                        {
                            dbUser.InvalidLoginAttempts++;
                            if (dbUser.InvalidLoginAttempts > this.m_securityConfiguration.GetSecurityPolicy<Int32>(SecurityPolicyIdentification.MaxInvalidLogins, 5))
                            {
                                dbUser.Lockout = DateTimeOffset.Now.Add(new TimeSpan(0, 0, dbUser.InvalidLoginAttempts.Value * 30));
                            }
                            dbUser.UpdatedByKey = Guid.Parse(AuthenticationContext.SystemUserSid);
                            dbUser.UpdatedTime = DateTimeOffset.Now;
                            context.Update(dbUser);
                            if (dbUser.Lockout > DateTimeOffset.Now)
                            {
                                throw new LockedIdentityAuthenticationException(dbUser.Lockout.Value);
                            }
                            else
                            {
                                throw new InvalidIdentityAuthenticationException();
                            }
                        }
                        else
                        {
                            var identity = new AdoUserIdentity(dbUser, "LOCAL_CHALLENGE");
                            this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.Login, new AdoClaimsPrincipal(identity)); // must still be allowed to login

                            // Establish role
                            var roleSql = context.CreateSqlStatement<DbSecurityRole>()
                                .SelectFrom()
                                .InnerJoin<DbSecurityUserRole>(o => o.Key, o => o.RoleKey)
                                .Where<DbSecurityUserRole>(o => o.UserKey == dbUser.Key);
                            identity.AddRoleClaims(context.Query<DbSecurityRole>(roleSql).Select(o => o.Name));

                            // Establish additional claims
                            identity.AddClaim(new SanteDBClaim(SanteDBClaimTypes.PurposeOfUse, PurposeOfUseKeys.SecurityAdmin.ToString()));
                            identity.AddClaim(new SanteDBClaim(SanteDBClaimTypes.SanteDBScopeClaim, PermissionPolicyIdentifiers.ReadMetadata));
                            identity.AddClaim(new SanteDBClaim(SanteDBClaimTypes.SanteDBScopeClaim, PermissionPolicyIdentifiers.LoginPasswordOnly));

                            var retVal = new AdoClaimsPrincipal(identity);
                            this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(userName, retVal, true));
                            return retVal;
                        }
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

        /// <summary>
        /// Get the security challenges for the specified user key
        /// </summary>
        /// <param name="principal">The principal that is requesting the security challenge information</param>
        /// <param name="userName">The user name which is requesting the challenge information</param>
        public IEnumerable<SecurityChallenge> Get(String userName, IPrincipal principal)
        {

            if(String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), ErrorMessages.ARGUMENT_NULL);
            }
            else if(principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ARGUMENT_NULL);
            }

            try
            {
                using (var context = this.m_configuration.Provider.GetWriteConnection())
                {

                    context.Open();
                    var sqlQuery = context.CreateSqlStatement<DbSecurityChallenge>().SelectFrom(typeof(DbSecurityChallenge), typeof(DbSecurityUserChallengeAssoc))
                            .InnerJoin<DbSecurityUserChallengeAssoc>(o => o.Key, o => o.ChallengeKey)
                            .InnerJoin<DbSecurityUserChallengeAssoc, DbSecurityUser>(o => o.UserKey, o => o.Key)
                            .Where<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userName.ToLowerInvariant());

                    var retVal = context.Query<CompositeResult<DbSecurityChallenge, DbSecurityUserChallengeAssoc>>(sqlQuery).ToArray().Select(o => new SecurityChallenge()
                    {
                        ChallengeText = o.Object1.ChallengeText,
                        Key = o.Object1.Key,
                        ObsoletionTime = o.Object2.ExpiryTime
                    }).ToList();

                    // Only the current user can fetch their own security challenge questions 
                    if (!userName.Equals(principal.Identity.Name, StringComparison.OrdinalIgnoreCase)
                        || !principal.Identity.IsAuthenticated)
                    {
                        return retVal.Skip(this.m_random.Next(0, retVal.Count)).Take(1);
                    }
                    else // Only a random option can be returned  
                    {
                        return retVal;
                    }

                }
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Failed to fetch security challenges for user {0}: {1}", userName, e);
                throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.USR_CHL_GEN_ERR, new { userName = userName }), e);
            }
        }

        /// <summary>
        /// Get the specified challenges
        /// </summary>
        public IEnumerable<SecurityChallenge> Get(Guid userKey, IPrincipal principal)
        {
            if(userKey == default(Guid))
            {
                throw new ArgumentOutOfRangeException(nameof(userKey), ErrorMessages.ARGUMENT_OUT_OF_RANGE);
            }
            else if(principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ARGUMENT_NULL);
            }

            try
            {

                using (var context = this.m_configuration.Provider.GetWriteConnection())
                {

                    context.Open();
                    var sqlQuery = context.CreateSqlStatement<DbSecurityChallenge>().SelectFrom(typeof(DbSecurityChallenge), typeof(DbSecurityUserChallengeAssoc))
                            .InnerJoin<DbSecurityUserChallengeAssoc>(o => o.Key, o => o.ChallengeKey)
                            .Where<DbSecurityUserChallengeAssoc>(o => o.UserKey == userKey);

                    var retVal = context.Query<CompositeResult<DbSecurityChallenge, DbSecurityUserChallengeAssoc>>(sqlQuery).ToArray().Select(o => new SecurityChallenge()
                    {
                        ChallengeText = o.Object1.ChallengeText,
                        Key = o.Object1.Key,
                        ObsoletionTime = o.Object2.ExpiryTime
                    }).ToList();

                    // Only the current user can fetch their own security challenge questions 
                    if (!context.Any<DbSecurityUser>(o => o.Key == userKey && principal.Identity.Name.ToLowerInvariant() == o.UserName.ToLowerInvariant())
                        || !principal.Identity.IsAuthenticated)
                    {
                        return retVal.Skip(this.m_random.Next(0, retVal.Count)).Take(1);
                    }
                    else // Only a random option can be returned  
                    {
                        return retVal;
                    }

                }
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Failed to fetch security challenges for user {0}: {1}", userKey, e);
                throw new Exception($"Failed to fetch security challenges for {userKey}", e);
            }
        }

        /// <summary>
        /// Remove the specified challenge for this particular key
        /// </summary>
        public void Remove(String userName, Guid challengeKey, IPrincipal principal)
        {
            if(String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), ErrorMessages.ARGUMENT_NULL);
            }
            else if(challengeKey == Guid.Empty)
            {
                throw new ArgumentOutOfRangeException(nameof(challengeKey), ErrorMessages.ARGUMENT_OUT_OF_RANGE);
            }
            else if (principal == null )
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ARGUMENT_NULL);
            }

            if (!userName.Equals(principal.Identity.Name, StringComparison.OrdinalIgnoreCase)
                || !principal.Identity.IsAuthenticated)
            {
                this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.AlterSecurityChallenge, principal);
            }

            try
            {
                using (var context = this.m_configuration.Provider.GetWriteConnection())
                {
                    context.Open();
                    var dbUser = context.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userName.ToLowerInvariant());
                    if (dbUser == null)
                    {
                        throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.USR_CHL_GEN_ERR, new { userName = userName }));
                    }
                    context.DeleteAll<DbSecurityUserChallengeAssoc>(o => o.ChallengeKey == challengeKey && o.UserKey == dbUser.Key);
                }
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Failed to removing security challenge {0} for user {1}: {2}", challengeKey, userName, e);
                throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.USR_CHL_GEN_ERR, new { userName = userName }), e);
            }
        }

        /// <summary>
        /// Set the specified challenge response
        /// </summary>
        public void Set(String userName, Guid challengeKey, string response, IPrincipal principal)
        {

            if (String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), ErrorMessages.ARGUMENT_NULL);
            }
            else if (challengeKey == Guid.Empty)
            {
                throw new ArgumentOutOfRangeException(nameof(challengeKey), ErrorMessages.ARGUMENT_NULL);
            }
            else if(String.IsNullOrEmpty(response))
            {
                throw new ArgumentNullException(nameof(response), ErrorMessages.ARGUMENT_NULL);
            }
            else if(principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ARGUMENT_NULL);
            }
            else if (!userName.Equals(principal.Identity.Name, StringComparison.OrdinalIgnoreCase)
                || !principal.Identity.IsAuthenticated)
            {
                this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.AlterSecurityChallenge, principal);
            }

            // Ensure that the user has been explicitly granted the special security policy
            try
            {
                using (var context = this.m_configuration.Provider.GetWriteConnection())
                {
                    context.Open();
                    var dbUser = context.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userName.ToLowerInvariant());
                    if (dbUser == null)
                    {
                        throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.USR_CHL_GEN_ERR, new { userName = userName }));
                    }

                    var pepperResponses = this.m_configuration.GetPepperCombos(response);
                    // Existing?
                    var challengeRec = context.FirstOrDefault<DbSecurityUserChallengeAssoc>(o => o.UserKey == dbUser.Key && o.ChallengeKey == challengeKey);
                    if (challengeRec == null)
                    {
                        context.Insert(new DbSecurityUserChallengeAssoc()
                        {
                            ChallengeKey = challengeKey,
                            UserKey = dbUser.Key,
                            ChallengeResponse = this.m_passwordHashingService.ComputeHash(this.m_configuration.AddPepper(response)),
                            ExpiryTime = DateTimeOffset.Now.Add(this.m_securityConfiguration.GetSecurityPolicy<TimeSpan>(SecurityPolicyIdentification.MaxChallengeAge, new TimeSpan(3650, 0, 0, 0)))
                        });
                    }
                    else if (!this.m_securityConfiguration.GetSecurityPolicy<Boolean>(SecurityPolicyIdentification.ChallengeHistory, false) ||
                        !context.Any<DbSecurityUserChallengeAssoc>(o => o.ChallengeKey == challengeRec.ChallengeKey && o.UserKey == challengeRec.UserKey && pepperResponses.Contains(o.ChallengeResponse)))
                    {
                        challengeRec.ExpiryTime = DateTimeOffset.Now.Add(this.m_securityConfiguration.GetSecurityPolicy<TimeSpan>(SecurityPolicyIdentification.MaxChallengeAge, new TimeSpan(3650, 0, 0, 0)));
                        challengeRec.ChallengeResponse = this.m_passwordHashingService.ComputeHash(this.m_configuration.AddPepper(response));
                        context.Update(challengeRec);
                    }
                    else
                    {
                        throw new InvalidOperationException(this.m_localizationService.GetString(ErrorMessageStrings.USR_CHL_DUP_RSP));
                    }
                }
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Failed to removing security challenge {0} for user {1}: {2}", challengeKey, userName, e);
                throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.USR_CHL_GEN_ERR, new { userName = userName }), e);
            }
        }
    }
}
