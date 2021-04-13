using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Configuration;
using SanteDB.Persistence.Data.Model.Security;
using SanteDB.Persistence.Data.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Persistence.Data.Services
{
    /// <summary>
    /// An identity provider service that uses the ADO session table
    /// </summary>
    public class AdoSessionProvider : ISessionIdentityProviderService, ISessionProviderService
    {
        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(AdoSessionProvider));

        // Session configuration
        private AdoPersistenceConfigurationSection m_configuration;

        // Data signing service
        private IDataSigningService m_dataSigningService;

        // Hashing service
        private IPasswordHashingService m_passwordHashingService;

        // Security configuration
        private SecurityConfigurationSection m_securityConfiguration;

        // PDP
        private IPolicyDecisionService m_pdpService;

        // PIP
        private IPolicyInformationService m_pipService;

        // PEP
        private IPolicyEnforcementService m_pepService;

        // TODO: Session 

        /// <summary>
        /// Claims which are not to be stored or set in the session
        /// </summary>
        private readonly String[] m_nonSessionClaims = {
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

        /// <summary>
        /// Creates a new ADO session identity provider with injected configuration manager
        /// </summary>
        public AdoSessionProvider(IConfigurationManager configuration, 
            IDataSigningService dataSigning, 
            IPasswordHashingService passwordHashingService, 
            IPolicyDecisionService policyDecisionService,
            IPolicyInformationService policyInformationService,
            IPolicyEnforcementService policyEnforcementService)
        {
            this.m_configuration = configuration.GetSection<AdoPersistenceConfigurationSection>();
            this.m_securityConfiguration = configuration.GetSection<SecurityConfigurationSection>();
            this.m_dataSigningService = dataSigning;
            this.m_passwordHashingService = passwordHashingService;
            this.m_pdpService = policyDecisionService;
            this.m_pepService = policyEnforcementService;
            this.m_pipService = policyInformationService;
        }

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Databased Session Authentication Provider";

        /// <summary>
        /// Fired when a new session is established
        /// </summary>
        public event EventHandler<SessionEstablishedEventArgs> Established;
        /// <summary>
        /// Fired when a session is abandoned
        /// </summary>
        public event EventHandler<SessionEstablishedEventArgs> Abandoned;

        /// <summary>
        /// Extracts and validates the session token
        /// </summary>
        private bool ExtractValidateSessionKey(byte[] sessionToken, out Guid sessionKey)
        {
            byte[] sessionId = new byte[16], signature = new byte[sessionToken.Length - 16];
            Array.Copy(sessionToken, sessionId, 16);
            Array.Copy(sessionToken, 16, signature, 0, signature.Length);
            sessionKey = new Guid(sessionId);
            return this.m_dataSigningService.Verify(sessionId, signature);
        }

        /// <summary>
        /// Abandons the specified session
        /// </summary>
        public void Abandon(ISession session)
        {
            if(session == null)
            {
                throw new ArgumentNullException(nameof(session), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            // Validate tamper check
            if (!this.ExtractValidateSessionKey(session.Id, out Guid sessionId))
            {
                throw new SecuritySessionException(SessionExceptionType.SignatureFailure, ErrorMessages.ERR_SESSION_TAMPER, null);
            }

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    using (var tx = context.BeginTransaction())
                    {
                        
                        var dbSession = context.FirstOrDefault<DbSession>(o => o.Key == sessionId && o.NotAfter > DateTimeOffset.Now);
                        if (dbSession == null)
                        {
                            return; // Already abandoned
                        }
                        else
                        {
                            dbSession.NotAfter = dbSession.RefreshExpiration = DateTimeOffset.Now;
                            context.Delete<DbSessionClaim>(o => o.SessionKey == dbSession.Key);
                        }

                        context.Update(dbSession);
                        tx.Commit();

                        this.Abandoned?.Invoke(this, new SessionEstablishedEventArgs(null, session, true, false, null, null));
                    }
                }
                catch(Exception e)
                {
                    this.m_tracer.TraceError("Cannot abandon session {0} - {1}", BitConverter.ToString(session.Id), e);
                    this.Abandoned?.Invoke(this, new SessionEstablishedEventArgs(null, session, false, false, null, null));
                    throw new SecuritySessionException(SessionExceptionType.Other, ErrorMessages.ERR_SESSION_ABANDON, e);
                }
            }
        }

        /// <summary>
        /// Authenticate (create a princpal) based off a session
        /// </summary>
        public IPrincipal Authenticate(ISession session)
        {
            if(session == null)
            {
                throw new ArgumentNullException(nameof(session), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            // Validate tamper check
            if (!this.ExtractValidateSessionKey(session.Id, out Guid sessionId))
            {
                throw new SecuritySessionException(SessionExceptionType.SignatureFailure, ErrorMessages.ERR_SESSION_TAMPER, null);
            }

            using (var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {

                    context.Open();
                    var sql = context.CreateSqlStatement<DbSession>()
                        .SelectFrom(typeof(DbSession), typeof(DbSecurityApplication), typeof(DbSecurityUser), typeof(DbSecurityDevice))
                        .InnerJoin<DbSecurityApplication>(o => o.ApplicationKey, o => o.Key)
                        .Join<DbSession, DbSecurityUser>("LEFT", o => o.UserKey, o => o.Key)
                        .Join<DbSession, DbSecurityDevice>("LEFT", o => o.DeviceKey, o => o.Key)
                        .Where<DbSession>(o => o.Key == sessionId);
                    var dbSession = context.FirstOrDefault<CompositeResult<DbSession, DbSecurityApplication, DbSecurityUser, DbSecurityDevice>>(sql);

                    if(dbSession.Object1.NotAfter < DateTimeOffset.Now)
                    {
                        throw new SecuritySessionException(SessionExceptionType.Expired, ErrorMessages.ERR_SESSION_EXPIRE, null);
                    }
                    else if(dbSession.Object1.NotBefore > DateTimeOffset.Now)
                    {
                        throw new SecuritySessionException(SessionExceptionType.NotYetValid, ErrorMessages.ERR_SESSION_NOT_VALID, null);
                    }

                    var crtSession = new AdoSecuritySession(session.Id, null, dbSession.Object1, context.Query<DbSessionClaim>(o => o.SessionKey == dbSession.Object1.Key));

                    // Precendence of identiites in the principal : User , App, Device
                    var identities = new IClaimsIdentity[3];
                    if (dbSession.Object3?.Key != null)
                    {
                        identities[0] = new AdoUserIdentity(dbSession.Object3, "SESSION");
                    }
                    if (dbSession.Object2?.Key != null)
                    {
                        identities[1] = new AdoApplicationIdentity(dbSession.Object2, "SESSION");
                    }
                    if(dbSession.Object4?.Key != null)
                    {
                        identities[2] = new AdoDeviceIdentity(dbSession.Object4, "SESSION");
                    }

                    return new AdoSessionPrincipal(crtSession, identities);
                }
                catch(Exception e)
                {
                    this.m_tracer.TraceError("Error authenticating based on session data - {0}", e);
                    throw new SecuritySessionException(SessionExceptionType.NotEstablished, ErrorMessages.ERR_SESSION_GEN_ERR, e);
                }
            }
        }

        /// <summary>
        /// Establish a new session with the specified principal
        /// </summary>
        public ISession Establish(IPrincipal principal, string remoteEp, bool isOverride, string purpose, string[] scope, string lang)
        {
            if(principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(!principal.Identity.IsAuthenticated)
            {
                throw new SecurityException(ErrorMessages.ERR_SESSION_NOT_AUTH_PRINCIPAL);
            }
            else if(isOverride && (String.IsNullOrEmpty(purpose) || scope == null || scope.Length == 0))
            {
                throw new InvalidOperationException(ErrorMessages.ERR_SESSION_OVERRIDE_WITH_INSUFFICIENT_DATA);
            }

            // Must be claims principal
            if (!(principal is IClaimsPrincipal claimsPrincipal))
            {
                throw new SecurityException(ErrorMessages.ERR_SESSION_NOT_CLAIMS_PRINCIPAL);
            }
            
            // Validate override permission for the user
            if(isOverride)
            {
                this.m_pepService.Demand(PermissionPolicyIdentifiers.OverridePolicyPermission, principal);
            }
            // Validate scopes are valid or can be overridden
            if(scope != null)
            {
                foreach(var pol in scope.Select(o=>this.m_pipService.GetPolicy(o)))
                {
                    var grant = this.m_pdpService.GetPolicyOutcome(principal, pol.Oid);
                    switch(grant)
                    {
                        case Core.Model.Security.PolicyGrantType.Deny:
                            throw new PolicyViolationException(principal, pol, grant);
                        case Core.Model.Security.PolicyGrantType.Elevate: // validate override
                            if(!pol.CanOverride)
                            {
                                throw new PolicyViolationException(principal, pol, Core.Model.Security.PolicyGrantType.Deny);
                            }
                            break;
                    }
                }
            }

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();
                    using(var tx = context.BeginTransaction())
                    {

                        // Generate refresh token
                        IIdentity applicationId = claimsPrincipal.Identities.OfType<IApplicationIdentity>().FirstOrDefault(),
                            deviceId = claimsPrincipal.Identities.OfType<IDeviceIdentity>().FirstOrDefault(),
                            userId = claimsPrincipal.Identities.FirstOrDefault(o => !(o is IApplicationIdentity || o is IDeviceIdentity));

                        if(applicationId == null)
                        {
                            throw new InvalidOperationException(ErrorMessages.ERR_SESSION_NO_APPLICATION_ID);
                        }

                        // Fetch the keys for the identities 
                        Guid? applicationKey = null, deviceKey = null, userKey = null;

                        // Application
                        if(applicationId is AdoIdentity adoApplication)
                        {
                            applicationKey = adoApplication.Sid;
                        }
                        else if(applicationId is IClaimsIdentity claimApplication)
                        {
                            applicationKey = Guid.Parse(claimApplication.FindFirst(SanteDBClaimTypes.Sid)?.Value ?? claimApplication.FindFirst(SanteDBClaimTypes.SanteDBApplicationIdentifierClaim)?.Value);
                        }
                        else
                        {
                            applicationKey = context.FirstOrDefault<DbSecurityApplication>(o => o.PublicId.ToLowerInvariant() == applicationId.Name.ToLowerInvariant())?.Key;
                        }

                        // Device
                        if(deviceId is AdoIdentity adoDevice)
                        {
                            deviceKey = adoDevice.Sid;
                        }
                        else if (deviceId is IClaimsIdentity claimDevice)
                        {
                            deviceKey = Guid.Parse(claimDevice.FindFirst(SanteDBClaimTypes.Sid)?.Value ?? claimDevice.FindFirst(SanteDBClaimTypes.SanteDBDeviceIdentifierClaim)?.Value);
                        }
                        else
                        {
                            deviceKey = context.FirstOrDefault<DbSecurityDevice>(o => o.PublicId.ToLowerInvariant() == deviceId.Name.ToLowerInvariant())?.Key;
                        }

                        // User
                        if(userId is AdoIdentity adoUser)
                        {
                            userKey = adoUser.Sid;
                        }
                        else if(userId is IClaimsIdentity claimUser)
                        {
                            userKey = Guid.Parse(claimUser.FindFirst(SanteDBClaimTypes.Sid)?.Value);
                        }
                        else
                        {
                            userKey = context.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userId.Name)?.Key;
                        }

                        // Establish time limit
                        var expiration = DateTimeOffset.Now.Add(this.m_securityConfiguration.GetSecurityPolicy<TimeSpan>(SecurityPolicyIdentification.SessionLength, new TimeSpan(1, 0, 0)));
                        // User is not really logging in, they are attempting to change their password only
                        if(scope?.Contains(PermissionPolicyIdentifiers.LoginPasswordOnly) == true &&
                            (purpose?.Equals(PurposeOfUseKeys.SecurityAdmin.ToString(), StringComparison.OrdinalIgnoreCase) == true ||
                            claimsPrincipal.FindFirst(SanteDBClaimTypes.PurposeOfUse)?.Value.Equals(PurposeOfUseKeys.SecurityAdmin.ToString(), StringComparison.OrdinalIgnoreCase) == true))
                        {
                            expiration = DateTimeOffset.Now.AddSeconds(120);
                        }

                        // Create sessoin data
                        var refreshToken = Guid.NewGuid();
                        var dbSession = new DbSession()
                        {
                            ApplicationKey = applicationKey.GetValueOrDefault(),
                            DeviceKey = deviceKey,
                            UserKey = userKey,
                            NotBefore = DateTimeOffset.Now,
                            NotAfter = expiration,
                            RefreshExpiration = DateTimeOffset.Now.Add(this.m_securityConfiguration.GetSecurityPolicy<TimeSpan>(SecurityPolicyIdentification.RefreshLength, new TimeSpan(1, 0, 0))),
                            RefreshToken = this.m_passwordHashingService.ComputeHash(refreshToken.ToString())
                        };

                        dbSession = context.Insert(dbSession);

                        // Claims to be added to session
                        var claims = claimsPrincipal.Claims.ToList();
                        if(!claims.Any(c=>c.Type.Equals(SanteDBClaimTypes.SanteDBOverrideClaim)))
                        {
                            // Default = *
                            var sessionScopes = new List<string>();
                            if (scope == null || scope.Contains("*"))
                                sessionScopes.AddRange(this.m_pdpService.GetEffectivePolicySet(principal).Select(c => c.Policy.Oid));

                            // Explicitly set scopes
                            sessionScopes.AddRange(scope.Where(s => !"*".Equals(s)));

                            // Add claims
                            claims.AddRange(sessionScopes.Distinct().Select(o => new SanteDBClaim(SanteDBClaimTypes.SanteDBScopeClaim, o)));
                        }

                        // Specialized language for this user?
                        if(!String.IsNullOrEmpty(lang))
                        {
                            claims.Add(new SanteDBClaim(SanteDBClaimTypes.Language, lang));
                        }

                        // Insert claims to database
                        var dbClaims = claims.Where(c=>!this.m_nonSessionClaims.Contains(c.Type)).Select(o => new DbSessionClaim()
                        {
                            SessionKey = dbSession.Key,
                            ClaimType = o.Type,
                            ClaimValue = o.Value
                        });
                        context.Insert(dbClaims);

                        tx.Commit();

                        var signedToken = dbSession.Key.ToByteArray().Concat(m_dataSigningService.SignData(dbSession.Key.ToByteArray())).ToArray();
                        var signedRefresh = refreshToken.ToByteArray().Concat(m_dataSigningService.SignData(refreshToken.ToByteArray())).ToArray();
                        var session = new AdoSecuritySession(signedToken, signedRefresh, dbSession, dbClaims);
                        this.Established?.Invoke(this, new SessionEstablishedEventArgs(principal, session, true, isOverride, purpose, scope));
                        return session;
                    }
                }
                catch (NullReferenceException e)
                {
                    this.Established?.Invoke(this, new SessionEstablishedEventArgs(principal, null, false, isOverride, purpose, scope));
                    throw new SecuritySessionException(SessionExceptionType.NotEstablished, ErrorMessages.ERR_SESSION_MISSING_IDENTITY_DATA, e);
                }
                catch (Exception e)
                {
                    this.Established?.Invoke(this, new SessionEstablishedEventArgs(principal, null, false, isOverride, purpose, scope));
                    throw new SecuritySessionException(SessionExceptionType.NotEstablished, ErrorMessages.ERR_SESSION_GEN_ERR, e);
                }
            }
        }


        public ISession Extend(byte[] refreshToken)
        {
            throw new NotImplementedException();
        }

        public ISession Get(byte[] sessionToken, bool allowExpired = false)
        {
            throw new NotImplementedException();
        }

        public IIdentity[] GetIdentities(ISession session)
        {
            throw new NotImplementedException();
        }
    }
}
