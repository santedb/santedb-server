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
 * Date: 2022-5-30
 */
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RestSrvr;
using RestSrvr.Attributes;
using RestSrvr.Message;
using SanteDB.Authentication.OAuth2.Configuration;
using SanteDB.Authentication.OAuth2.Model;
using SanteDB.Core;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Security;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Authentication;
using System.Security.Principal;
using System.Text;
using System.Xml;

namespace SanteDB.Authentication.OAuth2.Rest
{
    /// <summary>
    /// OAuth2 Access Control Service
    /// </summary>
    /// <remarks>An Access Control Service and Token Service implemented using OAUTH 2.0</remarks>
    [ServiceBehavior(Name = "OAuth2", InstanceMode = ServiceInstanceMode.Singleton)]
    [ExcludeFromCodeCoverage]
    public class OAuthServiceBehavior : IOAuthServiceContract
    {
        /// <summary>
        /// Trace Source
        /// </summary>
        protected readonly Tracer m_traceSource = new Tracer(OAuthConstants.TraceSourceName);

        /// <summary>
        /// Policy Enforcement Service.
        /// </summary>
        protected readonly IPolicyEnforcementService m_policyEnforcementService;

        /// <summary>
        /// Configuration for OAuth provider.
        /// </summary>
        protected readonly OAuthConfigurationSection m_configuration;

        /// <summary>
        /// Master secuirity configuration.
        /// </summary>
        protected readonly SanteDB.Core.Security.Configuration.SecurityConfigurationSection m_masterConfig;

        /// <summary>
        /// Localization service.
        /// </summary>
        protected readonly ILocalizationService m_LocalizationService;

        /// <summary>
        /// Session resolver
        /// </summary>
        protected readonly ISessionTokenResolverService m_SessionResolver;

        /// <summary>
        /// Session Provider
        /// </summary>
        protected readonly ISessionProviderService m_SessionProvider;
        /// <summary>
        /// Session Identity Provider that can authenticate and return a principal for a given session.
        /// </summary>
        protected readonly ISessionIdentityProviderService m_SessionIdentityProvider;
        /// <summary>
        /// Application identity provider.
        /// </summary>
        protected readonly IApplicationIdentityProviderService m_AppIdentityProvider;
        /// <summary>
        /// Device identity provider.
        /// </summary>
        protected readonly IDeviceIdentityProviderService m_DeviceIdentityProvider;
        /// <summary>
        /// User identity provider.
        /// </summary>
        protected readonly IIdentityProviderService m_IdentityProvider;
        /// <summary>
        /// JWT Handler to create JWTs with.
        /// </summary>
        protected readonly JsonWebTokenHandler m_JwtHandler;

        protected readonly IAppletSolutionManagerService _AppletSolutionManager;
        private IAssetProvider _AssetProvider;

        protected readonly ISymmetricCryptographicProvider _SymmetricProvider;

        readonly IAuditService _AuditService;


        // XHTML
        private const string XS_HTML = "http://www.w3.org/1999/xhtml";

        protected readonly Dictionary<string, Abstractions.ITokenRequestHandler> _TokenRequestHandlers;
        private readonly Dictionary<string, Func<OAuthAuthorizeRequestContext, object>> _AuthorizeResponseModeHandlers;


