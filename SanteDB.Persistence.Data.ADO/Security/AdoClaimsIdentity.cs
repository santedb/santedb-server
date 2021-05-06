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
 * Date: 2019-11-27
 */
using SanteDB.Core;
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
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;
using SanteDB.Persistence.Data.ADO.Data.Model.Error;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using SanteDB.Persistence.Data.ADO.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Authentication;

using System.Security.Principal;
using System.Threading;
using SanteDB.Server.Core.Configuration;

namespace SanteDB.Persistence.Data.ADO.Security
{
    /// <summary>
    /// Represents a user prinicpal based on a SecurityUser domain model 
    /// </summary>
    public class AdoClaimsIdentity : SanteDBClaimsIdentity, IIdentity, IClaimsIdentity
    {
        
        // Trace source
        private static Tracer s_traceSource = new Tracer(AdoDataConstants.IdentityTraceSourceName);

        // Lock object
        private static Object s_lockObject = new object();
        
        // The security user
        private DbSecurityUser m_securityUser;
        // The authentication type
        private String m_authenticationType;
        
        // Roles
        private List<DbSecurityRole> m_roles = null;

        // Configuration
        private static AdoPersistenceConfigurationSection s_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AdoPersistenceConfigurationSection>();

        // Security configuration section
        private static SecurityConfigurationSection s_securityConfiguration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SecurityConfigurationSection>();

