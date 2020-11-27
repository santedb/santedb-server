/*
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
 * Date: 2020-3-18
 */
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Configuration;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using SanteDB.Persistence.Data.ADO.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Authentication;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Represents a callenge service which uses the ADO.NET tables
    /// </summary>
    public class AdoSecurityChallengeProvider : ISecurityChallengeService, ISecurityChallengeIdentityService
    {

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(AdoSecurityChallengeProvider));

        // Configuration section
        private AdoPersistenceConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AdoPersistenceConfigurationSection>();

        // Security Configuration section
        private SecurityConfigurationSection m_securityConfiguration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SecurityConfigurationSection>();

        // The randomizer
        private Random m_random = new Random();

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
        public IPrincipal Authenticate(string userName, Guid challengeKey, string response, String tfa)
        {
            try
            {
                var authArgs = new AuthenticatingEventArgs(userName);
                this.Authenticating?.Invoke(this, authArgs);
                if (authArgs.Cancel)
                    throw new SecurityException("Authentication cancelled");

                var hashService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();
                var responseHash = hashService.ComputeHash(response);

                // Connection to perform auth
                using (var context = this.m_configuration.Provider.GetWriteConnection())
                {
                    context.Open();
                    var query = context.CreateSqlStatement<DbSecurityUser>().SelectFrom(typeof(DbSecurityUser), typeof(DbSecurityUserChallengeAssoc))
                        .InnerJoin<DbSecurityUserChallengeAssoc>(o => o.Key, o => o.UserKey)
                        .Where(o => o.UserName.ToLower() == userName.ToLower() && o.ObsoletionTime == null)
                        .And<DbSecurityUserChallengeAssoc>(o => o.ExpiryTime > DateTime.Now);
                    var dbUser = context.FirstOrDefault<CompositeResult<DbSecurityUser, DbSecurityUserChallengeAssoc>>(query);

                    // User found?
                    if (dbUser == null)
                        throw new SecurityException("AUTH_INV");

                    // TFA? 
                    if (!String.IsNullOrEmpty(tfa))
                    {
                        var tfaSecret = ApplicationServiceContext.Current.GetService<IPasswordHashingService>().ComputeHash(tfa);

                        var claims = context.Query<DbUserClaim>(o => o.SourceKey == dbUser.Object1.Key && (!o.ClaimExpiry.HasValue || o.ClaimExpiry > DateTime.Now)).ToList();
                        DbUserClaim tfaClaim = claims.FirstOrDefault(o => o.ClaimType == SanteDBClaimTypes.SanteDBOTAuthCode);
                        if (tfaClaim == null || !tfaSecret.Equals(tfaClaim.ClaimValue, StringComparison.OrdinalIgnoreCase))
                            throw new SecurityException("TFA_MISMATCH");

                    }

                    if (dbUser.Object1.Lockout > DateTime.Now)
                        throw new SecurityException("AUTH_LCK");
                    else if (dbUser.Object2.ChallengeResponse != responseHash || dbUser.Object1.Lockout.GetValueOrDefault() > DateTime.Now) // Increment invalid
                    {
                        dbUser.Object1.InvalidLoginAttempts++;
                        if (dbUser.Object1.InvalidLoginAttempts > this.m_securityConfiguration.GetSecurityPolicy<Int32>(SecurityPolicyIdentification.MaxInvalidLogins, 5))
                            dbUser.Object1.Lockout = DateTime.Now.Add(new TimeSpan(0, 0, dbUser.Object1.InvalidLoginAttempts.Value * 30));
                        dbUser.Object1.UpdatedByKey = Guid.Parse(AuthenticationContext.SystemUserSid);
                        dbUser.Object1.UpdatedTime = DateTimeOffset.Now;

                        context.Update(dbUser.Object1);
                        if (dbUser.Object1.Lockout > DateTime.Now)
                            throw new AuthenticationException("AUTH_LCK");
                        else
                            throw new AuthenticationException("AUTH_INV");
                    }
                    else
                    {
                        var principal = AdoClaimsIdentity.Create(context, dbUser.Object1, true, "Secret=" + challengeKey.ToString()).CreateClaimsPrincipal();

                        new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.Login, principal).Demand(); // must still be allowed to login

                        (principal.Identity as IClaimsIdentity).AddClaim(new SanteDBClaim(SanteDBClaimTypes.PurposeOfUse, PurposeOfUseKeys.SecurityAdmin.ToString()));
                        (principal.Identity as IClaimsIdentity).AddClaim(new SanteDBClaim(SanteDBClaimTypes.SanteDBScopeClaim, PermissionPolicyIdentifiers.ReadMetadata));
                        (principal.Identity as IClaimsIdentity).AddClaim(new SanteDBClaim(SanteDBClaimTypes.SanteDBScopeClaim, PermissionPolicyIdentifiers.LoginPasswordOnly));

                        this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(userName, principal, true));
                        return principal;
                    }
                }
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Challenge authentication failed: {0}", e);
                this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(userName, null, false));
                throw new AuthenticationException($"Challenge authentication failed");
            }
        }

        /// <summary>
        /// Get the security challenges for the specified user key
        /// </summary>
        public IEnumerable<SecurityChallenge> Get(String userName, IPrincipal principal)
        {
            try
            {

                using (var context = this.m_configuration.Provider.GetWriteConnection())
                {

                    context.Open();

                    userName = userName.ToLower();
                    var sqlQuery = context.CreateSqlStatement<DbSecurityChallenge>().SelectFrom(typeof(DbSecurityChallenge), typeof(DbSecurityUserChallengeAssoc))
                            .InnerJoin<DbSecurityUserChallengeAssoc>(o => o.Key, o => o.ChallengeKey)
                            .InnerJoin<DbSecurityUserChallengeAssoc, DbSecurityUser>(o => o.UserKey, o => o.Key)
                            .Where<DbSecurityUser>(o => o.UserName.ToLower() == userName);

                    var retVal = context.Query<CompositeResult<DbSecurityChallenge, DbSecurityUserChallengeAssoc>>(sqlQuery).Select(o => new SecurityChallenge()
                    {
                        ChallengeText = o.Object1.ChallengeText,
                        Key = o.Object1.Key,
                        ObsoletionTime = o.Object2.ExpiryTime
                    }).ToList();

                    // Only the current user can fetch their own security challenge questions 
                    if (!userName.Equals(principal.Identity.Name, StringComparison.OrdinalIgnoreCase)
                        || !principal.Identity.IsAuthenticated)
                        return retVal.Skip(this.m_random.Next(0, retVal.Count)).Take(1);
                    else // Only a random option can be returned  
                        return retVal;

                }
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Failed to fetch security challenges for user {0}: {1}", userName, e);
                throw new Exception($"Failed to fetch security challenges for {userName}", e);
            }
        }

        /// <summary>
        /// Get the specified challenges
        /// </summary>
        public IEnumerable<SecurityChallenge> Get(Guid userKey, IPrincipal principal)
        {
            try
            {

                using (var context = this.m_configuration.Provider.GetWriteConnection())
                {

                    context.Open();

                    var sqlQuery = context.CreateSqlStatement<DbSecurityChallenge>().SelectFrom(typeof(DbSecurityChallenge), typeof(DbSecurityUserChallengeAssoc))
                            .InnerJoin<DbSecurityUserChallengeAssoc>(o => o.Key, o => o.ChallengeKey)
                            .Where<DbSecurityUserChallengeAssoc>(o => o.UserKey == userKey);

                    var retVal = context.Query<CompositeResult<DbSecurityChallenge, DbSecurityUserChallengeAssoc>>(sqlQuery).Select(o => new SecurityChallenge()
                    {
                        ChallengeText = o.Object1.ChallengeText,
                        Key = o.Object1.Key,
                        ObsoletionTime = o.Object2.ExpiryTime
                    }).ToList();

                    var userInfo = context.FirstOrDefault<DbSecurityUser>(o => o.Key == userKey);
                    // Only the current user can fetch their own security challenge questions 
                    if (!userInfo.UserName.Equals(principal.Identity.Name, StringComparison.OrdinalIgnoreCase)
                        || !principal.Identity.IsAuthenticated)
                        return retVal.Skip(this.m_random.Next(0, retVal.Count)).Take(1);
                    else // Only a random option can be returned  
                        return retVal;

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
            if (!userName.Equals(principal.Identity.Name, StringComparison.OrdinalIgnoreCase)
                || !principal.Identity.IsAuthenticated)
                throw new SecurityException($"Users may only modify their own security challenges");

            // Ensure that the user has been explicitly granted the special security policy
            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.AlterSecurityChallenge).Demand();

            try
            {
                using (var context = this.m_configuration.Provider.GetWriteConnection())
                {
                    context.Open();
                    userName = userName.ToLower();
                    var dbUser = context.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLower() == userName);
                    if (dbUser == null)
                        throw new KeyNotFoundException($"User {userName} not found");
                    context.Delete<DbSecurityUserChallengeAssoc>(o => o.ChallengeKey == challengeKey && o.UserKey == dbUser.Key);
                }
            }
            catch (Exception e)
            {

                this.m_tracer.TraceError("Failed to removing security challenge {0} for user {1}: {2}", challengeKey, userName, e);
                throw new Exception($"Failed to remove security challenge {challengeKey} for {userName}", e);
            }
        }

        /// <summary>
        /// Set the specified challenge response
        /// </summary>
        public void Set(String userName, Guid challengeKey, string response, IPrincipal principal)
        {
            if (!userName.Equals(principal.Identity.Name, StringComparison.OrdinalIgnoreCase)
                || !principal.Identity.IsAuthenticated)
                throw new SecurityException($"Users may only modify their own security challenges");
            else if (String.IsNullOrEmpty(response))
                throw new ArgumentNullException(nameof(response), "Response to challenge must be provided");

            // Ensure that the user has been explicitly granted the special security policy
            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.AlterSecurityChallenge).Demand();

            try
            {
                using (var context = this.m_configuration.Provider.GetWriteConnection())
                {
                    context.Open();
                    userName = userName.ToLower();
                    var dbUser = context.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLower() == userName);
                    if (dbUser == null)
                        throw new KeyNotFoundException($"User {userName} not found");

                    var challengeResponse = ApplicationServiceContext.Current.GetService<IPasswordHashingService>().ComputeHash(response);

                    // Existing?
                    var challengeRec = context.FirstOrDefault<DbSecurityUserChallengeAssoc>(o => o.UserKey == dbUser.Key && o.ChallengeKey == challengeKey);
                    if (challengeRec == null)
                    {
                        context.Insert(new DbSecurityUserChallengeAssoc()
                        {
                            ChallengeKey = challengeKey,
                            UserKey = dbUser.Key,
                            ChallengeResponse = challengeResponse,
                            ExpiryTime = DateTime.Now.Add(this.m_securityConfiguration.GetSecurityPolicy<TimeSpan>(SecurityPolicyIdentification.MaxChallengeAge, new TimeSpan(3650, 0, 0, 0)))
                        });
                    }
                    else if (!this.m_securityConfiguration.GetSecurityPolicy<Boolean>(SecurityPolicyIdentification.ChallengeHistory, false) ||
                        !context.Any<DbSecurityUserChallengeAssoc>(o => o.ChallengeKey == challengeRec.ChallengeKey && o.UserKey == challengeRec.UserKey && o.ChallengeResponse == challengeResponse))
                    {
                        challengeRec.ExpiryTime = DateTime.Now.Add(this.m_securityConfiguration.GetSecurityPolicy<TimeSpan>(SecurityPolicyIdentification.MaxChallengeAge, new TimeSpan(3650, 0, 0, 0)));
                        challengeRec.ChallengeResponse = challengeResponse;
                        context.Update(challengeRec);
                    }
                    else
                        throw new InvalidOperationException("Challenge response cannot be the same as previous");
                }
            }
            catch (Exception e)
            {

                this.m_tracer.TraceError("Failed to removing security challenge {0} for user {1}: {2}", challengeKey, userName, e);
                throw new Exception($"Failed to remove security challenge {challengeKey} for {userName}", e);
            }
        }
    }
}