        /// <summary>
        /// Policy enforcement service
        /// </summary>
        public OAuthServiceBehavior()
        {
            _AuditService = ApplicationServiceContext.Current.GetAuditService();
            m_policyEnforcementService = ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>();
            var configurationManager = ApplicationServiceContext.Current.GetService<IConfigurationManager>();
            m_configuration = configurationManager.GetSection<OAuthConfigurationSection>();
            m_masterConfig = configurationManager.GetSection<SanteDB.Core.Security.Configuration.SecurityConfigurationSection>();
            m_LocalizationService = ApplicationServiceContext.Current.GetService<ILocalizationService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(ILocalizationService)} in {nameof(ApplicationServiceContext)}.");
            m_SessionResolver = ApplicationServiceContext.Current.GetService<ISessionTokenResolverService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(ISessionTokenResolverService)} in {nameof(ApplicationServiceContext)}.");
            m_SessionProvider = ApplicationServiceContext.Current.GetService<ISessionProviderService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(ISessionProviderService)} in {nameof(ApplicationServiceContext)}.");
            m_AppIdentityProvider = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(IApplicationIdentityProviderService)} in {nameof(ApplicationServiceContext)}.");
            m_DeviceIdentityProvider = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(IDeviceIdentityProviderService)} in {nameof(ApplicationServiceContext)}.");
            m_IdentityProvider = ApplicationServiceContext.Current.GetService<IIdentityProviderService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(IIdentityProviderService)} in {nameof(ApplicationServiceContext)}.");
            _SymmetricProvider = ApplicationServiceContext.Current.GetService<ISymmetricCryptographicProvider>() ?? throw new ApplicationException($"Cannot find instance of {nameof(ISymmetricCryptographicProvider)} in {nameof(ApplicationServiceContext)}.");

            //Optimization - try to resolve from the same session provider. 
            m_SessionIdentityProvider = m_SessionProvider as ISessionIdentityProviderService;



            //Fallback and resolve from DI.
            if (null == m_SessionIdentityProvider)
            {
                m_SessionIdentityProvider = ApplicationServiceContext.Current.GetService<ISessionIdentityProviderService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(ISessionIdentityProviderService)} in {nameof(ApplicationServiceContext)}.");
            }

            m_JwtHandler = new JsonWebTokenHandler();

            //Wire up token request handlers.
            var servicemanager = ApplicationServiceContext.Current.GetService<IServiceManager>();

            var tokenhandlers = servicemanager.CreateInjectedOfAll<Abstractions.ITokenRequestHandler>();

            _TokenRequestHandlers = new Dictionary<string, Abstractions.ITokenRequestHandler>();

            foreach (var handler in tokenhandlers)
            {
                foreach (var granttype in handler.SupportedGrantTypes)
                {
                    if (string.IsNullOrEmpty(granttype))
                    {
                        continue;
                    }

                    try
                    {
                        _TokenRequestHandlers.Add(granttype.Trim().ToLowerInvariant(), handler);
                    }
                    catch (ArgumentException)
                    {
                        m_traceSource.TraceError($"Configuration error. Multiple handlers are configured for the grant type {granttype}. The handler {handler.ServiceName} was not added.");
                    }
                }
            }


            _AppletSolutionManager = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>();

            ApplicationServiceContext.Current.Started += Application_Started;

            _AuthorizeResponseModeHandlers = new Dictionary<string, Func<OAuthAuthorizeRequestContext, object>>();
            _AuthorizeResponseModeHandlers.Add(OAuthConstants.ResponseMode_Query, RenderQueryResponseMode);
            _AuthorizeResponseModeHandlers.Add(OAuthConstants.ResponseMode_Fragment, RenderFragmentResponseMode);
            _AuthorizeResponseModeHandlers.Add(OAuthConstants.ResponseMode_FormPost, RenderFormPostResponseMode);


        }

        private void Application_Started(object sender, EventArgs e)
        {
            //We have to wait for stage 2 otherwise we cannot guarantee that the applets will be loaded.
            if (!string.IsNullOrEmpty(m_configuration.LoginAssetPath))
            {
                _AssetProvider = new LocalFolderAssetProvider(m_configuration.LoginAssetPath);
            }
            else if (!string.IsNullOrEmpty(m_configuration.LoginAssetSolution))
            {
                var applets = _AppletSolutionManager.GetApplets(m_configuration.LoginAssetSolution);

                _AssetProvider = new AppletAssetProvider(applets);
            }
            else
            {
                var applets = _AppletSolutionManager.GetApplets("santedb.core.sln");

                _AssetProvider = new AppletAssetProvider(applets);
            }
        }

        #region Helper Methods
        /// <summary>
        /// Try to resolve a device identity from a token request context.
        /// </summary>
        /// <param name="context">The context for the request.</param>
        /// <returns></returns>
        protected bool TryGetDeviceIdentity(OAuthRequestContextBase context)
        {
            if (null == context)
            {
                return false;
            }

            if (null != context.AuthenticationContext?.Principal?.Identity && context.AuthenticationContext.Principal.Identity is IDeviceIdentity deviceIdentity)
            {
                context.DeviceIdentity = deviceIdentity as IClaimsIdentity;
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(context.XDeviceAuthorizationHeader))
            {
                if (AuthorizationHeader.TryParse(context.XDeviceAuthorizationHeader, out var header))
                {
                    if (header.IsScheme(AuthorizationHeader.Scheme_Basic))
                    {
                        var basiccredentials = Encoding.UTF8.GetString(Convert.FromBase64String(header.Value)).Split(new[] { ":" }, 2, StringSplitOptions.RemoveEmptyEntries);

                        if (basiccredentials.Length == 2)
                        {
                            m_traceSource.TraceVerbose($"Attempting to Authenticate with credentials in {ExtendedHttpHeaderNames.HttpDeviceCredentialHeaderName}.");

                            var principal = m_DeviceIdentityProvider.Authenticate(basiccredentials[0], basiccredentials[1]);

                            if (principal?.Identity is IDeviceIdentity devid)
                            {
                                context.DeviceIdentity = devid as IClaimsIdentity;
                                context.DevicePrincipal = principal as IClaimsPrincipal;
                                return true;
                            }
                            else if (null != principal)
                            {
                                m_traceSource.TraceWarning($"Device authentication successful but identity was not {nameof(IDeviceIdentity)}.");
                            }
                            else
                            {
                                m_traceSource.TraceInfo($"Unsuccessful device authentication.");
                            }
                        }
                        else
                        {
                            m_traceSource.TraceVerbose($"Malformed basic credentials in {ExtendedHttpHeaderNames.HttpDeviceCredentialHeaderName} header.");
                        }
                    }
                    else
                    {
                        m_traceSource.TraceVerbose($"Unsupported scheme {header.Scheme} in {ExtendedHttpHeaderNames.HttpDeviceCredentialHeaderName} header.");
                    }
                }
                else
                {
                    m_traceSource.TraceVerbose($"Invalid {ExtendedHttpHeaderNames.HttpDeviceCredentialHeaderName} format. Expecting {{Scheme}} {{Value}}");
                }
            }

            return false;

        }

        /// <summary>
        /// Try to resolve an application identity from a token request context.
        /// </summary>
        /// <param name="context">The context for the request.</param>
        /// <returns></returns>
        protected bool TryGetApplicationIdentity(OAuthRequestContextBase context)
        {
            if (null == context)
            {
                return false;
            }

            if (null != context.AuthenticationContext?.Principal?.Identity && context.AuthenticationContext.Principal.Identity is IApplicationIdentity applicationIdentity)
            {
                context.ApplicationIdentity = applicationIdentity as IClaimsIdentity;
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(context.ClientId) && !string.IsNullOrWhiteSpace(context.ClientSecret))
            {
                m_traceSource.TraceVerbose("Attempting to authenticate application.");
                var principal = m_AppIdentityProvider.Authenticate(context.ClientId, context.ClientSecret);

                if (null != principal && principal.Identity is IApplicationIdentity appidentity)
                {
                    context.ApplicationIdentity = appidentity as IClaimsIdentity;
                    context.ApplicationPrincipal = principal as IClaimsPrincipal;
                    return true;
                }
                else if (null != principal)
                {
                    m_traceSource.TraceWarning($"Application authentication successful but identity is not {nameof(IApplicationIdentity)}");
                }
                else
                {
                    m_traceSource.TraceInfo($"Application authentication unsuccessful. Client ID: {context.ClientId}");
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the grant type that was provided is allowed by this service. The default implementation checks for a TokenRequestHandler for the grant type.
        /// </summary>
        /// <param name="grantType">The incoming grant type.</param>
        /// <returns>True if the grant type is supported, false otherwise.</returns>
        protected bool IsGrantTypePermitted(string grantType)
        {
            if (string.IsNullOrEmpty(grantType))
            {
                return false;
            }

            return _TokenRequestHandlers.ContainsKey(grantType.Trim().ToLowerInvariant());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="remoteIp"></param>
        /// <returns></returns>
        protected bool TryGetRemoteIp(HttpListenerRequest request, out string remoteIp)
        {
            if (null == request)
            {
                remoteIp = null;
                return false;
            }

            var xforwardedfor = request.Headers["X-Forwarded-For"];

            if (!string.IsNullOrEmpty(xforwardedfor))
            {
                //We need to split this value up. Successive proxies are supposed to append themselves to the end of this value (https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-For). 
                var values = xforwardedfor.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                var val = values.FirstOrDefault()?.Trim();

                remoteIp = val;

            }
            else
            {
                remoteIp = request.RemoteEndPoint?.Address?.ToString();
            }
            return !string.IsNullOrEmpty(remoteIp);
        }

        /// <summary>
        /// Create a descriptor that can be serialized into a JWT or other token format.
        /// </summary>
        protected OAuthRequestContextBase AddTokenDescriptorToContext(OAuthRequestContextBase context)
        {
            var descriptor = new SecurityTokenDescriptor();

            var claimsPrincipal = m_SessionIdentityProvider.Authenticate(context.Session) as IClaimsPrincipal;

            // System claims
            var claims = new Dictionary<string, object>();

            foreach (var claim in claimsPrincipal.Claims)
            {
                if (!m_configuration.AllowedClientClaims.Contains(claim.Type))
                {
                    continue;
                }

                if (null != claim?.Value)
                {
                    if (claims.ContainsKey(claim.Type))
                    {
                        var val = claims[claim.Type];

                        if (val is string originalstr)
                        {
                            if (claim.Value != originalstr)
                            {
                                claims[claim.Type] = new List<string> { originalstr, claim.Value };
                            }
                        }
                        else if (val is List<string> lst)
                        {
                            if (!lst.Contains(claim.Value))
                            {
                                lst.Add(claim.Value);
                            }
                        }
                        else
                        {
                            m_traceSource.TraceWarning($"Claim harmonization error: existing claims type is {val.GetType().Name} which is unrecognized.");
                        }
                    }
                    else
                    {
                        claims.Add(claim.Type, claim.Value);
                    }
                }
            }



            descriptor.Claims = claims;

            // Add JTI
            descriptor.Claims.Add("jti", m_SessionResolver.GetEncodedIdToken(context.Session));

            descriptor.NotBefore = context.Session.NotBefore.UtcDateTime;
            descriptor.Expires = context.Session.NotAfter.UtcDateTime;
            descriptor.IssuedAt = descriptor.NotBefore;
            descriptor.Claims.Add("sub", claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == SanteDBClaimTypes.Sid)?.Value);
            descriptor.Claims.Remove(SanteDBClaimTypes.Sid);

            // Creates signing credentials for the specified application key
            var appid = claimsPrincipal?.Claims?.FirstOrDefault(o => o.Type == SanteDBClaimTypes.SanteDBApplicationIdentifierClaim)?.Value;
            descriptor.Audience = appid; //Audience should be the client id of the app.

            descriptor.Issuer = m_configuration.IssuerName;

            // Signing credentials for the application
            // TODO: Expose this as a configuration option - which key to use other than default
            descriptor.SigningCredentials = CreateSigningCredentials($"SA.{appid}", m_configuration.JwtSigningKey, "default");

            // Is the default an HMAC256 key?
            if ((null == descriptor.SigningCredentials ||
                descriptor.SigningCredentials.Algorithm == SecurityAlgorithms.HmacSha256Signature) &&
                RestOperationContext.Current.Data.TryGetValue(OAuthConstants.DataKey_SymmetricSecret, out object clientsecret)) // OPENID States we should use the application client secret to sign the result , we can only do this if we actually have a symm_secret set
            {
                var secret = (clientsecret is byte[]) ? (byte[])clientsecret : Encoding.UTF8.GetBytes(clientsecret.ToString());
                while (secret.Length < 16)
                {
                    secret = secret.Concat(secret).ToArray();
                }

                descriptor.SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secret) { KeyId = appid }, SecurityAlgorithms.HmacSha256Signature);
            }

            if (null == descriptor.SigningCredentials)
            {
                throw new ApplicationException("No signing key found in configuration");
            }

            context.SecurityTokenDescriptor = descriptor;

            return context;
        }
        /// <summary>
        /// Creates the proper tokens in the context based on the server configuration.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected OAuthRequestContextBase AddTokensToContext(OAuthRequestContextBase context)
        {
            if (null == context)
            {
                return null;
            }

            if (null != context.Session)
            {
                if (null != context.SecurityTokenDescriptor)
                {
                    context.IdToken = m_JwtHandler.CreateToken(context.SecurityTokenDescriptor);
                }

                context.ExpiresIn = context.Session.NotAfter.Subtract(DateTimeOffset.UtcNow);

                context.TokenType = m_configuration.TokenType;

                if (context.TokenType == OAuthConstants.BearerTokenType)
                {
                    context.AccessToken = m_SessionResolver.GetEncodedIdToken(context.Session);
                }
                else
                {
                    context.AccessToken = context.IdToken;
                }
            }

            return context;
        }

        /// <summary>
        /// Create a token response
        /// </summary>
        protected ISession EstablishSession(IPrincipal primaryPrincipal, IPrincipal clientPrincipal, IPrincipal devicePrincipal, List<string> scopes, IEnumerable<IClaim> additionalClaims)
        {
            SanteDBClaimsPrincipal claimsPrincipal = null;

            if (primaryPrincipal is IClaimsPrincipal oizcp)
            {
                claimsPrincipal = new SanteDBClaimsPrincipal(oizcp.Identities);
            }
            else
            {
                claimsPrincipal = new SanteDBClaimsPrincipal(primaryPrincipal.Identity);
            }
            if (clientPrincipal is IClaimsPrincipal && !claimsPrincipal.Identities.OfType<IApplicationIdentity>().Any(o => o.Name == clientPrincipal.Identity.Name))
            {
                claimsPrincipal.AddIdentity(clientPrincipal.Identity as IClaimsIdentity);
            }

            if (devicePrincipal is IClaimsPrincipal && !claimsPrincipal.Identities.OfType<IDeviceIdentity>().Any(o => o.Name == devicePrincipal.Identity.Name))
            {
                claimsPrincipal.AddIdentity(devicePrincipal.Identity as IClaimsIdentity);
            }

            _ = TryGetRemoteIp(RestOperationContext.Current.IncomingRequest, out var remoteIp);

            // Establish the session

            string purposeOfUse = additionalClaims?.FirstOrDefault(o => o.Type == SanteDBClaimTypes.PurposeOfUse)?.Value;

            bool isOverride = additionalClaims?.Any(o => o.Type == SanteDBClaimTypes.SanteDBOverrideClaim) == true || scopes?.Any(o => o == PermissionPolicyIdentifiers.OverridePolicyPermission) == true;

            var session = m_SessionProvider.Establish(claimsPrincipal, remoteIp, isOverride, purposeOfUse, scopes?.ToArray(), additionalClaims.FirstOrDefault(o => o.Type == SanteDBClaimTypes.Language)?.Value);

            _AuditService.Audit().ForSessionStart(session, claimsPrincipal, true).Send();

            return session;
        }

        /// <summary>
        /// Create error condition
        /// </summary>
        private OAuthError CreateErrorResponse(OAuthErrorType errorType, String message, string state = null)
        {
            this.m_traceSource.TraceEvent(EventLevel.Error, message);
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
            return new OAuthError()
            {
                Error = errorType,
                ErrorDescription = message,
                State = state
            };
        }

        /// <summary>
        /// Render the specified asset
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="context">The request context.</param>
        /// <returns></returns>
        private Stream RenderInternal(String assetPath, Model.OAuthAuthorizeRequestContext context)
        {
            var locale = RestOperationContext.Current.IncomingRequest.QueryString["ui_locale"];

            Stream content = null;
            string mimetype = null;

            var bindingparameters = new Dictionary<string, string>();
            bindingparameters.Add("client_id", context.ClientId);
            bindingparameters.Add("redirect_uri", context.RedirectUri);
            bindingparameters.Add("response_type", context.ResponseType);
            bindingparameters.Add("response_mode", context.ResponseMode);
            bindingparameters.Add("state", context.State);
            bindingparameters.Add("scope", context.Scope);
            bindingparameters.Add("nonce", context.Nonce);
            bindingparameters.Add("login_hint", context.LoginHint);
            bindingparameters.Add("username", context.Username);
            bindingparameters.Add("activity_id", context.ActivityId.ToString());
            // bindingparameters.Add("password", context.FormFields?["password"]); //We don't send the password back on an invalid login.

            bindingparameters.Add("error_message", context.ErrorMessage);

            EventHandler handler = (s, e) =>
            {
                content?.Dispose();
            };

            RestOperationContext.Current.Disposed += handler;

            try
            {
                (content, mimetype) = _AssetProvider.GetAsset(assetPath, locale, bindingparameters);

                if (null != content)
                {
                    RestOperationContext.Current.OutgoingResponse.ContentType = mimetype;
                    return content;
                }
                else
                {
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 204;
                    RestOperationContext.Current.OutgoingResponse.StatusDescription = "NO CONTENT";
                    return Stream.Null;
                }
            }
            catch (FileNotFoundException)
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = 404;
                RestOperationContext.Current.OutgoingResponse.StatusDescription = "NOT FOUND";
                return Stream.Null;
            }
            finally
            {
                RestOperationContext.Current.Disposed -= handler;
            }
        }

        /// <summary>
        /// Create signing credentials
        /// </summary>
        /// <param name="keyNames">One or more key names to search (in-order) for a signature for.</param>
        private SigningCredentials CreateSigningCredentials(params string[] keyNames)
        {
            if (null == keyNames || keyNames.Length == 0)
            {
                throw new ArgumentNullException(nameof(keyNames), "Key names are required.");
            }

            SecuritySignatureConfiguration configuration = null;

            foreach (var keyname in keyNames)
            {
                configuration = m_masterConfig.Signatures.FirstOrDefault(s => s.KeyName == keyname);

                if (null != configuration)
                {
                    break;
                }
            }

            if (null == configuration) //No configuration provided, return a null credential back.
            {
                return null;
            }

            // Signing credentials
            switch (configuration.Algorithm)
            {
                case SignatureAlgorithm.RS256:
                case SignatureAlgorithm.RS512:
                    var cert = configuration.Certificate;
                    if (cert == null)
                    {
                        throw new SecurityException("Cannot find certificate to sign data!");
                    }

                    // Signature algorithm
                    string signingAlgorithm = SecurityAlgorithms.RsaSha256;
                    if (configuration.Algorithm == SignatureAlgorithm.RS512)
                    {
                        signingAlgorithm = SecurityAlgorithms.RsaSha512;
                    }
                    return new X509SigningCredentials(cert, signingAlgorithm);
                case SignatureAlgorithm.HS256:
                    byte[] secret = configuration.GetSecret().ToArray();
                    while (secret.Length < 16) //TODO: Why are we doing this?
                    {
                        secret = secret.Concat(secret).ToArray();
                    }

                    var key = new SymmetricSecurityKey(secret);

                    if (!string.IsNullOrEmpty(configuration.KeyName))
                    {
                        key.KeyId = configuration.KeyName;
                    }
                    else
                    {
                        key.KeyId = "0"; //Predefined default KID
                    }


                    return new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256);
                default:
                    throw new SecurityException("Invalid signing configuration");
            }
        }
        #endregion

        #region Token Endpoint
        /// <summary>
        /// OAuth token request
        /// </summary>
        public object Token(NameValueCollection formFields)
        {
            m_traceSource.TraceVerbose("Processing token request.");

            if (null == formFields || formFields.Count == 0)
            {
                m_traceSource.TraceVerbose("Empty request received. Returning error.");
                return this.CreateErrorResponse(OAuthErrorType.invalid_request, "request is empty.");
            }

            var context = new Model.OAuthTokenRequestContext(RestOperationContext.Current, formFields);
            context.AuthenticationContext = AuthenticationContext.Current;
            context.Configuration = m_configuration;

            if (!IsGrantTypePermitted(context.GrantType))
            {
                m_traceSource.TraceInfo("Request has unsupported grant type {0}", context.GrantType);
                return this.CreateErrorResponse(OAuthErrorType.unsupported_grant_type, $"unsupported grant type {context.GrantType}");
            }

            //HACK: Remove this when we figure out how to complete the refactor.
            if (!string.IsNullOrEmpty(context.ClientSecret))
            {
                m_traceSource.TraceVerbose("Adding symmetric key override from client secret in request.");
                context.OperationContext.Data.Add(OAuthConstants.DataKey_SymmetricSecret, context.ClientSecret);
            }

            _ = TryGetDeviceIdentity(context);
            _ = TryGetApplicationIdentity(context);

            var clientClaims = ClaimsUtility.ExtractClientClaims(context.IncomingRequest.Headers);
            // Set the language claim?
            if (!String.IsNullOrEmpty(formFields["ui_locales"]) &&
                !clientClaims.Any(o => o.Type == SanteDBClaimTypes.Language))
            {
                clientClaims.Add(new SanteDBClaim(SanteDBClaimTypes.Language, formFields["ui_locales"]));
            }

            context.AdditionalClaims = clientClaims;

            var handler = _TokenRequestHandlers[context.GrantType];

            if (null == handler) //How did this happen?
            {
                m_traceSource.TraceWarning("Found null handler for grant type {0}.", context.GrantType);
                return this.CreateErrorResponse(OAuthErrorType.unsupported_grant_type, $"unsupported grant type {context.GrantType}");
            }

            m_traceSource.TraceVerbose("Executing token request handler.");
            try
            {
                bool success = handler.HandleRequest(context);

                if (!success)
                {
                    m_traceSource.TraceVerbose("Handler returned error. Type: {1}, Message: {2}", context.ErrorType ?? OAuthErrorType.unspecified_error, context.ErrorMessage);
                    return this.CreateErrorResponse(context.ErrorType ?? OAuthErrorType.unspecified_error, context.ErrorMessage ?? "unspecified error");
                }

                if (null == context.Session) //If the session is null, the handler is delegating session initialization back to us.
                {
                    m_traceSource.TraceVerbose($"Establishing session in {nameof(OAuthServiceBehavior)}. This is expected when the handler does not initialize the session.");
                    context.Session = EstablishSession(context.UserPrincipal ?? context.ApplicationPrincipal, context.ApplicationPrincipal, context.DevicePrincipal, context.Scopes, context.AdditionalClaims);

                    _AuditService.Audit().ForSessionStart(context.Session, context.UserPrincipal ?? context.ApplicationPrincipal, context.Session != null).Send();

                    if (null == context.Session)
                    {
                        m_traceSource.TraceInfo("Error establishing session and handler indicated success.");
                        return CreateErrorResponse(OAuthErrorType.unspecified_error, "error establishing session");
                    }
                }

                m_traceSource.TraceInfo("Token request complete, creating response.");

                AddTokenDescriptorToContext(context);

                AddTokensToContext(context);

                return CreateTokenResponse(context);
            }
            catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
            {
                m_traceSource.TraceError("Unhandled exception from token request handler: {0}", ex.ToString());
                return CreateErrorResponse(OAuthErrorType.unspecified_error, ex.Message);
            }
        }

        /// <summary>
        /// Create a token response.
        /// </summary>
        /// <param name="context">The <see cref="OAuthRequestContextBase"/> that is used to create the response.</param>
        /// <returns>A completed response that can be provided to the caller.</returns>
        protected OAuthTokenResponse CreateTokenResponse(OAuthTokenRequestContext context)
        {
            if (null == context)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (null != context.OutgoingResponse)
            {
                context.OutgoingResponse.ContentType = "application/json";
            }

            var response = new OAuthTokenResponse();

            response.IdToken = context.IdToken;
            response.ExpiresIn = unchecked((int)Math.Floor(context.ExpiresIn.TotalSeconds));
            response.TokenType = m_configuration.TokenType;
            response.AccessToken = context.AccessToken;

            if (null != context.Session.RefreshToken)
            {
                response.RefreshToken = m_SessionResolver.GetEncodedRefreshToken(context.Session);
            }

            return response;
        }

        #endregion

        #region Session Endpoint
        /// <summary>
        /// Get the specified session information
        /// </summary>
        public object Session()
        {
            new SanteDB.Rest.Common.Security.TokenAuthorizationAccessBehavior().Apply(new RestRequestMessage(RestOperationContext.Current.IncomingRequest));

            if (RestOperationContext.Current.Data.TryGetValue(TokenAuthorizationAccessBehavior.RestPropertyNameSession, out var sessobj))
            {
                if (sessobj is ISession session)
                {
                    var context = new OAuthSessionRequestContext(RestOperationContext.Current);
                    context.Session = session;

                    AddTokenDescriptorToContext(context);
                    AddTokensToContext(context);

                    return CreateSessionResponse(context);

                }
            }

            return new OAuthError()
            {
                Error = OAuthErrorType.invalid_request,
                ErrorDescription = "No Such Session"
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected OAuthSessionResponse CreateSessionResponse(OAuthSessionRequestContext context)
        {
            if (null == context)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (null != context.OutgoingResponse)
            {
                context.OutgoingResponse.ContentType = "application/json";
            }

            var response = new OAuthSessionResponse();

            response.IdToken = context.IdToken;
            response.ExpiresIn = unchecked((int)Math.Floor(context.ExpiresIn.TotalSeconds));
            response.TokenType = context.TokenType;
            response.AccessToken = context.AccessToken;

            return response;
        }
        #endregion

        #region Authorize Endpoint
        /// <summary>
        /// HTTP GET Authorization Endpoint.
        /// </summary>
        /// <returns></returns>
        public object Authorize()
        {
            var context = new OAuthAuthorizeRequestContext(RestOperationContext.Current);
            context.AuthenticationContext = AuthenticationContext.Current;
            context.Configuration = m_configuration;

            return AuthorizeInternal(context);
        }

        /// <summary>
        /// HTTP POST Authorization endpoint.
        /// </summary>
        /// <param name="formFields"></param>
        /// <returns></returns>
        public object Authorize_Post(NameValueCollection formFields)
        {
            var context = new OAuthAuthorizeRequestContext(RestOperationContext.Current, formFields);
            context.AuthenticationContext = AuthenticationContext.Current;
            context.Configuration = m_configuration;

            return AuthorizeInternal(context);
        }

        /// <summary>
        /// Internal method for <see cref="Authorize" /> and <see cref="Authorize_Post(NameValueCollection)"/>.
        /// </summary>
        /// <param name="context">Request context</param>
        /// <returns>An object to respond to the caller with.</returns>
        private object AuthorizeInternal(OAuthAuthorizeRequestContext context)
        {
            if (!IsAuthorizeRequestValid(context, out var error))
            {
                return error ?? CreateErrorResponse(OAuthErrorType.unspecified_error, "invalid request", context.State);
            }

            _ = TryGetDeviceIdentity(context);

            if (null != context.DevicePrincipal)
            {
                m_policyEnforcementService.Demand(OAuthConstants.OAuthCodeFlowPolicy, context.DevicePrincipal);
            }

            context.ApplicationIdentity = m_AppIdentityProvider.GetIdentity(context.ClientId) as IClaimsIdentity;

            if (null == context.ApplicationIdentity || context.ApplicationIdentity.Claims.FirstOrDefault(c => c.Type == SanteDBClaimTypes.Sid)?.Value == AuthenticationContext.SystemApplicationSid)
            {
                error = CreateErrorResponse(OAuthErrorType.invalid_client, $"unrecognized client: {context.ClientId}", context.State);
                return false;
            }

            if (!string.IsNullOrEmpty(context.Username))
            {
                try
                {
                    context.UserPrincipal = m_IdentityProvider.Authenticate(context.Username, context.Password) as IClaimsPrincipal;
                    context.UserIdentity = context.UserPrincipal?.Identities?.FirstOrDefault();

                    if (null != context.UserPrincipal)
                    {
                        CreateAuthorizationCode(context);

                        var responsehandler = _AuthorizeResponseModeHandlers[context.ResponseMode];

                        return responsehandler(context);
                    }
                }
                catch (AuthenticationException aex)
                {
                    m_traceSource.TraceInfo("Authentication exception in oauth authorize. {0}", aex.ToString());
                    context.ErrorMessage = aex.Message;
                }
                catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
                {
                    if (ex.InnerException is AuthenticationException aex)
                    {
                        m_traceSource.TraceInfo("Authentication exception in oauth authorize. {0}", aex.ToString());
                        context.ErrorMessage = aex.Message;
                    }
                    else
                    {
                        m_traceSource.TraceWarning("Exception in oauth authorize. {0}", ex.ToString());
                        context.ErrorMessage = ex.Message;
                    }
                }
            }


            return RenderInternal(null, context);
        }

        /// <summary>
        /// Makes an authorization code and adds it to the context.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private OAuthAuthorizeRequestContext CreateAuthorizationCode(OAuthAuthorizeRequestContext context)
        {
            var authcode = new AuthorizationCode();
            authcode.iat = DateTimeOffset.UtcNow;
            authcode.scp = context.Scope;
            authcode.usr = context.UserPrincipal.GetClaimValue(SanteDBClaimTypes.Sid);
            authcode.app = context.ApplicationIdentity?.Claims?.FirstOrDefault(c => c.Type == SanteDBClaimTypes.Sid)?.Value;
            authcode.dev = context.DeviceIdentity?.Claims?.FirstOrDefault(c => c.Type == SanteDBClaimTypes.Sid)?.Value;
            authcode.nonce = context.Nonce;

            var codejson = JsonConvert.SerializeObject(authcode);

            context.Code = _SymmetricProvider.EncryptString(codejson);

            return context;
        }

        /// <summary>
        /// Validate an <see cref="OAuthAuthorizeRequestContext"/> context contains a valid request.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool IsAuthorizeRequestValid(OAuthAuthorizeRequestContext context, out OAuthError error)
        {
            if (null == context)
            {
                error = CreateErrorResponse(OAuthErrorType.invalid_request, "empty request", null);
                return false;
            }

            if (string.IsNullOrEmpty(context.ClientId))
            {
                error = CreateErrorResponse(OAuthErrorType.invalid_request, "missing client_id", context.State);
                return false;
            }

            if (string.IsNullOrEmpty(context.ResponseType))
            {
                error = CreateErrorResponse(OAuthErrorType.invalid_request, "missing response_type", context.State);
                return false;
            }

            if (string.IsNullOrEmpty(context.ResponseType))
            {
                context.ResponseType = "code"; //Default
            }

            if (context.ResponseType != "code")
            {
                error = CreateErrorResponse(OAuthErrorType.unsupported_response_type, $"response_type '{context.ResponseType}' not supported", context.State);
                return false;
            }

            if (string.IsNullOrEmpty(context.ResponseMode))
            {
                switch (context.ResponseType)
                {
                    case "code":
                        context.ResponseMode = "query";
                        break;
                    case "token":
                        context.ResponseMode = "fragment";
                        break;
                    default:
                        break;
                }
            }

            if (!_AuthorizeResponseModeHandlers.ContainsKey(context.ResponseMode))
            {
                error = CreateErrorResponse(OAuthErrorType.unsupported_response_mode, $"response_mode '{context.ResponseMode}' is not supported", context.State);
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Render an authorization response in the form [redirect_uri]?code=XXX&amp;state=YYY
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Stream RenderQueryResponseMode(OAuthAuthorizeRequestContext context)
        {
            context.OutgoingResponse.StatusCode = 302;
            context.OutgoingResponse.StatusDescription = "FOUND";
            context.OutgoingResponse.RedirectLocation = $"{context.RedirectUri}?code={context.Code}&state={context.State}";
            return null;
        }

        /// <summary>
        /// Render an authorization response in the form [redirect_uri]#code=XXX&amp;state=YYY
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Stream RenderFragmentResponseMode(OAuthAuthorizeRequestContext context)
        {
            context.OutgoingResponse.StatusCode = 302;
            context.OutgoingResponse.StatusDescription = "FOUND";
            context.OutgoingResponse.RedirectLocation = $"{context.RedirectUri}#code={context.Code}&state={context.State}";
            return null;
        }

        /// <summary>
        /// Render a redirect oauth post which will post to redirect uri a set of form values with application/x-www-form-urlencoded encoding.
        /// </summary>
        private Stream RenderFormPostResponseMode(OAuthAuthorizeRequestContext context)
        {
            var responsedata = new Dictionary<string, string>();
            responsedata.Add("state", context.State);
            responsedata.Add("code", context.Code);

            var ms = new MemoryStream();
            RestOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            using (var xw = XmlWriter.Create(ms, new XmlWriterSettings() { CloseOutput = false, OmitXmlDeclaration = true }))
            {
                xw.WriteDocType("html", "-//W3C//DTD XHTML 1.0 Transitional//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", null);
                xw.WriteStartElement("html", XS_HTML);
                xw.WriteStartElement("head");
                xw.WriteStartElement("title");
                xw.WriteString("Submit This Form");
                xw.WriteEndElement(); // title
                xw.WriteEndElement(); // head

                xw.WriteStartElement("body", XS_HTML);
                xw.WriteAttributeString("onload", "javascript:document.forms[0].submit()");

                xw.WriteStartElement("form", XS_HTML);
                xw.WriteAttributeString("method", "POST");
                xw.WriteAttributeString("action", context.RedirectUri);

                // Emit data
                foreach (var itm in responsedata)
                {
                    xw.WriteStartElement("input");
                    xw.WriteAttributeString("type", "hidden");
                    xw.WriteAttributeString("name", itm.Key);
                    xw.WriteAttributeString("value", itm.Value);
                    xw.WriteEndElement(); // input
                }
                xw.WriteStartElement("button", XS_HTML);
                xw.WriteAttributeString("type", "submit");
                xw.WriteString("Complete Authentication");
                xw.WriteEndElement();
                xw.WriteEndElement(); // form

                xw.WriteEndElement(); // body
                xw.WriteEndElement(); // html
            }

            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
        #endregion

        #region Content Endpoint
        /// <summary>
        /// Render the specified login asset.
        /// </summary>
        /// <returns>A stream of the rendered login asset</returns>
        public Stream Content(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = 404;
                RestOperationContext.Current.OutgoingResponse.StatusDescription = "NOT FOUND";
                return null;
            }
            var context = new OAuthAuthorizeRequestContext(RestOperationContext.Current);
            return this.RenderInternal(assetPath, context);
        }
        #endregion

        #region Ping Endpoint
        /// <summary>
        /// Perform a ping
        /// </summary>
        public void Ping()
        {
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.NoContent;
        }
        #endregion

        #region Discovery Endpoint
        /// <summary>
        /// Gets the discovery object
        /// </summary>
        public OpenIdConfiguration Discovery()
        {
            try
            {
                RestOperationContext.Current.OutgoingResponse.ContentType = "application/json";
                var authDiscovery = ApplicationServiceContext.Current.GetService<OAuthMessageHandler>() as IApiEndpointProvider;
                var securityConfiguration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SanteDB.Core.Security.Configuration.SecurityConfigurationSection>();
                var retVal = new OpenIdConfiguration();

                // mex configuration
                var mexConfig = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SanteDB.Rest.Common.Configuration.RestConfigurationSection>();
                String boundHostPort = $"{RestOperationContext.Current.IncomingRequest.Url.Scheme}://{RestOperationContext.Current.IncomingRequest.Url.Host}:{RestOperationContext.Current.IncomingRequest.Url.Port}";
                if (!String.IsNullOrEmpty(mexConfig.ExternalHostPort))
                {
                    var tUrl = new Uri(mexConfig.ExternalHostPort);
                    boundHostPort = $"{tUrl.Scheme}://{tUrl.Host}:{tUrl.Port}";
                }
                boundHostPort = $"{boundHostPort}{new Uri(authDiscovery.Url.First()).AbsolutePath}";

                // Now get the settings
                retVal.Issuer = this.m_configuration.IssuerName;
                retVal.TokenEndpoint = $"{boundHostPort}/oauth2_token";
                retVal.AuthorizationEndpoint = $"{boundHostPort}/authorize";
                retVal.UserInfoEndpoint = $"{boundHostPort}/userinfo";
                retVal.GrantTypesSupported = _TokenRequestHandlers.Keys.ToList();
                retVal.IdTokenSigning = securityConfiguration.Signatures.Select(o => o.Algorithm).Distinct().Select(o => o.ToString()).ToList();
                retVal.ResponseTypesSupported = new List<string>() { "code" };
                retVal.ScopesSupported = ApplicationServiceContext.Current.GetService<IPolicyInformationService>().GetPolicies().Select(o => o.Oid).ToList();
                retVal.SigningKeyEndpoint = $"{boundHostPort}/jwks";
                retVal.SubjectTypesSupported = new List<string>() { "public" };
                return retVal;
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Error generating OpenID Metadata: {0}", e);
                throw new Exception("Error generating OpenID Metadata", e);
            }
        }
        #endregion

        #region UserInfo Endpoint
        /// <summary>
        /// Get the specified session information
        /// </summary>
        public object UserInfo()
        {
            new SanteDB.Rest.Common.Security.TokenAuthorizationAccessBehavior().Apply(new RestRequestMessage(RestOperationContext.Current.IncomingRequest));

            if (RestOperationContext.Current.Data.TryGetValue(TokenAuthorizationAccessBehavior.RestPropertyNameSession, out var sessobj))
            {
                if (sessobj is ISession session)
                {
                    var principal = m_SessionIdentityProvider.Authenticate(session) as IClaimsPrincipal;
                    RestOperationContext.Current.OutgoingResponse.ContentType = "application/json";
                    var claims = new Dictionary<string, object>();

                    foreach (var claim in principal.Claims)
                    {
                        if (null != claim?.Value && !claims.ContainsKey(claim.Type))
                        {
                            claims.Add(claim.Type, claim.Value);
                        }
                    }

                    return claims;
                }
            }

            RestOperationContext.Current.OutgoingResponse.ContentType = "application/json";
            return new OAuthError()
            {
                Error = OAuthErrorType.invalid_request,
                ErrorDescription = "No Such Session"
            };
        }
        #endregion

        #region JWKS Endpoint
        /// <summary>
        /// Gets the keys associated with this service.
        /// </summary>
        /// <returns></returns>
        public virtual object JsonWebKeySet()
        {
            var keyset = new Microsoft.IdentityModel.Tokens.JsonWebKeySet();

            keyset.SkipUnresolvedJsonWebKeys = true;

            foreach (var signkey in m_masterConfig.Signatures)
            {
                if (null == signkey)
                {
                    continue;
                }

                JsonWebKey jwk = null;

                switch (signkey.Algorithm)
                {
                    case SignatureAlgorithm.RS256:
                    case SignatureAlgorithm.RS512:

                        if (null == signkey.Certificate)
                        {
                            continue;
                        }

                        var x509key = new X509SecurityKey(signkey.Certificate);
                        jwk = JsonWebKeyConverter.ConvertFromX509SecurityKey(x509key);

                        break;
                    case SignatureAlgorithm.HS256:

                        var secret = signkey.GetSecret().ToArray();

                        if (null == secret)
                        {
                            continue;
                        }


                        while (secret.Length < 16) //TODO: Why are we doing this?
                        {
                            secret = secret.Concat(secret).ToArray();
                        }

                        var hmackey = new SymmetricSecurityKey(secret);

                        if (!string.IsNullOrEmpty(signkey.KeyName))
                        {
                            hmackey.KeyId = signkey.KeyName;
                        }
                        else
                        {
                            hmackey.KeyId = "0"; //Predefined default KID
                        }

                        jwk = JsonWebKeyConverter.ConvertFromSymmetricSecurityKey(hmackey);

                        break;
                    default:
                        break;
                }

                if (null != jwk && !keyset.Keys.Any(k => k.KeyId == jwk?.KeyId))
                {
                    keyset.Keys.Add(jwk);
                }
            }


            return new Model.Jwks.KeySet(keyset);
        }
        #endregion
    }
}
