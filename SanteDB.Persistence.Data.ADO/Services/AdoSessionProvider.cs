/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.ADO.Configuration;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using SanteDB.Persistence.Data.ADO.Security;
using SanteDB.Server.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Security;

using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Represents a session provider for ADO based sessions
    /// </summary>
    [ServiceProvider("ADO.NET Session Storage")]
    public class AdoSessionProvider : ISessionProviderService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "ADO.NET Session Storage Provider";

        private String[] m_nonSessionClaims = {
            SanteDBClaimTypes.Actor,
            SanteDBClaimTypes.AuthenticationInstant,
            SanteDBClaimTypes.AuthenticationMethod,
            SanteDBClaimTypes.AuthenticationType,
            SanteDBClaimTypes.DefaultNameClaimType,
            SanteDBClaimTypes.DefaultRoleClaimType,
            SanteDBClaimTypes.Email,
            SanteDBClaimTypes.Expiration,
            SanteDBClaimTypes.IsPersistent,
            SanteDBClaimTypes.Name,
            SanteDBClaimTypes.NameIdentifier,
            SanteDBClaimTypes.SanteDBApplicationIdentifierClaim,
            SanteDBClaimTypes.SanteDBDeviceIdentifierClaim,
            SanteDBClaimTypes.SanteDBOTAuthCode,
            SanteDBClaimTypes.SanteDBSessionIdClaim,
            SanteDBClaimTypes.Sid,
            SanteDBClaimTypes.SubjectId,
            SanteDBClaimTypes.Telephone
        };

        // Sync lock
        private Object m_syncLock = new object();

        // Trace source
        private Tracer m_traceSource = new Tracer(AdoDataConstants.IdentityTraceSourceName);

        // Configuration
        private AdoPersistenceConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AdoPersistenceConfigurationSection>();

        // Security configuration
        private SecurityConfigurationSection m_securityConfiguration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SecurityConfigurationSection>();

        // Session cache
        private IAdhocCacheService m_adhocCache = ApplicationServiceContext.Current.GetService<IAdhocCacheService>();

        // Session lookups
        private Int32 m_sessionLookups = 0;

        /// <summary>
        /// Fired when the session is established
        /// </summary>
        public event EventHandler<SessionEstablishedEventArgs> Established;

        /// <summary>
        /// Fired when a session is abandoned
        /// </summary>
        public event EventHandler<SessionEstablishedEventArgs> Abandoned;


        /// <summary>
        /// Create and register a refresh token for the specified principal
        /// </summary>
        public byte[] CreateRefreshToken()
        {
            // First we shall set the refresh claim
            return Guid.NewGuid().ToByteArray();
        }

        /// <summary>
        /// Establish the session
        /// </summary>
        /// <param name="principal">The security principal for which the session is being created</param>
        /// <param name="expiry">The expiration of the session</param>
        /// <param name="aud">The audience of the session</param>
        /// <param name="remoteEp">The remote endpoint from which the session is created</param>
        /// <param name="policyDemands">The policies which are being demanded for this session</param>
        /// <param name="purposeOfUse">The purpose of this session</param>
        /// <returns>A constructed <see cref="global::ThisAssembly:AdoSession"/></returns>
        public ISession Establish(IPrincipal principal, String remoteEp, bool isOverride, String purpose, String[] policyDemands, string lang)
        {
            // Validate the parameters
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));
            else if (!principal.Identity.IsAuthenticated)
                throw new InvalidOperationException("Cannot create a session for a non-authenticated principal");
            else if (!(principal is IClaimsPrincipal))
                throw new ArgumentException("Principal must be ClaimsPrincipal", nameof(principal));
            else if (isOverride && (String.IsNullOrEmpty(purpose) || policyDemands == null || policyDemands.Length == 0))
                throw new InvalidOperationException("Override requests require policy demands and a purpose of use");

            var cprincipal = principal as IClaimsPrincipal;
            try
            {

                using (var context = this.m_configuration.Provider.GetWriteConnection())
                {
                    context.Open();
                    using (var tx = context.BeginTransaction())
                    {
                        var refreshToken = this.CreateRefreshToken();

                        var applicationKey = cprincipal.FindFirst(SanteDBClaimTypes.SanteDBApplicationIdentifierClaim)?.Value;
                        var deviceKey = cprincipal.FindFirst(SanteDBClaimTypes.SanteDBDeviceIdentifierClaim)?.Value;
                        var userKey = cprincipal.FindFirst(SanteDBClaimTypes.Sid).Value;
                        var expiration = DateTimeOffset.Now.Add(this.m_securityConfiguration.GetSecurityPolicy<TimeSpan>(SecurityPolicyIdentification.SessionLength, new TimeSpan(0, 5, 0)));
                        if ((purpose == PurposeOfUseKeys.SecurityAdmin.ToString() ||
                            cprincipal.Claims.Any(o => o.Type == SanteDBClaimTypes.PurposeOfUse && o.Value == PurposeOfUseKeys.SecurityAdmin.ToString()))
                            && policyDemands?.Contains(PermissionPolicyIdentifiers.LoginPasswordOnly) == true) // TODO: Make purpose of use menmonic check instead
                            expiration = DateTimeOffset.Now.Add(new TimeSpan(0, 2, 0));

                        var dbSession = new DbSession()
                        {
                            DeviceKey = deviceKey != null ? (Guid?)Guid.Parse(deviceKey) : null,
                            ApplicationKey = Guid.Parse(applicationKey),
                            UserKey = userKey != null && userKey != deviceKey ? (Guid?)Guid.Parse(userKey) : null,
                            NotBefore = DateTimeOffset.Now,
                            NotAfter = expiration,
                            RefreshExpiration = DateTimeOffset.Now.Add(this.m_securityConfiguration.GetSecurityPolicy<TimeSpan>(SecurityPolicyIdentification.RefreshLength, new TimeSpan(0, 10, 0))),
                            RemoteEndpoint = remoteEp,
                            RefreshToken = ApplicationServiceContext.Current.GetService<IPasswordHashingService>().ComputeHash(BitConverter.ToString(refreshToken).Replace("-", ""))
                        };

                        if (dbSession.ApplicationKey == dbSession.UserKey) // SID == Application = Application Grant
                            dbSession.UserKey = null;

                        dbSession = context.Insert(dbSession);

                        // Setup claims
                        var claims = cprincipal.Claims.ToList();

                        // Did the caller explicitly set policies?
                        var pdp = ApplicationServiceContext.Current.GetService<IPolicyDecisionService>();
                        // Is the principal only valid for pwd reset?
                        if (cprincipal.HasClaim(o => o.Type == SanteDBClaimTypes.SanteDBScopeClaim)) // Allow the createor to specify
                            ;
                        else if (policyDemands?.Length > 0)
                        {

                            if (isOverride)
                                claims.Add(new SanteDBClaim(SanteDBClaimTypes.SanteDBOverrideClaim, "true"));
                            if (!String.IsNullOrEmpty(purpose))
                                claims.Add(new SanteDBClaim(SanteDBClaimTypes.PurposeOfUse, purpose));

                            foreach (var pol in policyDemands)
                            {
                                // Get grant
                                var grant = pdp.GetPolicyOutcome(cprincipal, pol);
                                if (isOverride && grant == PolicyGrantType.Elevate &&
                                    (pol.StartsWith(PermissionPolicyIdentifiers.SecurityElevations) || // Special security elevations don't require override permission
                                    pdp.GetPolicyOutcome(cprincipal, PermissionPolicyIdentifiers.OverridePolicyPermission) == PolicyGrantType.Grant
                                    )) // We are attempting to override
                                    claims.Add(new SanteDBClaim(SanteDBClaimTypes.SanteDBScopeClaim, pol));
                                else if (grant == PolicyGrantType.Grant)
                                    claims.Add(new SanteDBClaim(SanteDBClaimTypes.SanteDBScopeClaim, pol));
                                else
                                    throw new PolicyViolationException(cprincipal, pol, grant);
                            }

                        }

                        claims.Add(new SanteDBClaim(SanteDBClaimTypes.SanteDBSessionIdClaim, dbSession.Key.ToString()));
                        (cprincipal.Identity as IClaimsIdentity).AddClaim(claims.Last());
                        // Add default policies
                        var oizPrincipalPolicies = pdp.GetEffectivePolicySet(cprincipal);
                        // Scopes user is allowed to access
                        claims.AddRange(oizPrincipalPolicies.Where(o => o.Rule == PolicyGrantType.Grant).Select(o => new SanteDBClaim(SanteDBClaimTypes.SanteDBScopeClaim, o.Policy.Oid)));

                        if (!String.IsNullOrEmpty(lang)) // set language
                            claims.Add(new SanteDBClaim(SanteDBClaimTypes.Language, lang));

                        // Claims?
                        foreach (var clm in claims.Where(o => !m_nonSessionClaims.Contains(o.Type)))
                            context.Insert(new DbSessionClaim()
                            {
                                SessionKey = dbSession.Key,
                                ClaimType = clm.Type,
                                ClaimValue = clm.Value,
                            });

                        tx.Commit();

                        var signingService = ApplicationServiceContext.Current.GetService<IDataSigningService>();

                        if (signingService == null)
                        {
                            this.m_traceSource.TraceWarning("No IDataSigningService provided. Session data will be unsigned!");
                            var session = new AdoSecuritySession(dbSession.Key, dbSession.Key.ToByteArray(), refreshToken, dbSession.NotBefore, dbSession.NotAfter, claims.ToArray());
                            this.Established?.Invoke(this, new SessionEstablishedEventArgs(principal, session, true, isOverride, purpose, policyDemands));

                            this.m_adhocCache?.Add($"ses.{session.Key}", session);
                            return session;
                        }
                        else
                        {
                            var signedToken = dbSession.Key.ToByteArray().Concat(signingService.SignData(dbSession.Key.ToByteArray())).ToArray();
                            var signedRefresh = refreshToken.Concat(signingService.SignData(refreshToken)).ToArray();

                            var session = new AdoSecuritySession(dbSession.Key, signedToken, signedRefresh, dbSession.NotBefore, dbSession.NotAfter, claims.ToArray());
                            this.Established?.Invoke(this, new SessionEstablishedEventArgs(principal, session, true, isOverride, purpose, policyDemands));
                            this.m_adhocCache?.Add($"ses.{session.Key}", session);
                            return session;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Error establishing session: {0}", e.Message);
                this.Established?.Invoke(this, new SessionEstablishedEventArgs(principal, null, false, isOverride, purpose, policyDemands));
                throw new SecurityException("Error establishing session", e);
            }
        }

        /// <summary>
        /// Extend the session 
        /// </summary>
        /// <param name="refreshToken">The signed session token to be refreshed</param>
        /// <returns>The session that was extended</returns>
        public ISession Extend(byte[] refreshToken)
        {
            // Validate the parameters
            if (refreshToken == null)
                throw new ArgumentNullException(nameof(refreshToken));

            IDbTransaction tx = null;

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {

                    context.Open();

                    tx = context.BeginTransaction();

                    var signingService = ApplicationServiceContext.Current.GetService<IDataSigningService>();
                    if (signingService == null)
                    {
                        this.m_traceSource.TraceWarning("No IDataSigningService provided. Digital signatures will not be verified");
                    }
                    else if (!signingService.Verify(refreshToken.Take(16).ToArray(), refreshToken.Skip(16).ToArray()))
                        throw new SecurityException("Refresh token appears to have been tampered with");

                    // Get the session to be extended
                    var qToken = BitConverter.ToString(refreshToken.Take(16).ToArray()).Replace("-", "");
                    qToken = ApplicationServiceContext.Current.GetService<IPasswordHashingService>().ComputeHash(qToken);
                    var dbSession = context.SingleOrDefault<DbSession>(o => o.RefreshToken == qToken && o.RefreshExpiration > DateTimeOffset.Now);
                    if (dbSession == null)
                        throw new SecurityTokenValidationException(BitConverter.ToString(refreshToken));

                    var claims = context.Query<DbSessionClaim>(o => o.SessionKey == dbSession.Key).ToArray();

                    // Get rid of the old session
                    context.Delete<DbSessionClaim>(o => o.SessionKey == dbSession.Key);
                    context.Delete(dbSession);

                    // Generate a new session for this user
                    dbSession.Key = Guid.Empty;
                    refreshToken = this.CreateRefreshToken();
                    dbSession.RefreshToken = ApplicationServiceContext.Current.GetService<IPasswordHashingService>().ComputeHash(BitConverter.ToString(refreshToken).Replace("-", ""));
                    dbSession.NotAfter = DateTimeOffset.Now + (dbSession.NotAfter - dbSession.NotBefore); // Extend for original time
                    dbSession.NotBefore = DateTimeOffset.Now;
                    dbSession.RefreshExpiration = dbSession.NotAfter.AddMinutes(10);

                    // Save
                    dbSession = context.Insert(dbSession);

                    foreach (var clm in claims)
                        context.Insert(new DbSessionClaim()
                        {
                            SessionKey = dbSession.Key,
                            ClaimType = clm.ClaimType,
                            ClaimValue = clm.ClaimValue
                        });

                    tx.Commit();

                    if (signingService == null)
                    {
                        this.m_traceSource.TraceWarning("No IDataSigningService provided. Session data will be unsigned!");
                        var session = new AdoSecuritySession(dbSession.Key, dbSession.Key.ToByteArray(), refreshToken, dbSession.NotBefore, dbSession.NotAfter, claims.Select(o => new SanteDBClaim(o.ClaimType, o.ClaimValue)).ToArray());
                        this.m_adhocCache?.Add($"ses.{session.Key}", session);
                        return session;
                    }
                    else
                    {
                        var signedToken = dbSession.Key.ToByteArray().Concat(signingService.SignData(dbSession.Key.ToByteArray())).ToArray();
                        var signedRefresh = refreshToken.Concat(signingService.SignData(refreshToken)).ToArray();
                        var session = new AdoSecuritySession(dbSession.Key, signedToken, signedRefresh, dbSession.NotBefore, dbSession.NotAfter, claims.Select(o => new SanteDBClaim(o.ClaimType, o.ClaimValue)).ToArray());
                        this.m_adhocCache?.Add($"ses.{session.Key}", session);
                        return session;
                    }

                }
                catch (Exception e)
                {
                    tx?.Rollback();
                    this.m_traceSource.TraceError("Error getting session: {0}", e.Message);
                    throw new SecurityException("Error getting session", e);

                }
            }
        }

        /// <summary>
        /// Gets the specified session if valid from a signed session token
        /// </summary>
        /// <param name="sessionToken">The session token to retrieve the session for</param>
        /// <param name="allowExpired">When true, instructs the method to fetch a session even if it is expired</param>
        /// <returns>The fetched session token</returns>
        public ISession Get(byte[] sessionToken, bool allowExpired = false)
        {
            // Validate the parameters
            if (sessionToken == null)
                throw new ArgumentNullException(nameof(sessionToken));

            try
            {

                var signingService = ApplicationServiceContext.Current.GetService<IDataSigningService>();
                var sessionId = new Guid(sessionToken.Take(16).ToArray());
                if (signingService == null)
                    this.m_traceSource.TraceWarning("No IDataSigingService registered. Session data will not be verified");
                else if (sessionToken.Length == 16)
                    this.m_traceSource.TraceWarning("Will not verify signature for session {0}", sessionId);
                else if (!signingService.Verify(sessionToken.Take(16).ToArray(), sessionToken.Skip(16).ToArray()))
                    throw new SecurityException("Session token appears to have been tampered with");

                var session = this.m_adhocCache?.Get<AdoSecuritySession>($"ses.{sessionId}");
                if (session != null)
                {
                    if (session.Key == Guid.Empty)
                    {
                        var exp = this.m_adhocCache.Get<dynamic>($"ses.{sessionId}");
                        return new GenericSession(Convert.FromBase64String((String)exp.Id), null, DateTime.Parse((String)exp.NotBefore), DateTime.Parse((String)exp.NotAfter), (exp.Claims as IEnumerable<dynamic>).Select(o => new SanteDBClaim((String)o.Type, (String)o.Value)).ToArray());
                    }
                    else
                    {
                        return session;
                    }
                }
                else
                {
                    using (var context = this.m_configuration.Provider.GetReadonlyConnection())
                    {
                        context.Open();

                        // Check the cache

                        var dbSession = context.SingleOrDefault<DbSession>(o => o.Key == sessionId);

                        if (dbSession == null)
                            throw new SecuritySessionException(SessionExceptionType.NotEstablished, $"Session {BitConverter.ToString(sessionToken)} not found", null);
                        else if (dbSession.NotAfter < DateTime.Now)
                            throw new SecuritySessionException(SessionExceptionType.Expired, new AdoSecuritySession(dbSession), $"Session {BitConverter.ToString(sessionToken)} is expired", null);
                        else if (dbSession.NotBefore > DateTime.Now)
                            throw new SecuritySessionException(SessionExceptionType.NotYetValid, new AdoSecuritySession(dbSession), $"Session {BitConverter.ToString(sessionToken)} is expired", null);
                        else
                        {
                            var sessionInfo = new KeyValuePair<DbSession, DbSessionClaim[]>(dbSession, context.Query<DbSessionClaim>(o => o.SessionKey == dbSession.Key).ToArray());
                            session = new AdoSecuritySession(sessionInfo.Key.Key, sessionToken, null, sessionInfo.Key.NotBefore, sessionInfo.Key.NotAfter, sessionInfo.Value.Select(o => new SanteDBClaim(o.ClaimType, o.ClaimValue)).ToArray());
                            this.m_adhocCache?.Add($"ses.{sessionId}", session);
                        }

                    }
                }
                return session;
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Error getting session: {0}", e.Message);
                throw new SecuritySessionException(SessionExceptionType.Other, null, $"Could not get session token {BitConverter.ToString(sessionToken)}", e);
            }
        }

        /// <summary>
        /// Abandon the specified session
        /// </summary>
        public void Abandon(ISession session)
        {
            try
            {
                using (var context = this.m_configuration.Provider.GetReadonlyConnection())
                {
                    context.Open();
                    // We want a record of the session, just set the expiration
                    var sessionId = new Guid(session.Id);
                    var dbSession = context.FirstOrDefault<DbSession>(o => o.Key == sessionId && o.NotAfter > DateTimeOffset.Now);
                    if (dbSession == null)
                        return;
                    else
                        dbSession.NotAfter = dbSession.RefreshExpiration = DateTimeOffset.Now;
                    context.Update(dbSession);

                }

                this.Abandoned?.Invoke(this, new SessionEstablishedEventArgs(null, session, true, false, null, null));
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Cannot abandon session {0} - {1}", BitConverter.ToString(session.Id, 0), e);
                this.Abandoned?.Invoke(this, new SessionEstablishedEventArgs(null, session, false, false, null, null));
                throw new SecurityException($"Cannot abandon session {BitConverter.ToString(session.Id)}", e);
            }
        }

    }
}