        /// <summary>
        /// Creates a principal based on username and password
        /// </summary>
        internal static AdoClaimsIdentity Create(String userName, String password)
        {
            try
            {
                if (userName == AuthenticationContext.AnonymousPrincipal.Identity.Name ||
                    userName == AuthenticationContext.SystemPrincipal.Identity.Name)
                {
                    throw new PolicyViolationException(new GenericPrincipal(new GenericIdentity(userName), new String[0]), PermissionPolicyIdentifiers.Login, PolicyGrantType.Deny);
                }

                Guid? userId = Guid.Empty;

                using (var dataContext = s_configuration.Provider.GetWriteConnection())
                {
                    dataContext.Open();

                    // Attempt to get a user
                    var hashingService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();

                    // Generate pepper
                    var passwordHash = AdoDataConstants.PEPPER_CHARS.Select(o=> hashingService.ComputeHash($"{password}{o}"));
                    CompositeResult<DbSecurityUser, FunctionErrorCode> fnResult;
                    try
                    {
                        // v2 auth
                        fnResult = dataContext.ExecuteProcedure<CompositeResult<DbSecurityUser, FunctionErrorCode>>("auth_usr_ex", userName, String.Join(";", passwordHash), s_securityConfiguration?.GetSecurityPolicy(SecurityPolicyIdentification.MaxInvalidLogins, 5) ?? 5);
                    }
                    catch(Exception e)
                    {
                        fnResult = dataContext.ExecuteProcedure<CompositeResult<DbSecurityUser, FunctionErrorCode>>("auth_usr", userName, passwordHash.First(), s_securityConfiguration?.GetSecurityPolicy(SecurityPolicyIdentification.MaxInvalidLogins, 5) ?? 5);
                    }
                    var user = fnResult.Object1;

					if (!String.IsNullOrEmpty(fnResult.Object2.ErrorCode))
	                {
		                if (fnResult.Object2.ErrorCode.Contains("AUTH_LCK:"))
							UpdateCache(user, dataContext);
                        
						throw new AuthenticationException(fnResult.Object2.ErrorCode);
					}


                    var roles = dataContext.Query<DbSecurityRole>(dataContext.CreateSqlStatement< DbSecurityRole>().SelectFrom()
                        .InnerJoin<DbSecurityUserRole>(o => o.Key, o => o.RoleKey)
                        .Where<DbSecurityUserRole>(o => o.UserKey == user.Key));

                    var userIdentity = new AdoClaimsIdentity(user, roles, true) { m_authenticationType = "Password" };

                    // Is user allowed to login?
                    if (user.UserClass == UserClassKeys.HumanUser)
                        new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.Login, new GenericPrincipal(userIdentity, null)).Demand();
                    else if (user.UserClass == UserClassKeys.ApplicationUser)
                        new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.LoginAsService, new GenericPrincipal(userIdentity, null)).Demand();

					// add the security user to the cache before exiting
					UpdateCache(user, dataContext);

					return userIdentity;
                }
            }
            catch (AuthenticationException e) when (e.Message.Contains("AUTH_INV:") || e.Message.Contains("AUTH_LCK:") || e.Message.Contains("AUTH_TFA:"))
            {
                throw new AuthenticationException(e.Message.Substring(0, e.Message.IndexOf(":")), e);
            }
            catch (SecurityException e)
            {
                // TODO: Audit this
                throw new SecurityException($"Error authenticating {userName}", e);
            }
            catch (DbException e)
            {
                s_traceSource.TraceEvent(EventLevel.Error,  "Database Error Creating Identity: {0}", e);
                throw new AuthenticationException(e.Message, e);
            }
            catch (Exception e)
            {
                s_traceSource.TraceEvent(EventLevel.Error,  e.ToString());
                throw new Exception("Creating identity failed", e);
            }
        }
        
        /// <summary>
        /// Create a claims identity from a data context user
        /// </summary>
        internal static AdoClaimsIdentity Create(DataContext context, DbSecurityUser user, bool isAuthenticated = false, String authenticationMethod = null)
        {

            var roles = context.Query<DbSecurityRole>(context.CreateSqlStatement<DbSecurityRole>().SelectFrom()
                        .InnerJoin<DbSecurityUserRole>(o => o.Key, o => o.RoleKey)
                        .Where<DbSecurityUserRole>(o => o.UserKey == user.Key));
            
            return new AdoClaimsIdentity(user, roles, isAuthenticated)
            {
                m_authenticationType = authenticationMethod
            };


        }

        /// <summary>
        /// Creates an identity from a hash
        /// </summary>
        internal static AdoClaimsIdentity Create(String userName)
        {
            try
            {
                using (var dataContext = s_configuration.Provider.GetWriteConnection())
                {
                    dataContext.Open();
                    var user = dataContext.SingleOrDefault<DbSecurityUser>(u => u.ObsoletionTime == null && u.UserName.ToLower() == userName.ToLower());
                    if (user == null)
                        return null;

                    var roles = dataContext.Query<DbSecurityRole>(dataContext.CreateSqlStatement<DbSecurityRole>().SelectFrom()
                        .InnerJoin<DbSecurityUserRole>(o => o.Key, o => o.RoleKey)
                        .Where<DbSecurityUserRole>(o => o.UserKey == user.Key));

                    return new AdoClaimsIdentity(user, roles, false);
                }
            }
            catch (Exception e)
            {
                s_traceSource.TraceEvent(EventLevel.Error,  e.ToString());
                throw new Exception("Creating unauthorized identity failed", e);
            }
        }

        /// <summary>
        /// Private ctor
        /// </summary>
        private AdoClaimsIdentity(DbSecurityUser user, IEnumerable<DbSecurityRole> roles, bool isAuthenticated)
            : base(user.UserName, isAuthenticated, isAuthenticated ? "LOCAL" : null)
        {
            this.m_securityUser = user;
            this.m_roles = roles.ToList();

            if (!this.Claims.Any(o => o.Type == SanteDBClaimTypes.DefaultRoleClaimType))
                this.m_roles.ForEach(r => this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.DefaultRoleClaimType, r.Name)));
        }
        
        /// <summary>
        /// Gets the original user upon which this principal is based
        /// </summary>
        public DbSecurityUser User
        {
            get
            {
                return this.m_securityUser;
            }
        }

        /// <summary>
        /// Create an authorization context
        /// </summary>
        public IClaimsPrincipal CreateClaimsPrincipal(IEnumerable<IClaimsIdentity> otherIdentities = null)
        {

            if (!this.IsAuthenticated)
                throw new SecurityException("Principal is not authenticated");

            try
            {

                // System claims
                List<IClaim> claims = new List<IClaim>(
                    
                )
                {
                    new SanteDBClaim(SanteDBClaimTypes.AuthenticationMethod, this.m_authenticationType ?? "LOCAL"),
                    new SanteDBClaim(SanteDBClaimTypes.Sid, this.m_securityUser.Key.ToString()),
                    new SanteDBClaim(SanteDBClaimTypes.NameIdentifier, this.m_securityUser.Key.ToString()),
                    new SanteDBClaim(SanteDBClaimTypes.Actor, this.m_securityUser.UserClass.ToString()),
                };

                if (!this.Claims.Any(o => o.Type == SanteDBClaimTypes.Name))
                    claims.Add(new SanteDBClaim(SanteDBClaimTypes.Name, this.m_securityUser.UserName));
                if (!this.Claims.Any(o => o.Type == SanteDBClaimTypes.DefaultRoleClaimType))
                    claims.AddRange(this.m_roles.Select(r => new SanteDBClaim(SanteDBClaimTypes.DefaultRoleClaimType, r.Name)));
                if (this.m_securityUser.PasswordExpiration.HasValue && this.m_securityUser.PasswordExpiration < DateTime.Now)
                {
                    s_traceSource.TraceWarning("User {0} password expired on {1}", this.m_securityUser.UserName, this.m_securityUser.PasswordExpiration);
                    claims.Add(new SanteDBClaim(SanteDBClaimTypes.PurposeOfUse, PurposeOfUseKeys.SecurityAdmin.ToString()));
                    claims.Add(new SanteDBClaim(SanteDBClaimTypes.SanteDBScopeClaim, PermissionPolicyIdentifiers.LoginPasswordOnly));
                    claims.Add(new SanteDBClaim(SanteDBClaimTypes.SanteDBScopeClaim, PermissionPolicyIdentifiers.ReadMetadata));
                }
                if (this.m_securityUser.Email != null)
                    claims.Add(new SanteDBClaim(SanteDBClaimTypes.Email, this.m_securityUser.Email));
                if (this.m_securityUser.PhoneNumber != null)
                    claims.Add(new SanteDBClaim(SanteDBClaimTypes.Telephone, this.m_securityUser.PhoneNumber));

                this.AddClaims(claims);

                var identities = new IClaimsIdentity[] { this };
                if (otherIdentities != null)
                    identities = identities.Union(otherIdentities).ToArray();

                // TODO: Demographic data for the user
                var retVal = new SanteDBClaimsPrincipal(
                        identities
                    );
                s_traceSource.TraceInfo("Created security principal from identity {0} > {1}", this, AdoClaimsIdentity.PrincipalToString(retVal));
                return retVal;
            }
            catch (Exception e)
            {
                s_traceSource.TraceEvent(EventLevel.Error,  e.ToString());
                throw new Exception("Creating principal from identity failed", e);
            }
        }
        
        /// <summary>
        /// Represent principal as a string
        /// </summary>
        private static String PrincipalToString(IClaimsPrincipal retVal)
        {
            using (StringWriter sw = new StringWriter())
            {
                sw.Write("{{ Identity = {0}, Claims = [", retVal.Identity);
                foreach (var itm in retVal.Claims)
                {
                    sw.Write("{{ Type = {0}, Value = {1} }}", itm.Type, itm.Value);
                    if (itm != retVal.Claims.Last()) sw.Write(",");
                }
                sw.Write("] }");
                return sw.ToString();
            }
        }

		/// <summary>
		/// Updates the cache.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="context">The context.</param>
		private static void UpdateCache(DbSecurityUser user, DataContext context)
	    {
		    var securityUser = new SanteDB.Persistence.Data.ADO.Services.Persistence.SecurityUserPersistenceService(ApplicationServiceContext.Current.GetService<AdoPersistenceService>()).ToModelInstance(user, context);
		    ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Add(securityUser);
		}
    }
}
