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
using SanteDB.Core.Security.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RestSrvr;
using RestSrvr.Attributes;
using RestSrvr.Message;
using SanteDB.Authentication.OAuth2.Configuration;
using SanteDB.Authentication.OAuth2.Model;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.IdentityModel.JsonWebTokens;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Authentication;

using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Diagnostics.Tracing;
using SanteDB.Core.Applets.Services;
using System.Globalization;
using SanteDB.Core.Http;
using SanteDB.Core.Model.Audit;
using System.Net;
using SanteDB.Rest.Common;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Interop;
using System.Xml;
using SanteDB.Core.Security.Configuration;
using SanteDB.Rest.Common.Security;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace SanteDB.Authentication.OAuth2.Rest
{
    /// <summary>
    /// OAuth2 Access Control Service
    /// </summary>
    /// <remarks>An Access Control Service and Token Service implemented using OAUTH 2.0</remarks>
    [ServiceBehavior(Name = "OAuth2", InstanceMode = ServiceInstanceMode.Singleton)]
    [ExcludeFromCodeCoverage]
    public class OAuthTokenBehavior : IOAuthTokenContract
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
        /// User identity provider. 
        /// </summary>
        protected readonly IIdentityProviderService m_IdentityProvider;
        /// <summary>
        /// Application identity provider.
        /// </summary>
        protected readonly IApplicationIdentityProviderService m_AppIdentityProvider;
        /// <summary>
        /// Device identity provider.
        /// </summary>
        protected readonly IDeviceIdentityProviderService m_DeviceIdentityProvider;
        /// <summary>
        /// JWT Handler to create JWTs with.
        /// </summary>
        protected readonly JsonWebTokenHandler m_JwtHandler;

        // XHTML
        private const string XS_HTML = "http://www.w3.org/1999/xhtml";

        /// <summary>
        /// Policy enforcement service
        /// </summary>
        public OAuthTokenBehavior()
        {
            m_policyEnforcementService = ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>();
            var configurationManager = ApplicationServiceContext.Current.GetService<IConfigurationManager>();
            m_configuration = configurationManager.GetSection<OAuthConfigurationSection>();
            m_masterConfig = configurationManager.GetSection<SanteDB.Core.Security.Configuration.SecurityConfigurationSection>();
            m_LocalizationService = ApplicationServiceContext.Current.GetService<ILocalizationService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(ILocalizationService)} in {nameof(ApplicationServiceContext)}.");
            m_SessionResolver = ApplicationServiceContext.Current.GetService<ISessionTokenResolverService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(ISessionTokenResolverService)} in {nameof(ApplicationServiceContext)}.");
            m_SessionProvider = ApplicationServiceContext.Current.GetService<ISessionProviderService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(ISessionProviderService)} in {nameof(ApplicationServiceContext)}.");
            m_IdentityProvider = ApplicationServiceContext.Current.GetService<IIdentityProviderService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(IIdentityProviderService)} in {nameof(ApplicationServiceContext)}.");
            m_AppIdentityProvider = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(IApplicationIdentityProviderService)} in {nameof(ApplicationServiceContext)}.");
            m_DeviceIdentityProvider = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(IDeviceIdentityProviderService)} in {nameof(ApplicationServiceContext)}.");

            //Optimization - try to resolve from the same session provider. 
            m_SessionIdentityProvider = m_SessionProvider as ISessionIdentityProviderService;

            //Fallback and resolve from DI.
            if (null == m_SessionIdentityProvider)
            {
                m_SessionIdentityProvider = ApplicationServiceContext.Current.GetService<ISessionIdentityProviderService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(ISessionIdentityProviderService)} in {nameof(ApplicationServiceContext)}.");
            }

            m_JwtHandler = new JsonWebTokenHandler();
        }

        /// <summary>
        /// Try to resolve a device identity from a token request context.
        /// </summary>
        /// <param name="authContext"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual bool TryGetDeviceIdentity(Core.Security.AuthenticationContext authContext, OAuthTokenRequest request)
        {
            if (null == request)
            {
                return false;
            }

            if (null != authContext?.Principal?.Identity && authContext.Principal.Identity is IDeviceIdentity deviceIdentity)
            {
                request.DeviceIdentity = deviceIdentity;
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(request.XDeviceAuthorizationHeader))
            {
                if (AuthorizationHeader.TryParse(request.XDeviceAuthorizationHeader, out var header))
                {
                    if (header.IsScheme(AuthorizationHeader.Schemes_Basic))
                    {
                        var basiccredentials = Encoding.UTF8.GetString(Convert.FromBase64String(header.Value)).Split(new[] { ":" }, 2, StringSplitOptions.RemoveEmptyEntries);

                        if (basiccredentials.Length == 2)
                        {
                            m_traceSource.TraceVerbose($"Attempting to Authenticate with credentials in {OAuthConstants.Header_XDeviceAuthorization}.");

                            var principal = m_DeviceIdentityProvider.Authenticate(basiccredentials[0], basiccredentials[1]);

                            if (principal?.Identity is IDeviceIdentity devid)
                            {
                                request.DeviceIdentity = devid;
                                request.DevicePrincipal = principal as IClaimsPrincipal;
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
                            m_traceSource.TraceVerbose($"Malformed basic credentials in {OAuthConstants.Header_XDeviceAuthorization} header.");
                        }
                    }
                    else
                    {
                        m_traceSource.TraceVerbose($"Unsupported scheme {header.Scheme} in {OAuthConstants.Header_XDeviceAuthorization} header.");
                    }
                }
                else
                {
                    m_traceSource.TraceVerbose($"Invalid {OAuthConstants.Header_XDeviceAuthorization} format. Expecting {{Scheme}} {{Value}}");
                }
            }

            return false;

        }

        /// <summary>
        /// Try to resolve an application identity from a token request context.
        /// </summary>
        /// <param name="authContext"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual bool TryGetApplicationIdentity(Core.Security.AuthenticationContext authContext, OAuthTokenRequest request)
        {
            if (null == request)
            {
                return false;
            }

            if (null != authContext?.Principal?.Identity && authContext.Principal.Identity is IApplicationIdentity applicationIdentity)
            {
                request.ApplicationIdentity = applicationIdentity;
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(request.ClientId) && !string.IsNullOrWhiteSpace(request.ClientSecret))
            {
                m_traceSource.TraceVerbose("Attempting to authenticate application.");
                var principal = m_AppIdentityProvider.Authenticate(request.ClientId, request.ClientSecret);

                if (null != principal && principal.Identity is IApplicationIdentity appidentity)
                {
                    request.ApplicationIdentity = appidentity;
                    request.ApplicationPrincipal = principal as IClaimsPrincipal;
                    return true;
                }
                else if (null != principal)
                {
                    m_traceSource.TraceWarning($"Application authentication successful but identity is not {nameof(IApplicationIdentity)}");
                }
                else
                {
                    m_traceSource.TraceInfo($"Application authentication unsuccessful. Client ID: {request.ClientId}");
                }
            }

            return false;
        }

        /// <summary>
        /// Handle a Token request with the refresh_token grant type.
        /// </summary>
        /// <param name="tokenRequest"></param>
        /// <returns></returns>
        protected virtual object ProcessRefreshTokenGrantRequest(OAuthTokenRequest tokenRequest)
        {
            if (string.IsNullOrEmpty(tokenRequest?.RefreshToken))
            {
                return this.CreateErrorCondition(OAuthErrorType.invalid_grant, "missing refresh token.");
            }

            var session = m_SessionResolver.ExtendSessionWithRefreshToken(tokenRequest.RefreshToken);

            if (null == session)
            {
                return this.CreateErrorCondition(OAuthErrorType.invalid_grant, "invalid refresh token.");
            }

            return CreateTokenResponse(CreateTokenDescriptor(session), session, RestOperationContext.Current.OutgoingResponse);
        }

        /// <summary>
        /// Handle a Token request with the password grant type.
        /// </summary>
        /// <param name="tokenRequest"></param>
        /// <returns></returns>
        protected virtual object ProcessPasswordGrantRequest(OAuthTokenRequest tokenRequest)
        {
            if (string.IsNullOrEmpty(tokenRequest?.UserName))
            {
                m_traceSource.TraceInfo("Missing username in Token request.");
                return CreateErrorCondition(OAuthErrorType.invalid_request, "missing username in the request.");
            }

            m_policyEnforcementService.Demand(OAuthConstants.OAuthPasswordFlowPolicy, tokenRequest.ApplicationPrincipal);

            if (null != tokenRequest.DevicePrincipal)
            {
                m_policyEnforcementService.Demand(OAuthConstants.OAuthPasswordFlowPolicy, tokenRequest.DevicePrincipal);
            }

            IPrincipal userprincipal = null;

            if (!string.IsNullOrEmpty(tokenRequest.TfaSecret))
            {
                userprincipal = m_IdentityProvider.Authenticate(tokenRequest.UserName, tokenRequest.Password, tokenRequest.TfaSecret);
            }
            else
            {
                userprincipal = m_IdentityProvider.Authenticate(tokenRequest.UserName, tokenRequest.Password);
            }

            if (null == userprincipal)
            {
                m_traceSource.TraceInfo("Authentication failed in Token request.");
                return CreateErrorCondition(OAuthErrorType.invalid_grant, "invalid password.");
            }

            var session = EstablishSession(userprincipal, tokenRequest.ApplicationPrincipal, tokenRequest.DevicePrincipal, tokenRequest.Scopes, tokenRequest.AdditionalClaims);

            if (null == session)
            {
                m_traceSource.TraceWarning("Failed to establish session.");
                return this.CreateErrorCondition(OAuthErrorType.unauthorized_client, "Unauthorized client.");
            }

            return CreateTokenResponse(CreateTokenDescriptor(session), session, RestOperationContext.Current.OutgoingResponse);
        }

        /// <summary>
        /// Handle a token request with the client_redentials grant type.
        /// </summary>
        /// <param name="tokenRequest"></param>
        /// <returns></returns>
        protected virtual object ProcessClientCredentialsGrantRequest(OAuthTokenRequest tokenRequest)
        {
            if (string.IsNullOrEmpty(tokenRequest?.ClientId))
            {
                m_traceSource.TraceInfo("Missing client id in Token request.");
                return this.CreateErrorCondition(OAuthErrorType.invalid_grant, "invalid client id");
            }

            if (null == tokenRequest.ApplicationPrincipal && !TryGetApplicationIdentity(AuthenticationContext.Current, tokenRequest)) //Try in case an override removed this.
            {
                m_traceSource.TraceInfo("Wrong or missing client secret in Token request.");
                return this.CreateErrorCondition(OAuthErrorType.invalid_client, "invalid client secret");
            }

            m_policyEnforcementService?.Demand(OAuthConstants.OAuthClientCredentialFlowPolicy);

            if (null != tokenRequest.DevicePrincipal)
            {
                m_policyEnforcementService.Demand(OAuthConstants.OAuthClientCredentialFlowPolicy, tokenRequest.DevicePrincipal);
            }

            if (null == tokenRequest.DevicePrincipal && m_configuration?.AllowClientOnlyGrant != true)
            {
                m_traceSource.TraceError("No device principal was authenticated and AllowClientOnlyGrant is not enabled.");
                return this.CreateErrorCondition(OAuthErrorType.unauthorized_client, $"{OAuthConstants.GrantNameClientCredentials} grant type requires device authentication either using X509 or X-Device-Authorization or enabling the DeviceAuthorizationAccessBehavior in the configuration.");
            }

            var session = EstablishSession(tokenRequest.DevicePrincipal ?? tokenRequest.ApplicationPrincipal, tokenRequest.ApplicationPrincipal, tokenRequest.DevicePrincipal, tokenRequest.Scopes, tokenRequest.AdditionalClaims);

            if (null == session)
            {
                m_traceSource.TraceWarning("Failed to establish session.");
                return this.CreateErrorCondition(OAuthErrorType.unauthorized_client, "invalid client credentials");
            }

            return CreateTokenResponse(CreateTokenDescriptor(session), session, RestOperationContext.Current.OutgoingResponse);
        }

        protected virtual bool IsGrantTypePermitted(string grantType)
        {
            switch (grantType)
            {
                case OAuthConstants.GrantNameAuthorizationCode:
                case OAuthConstants.GrantNameClientCredentials:
                case OAuthConstants.GrantNamePassword:
                case OAuthConstants.GrantNameRefresh:
                case OAuthConstants.GrantNameReset:
                    return true;
                default:
                    return false;
            }

        }

        /// <summary>
        /// OAuth token request
        /// </summary>
        public virtual object Token(NameValueCollection formFields)
        {
            if (null == formFields || formFields.Count == 0)
            {
                return this.CreateErrorCondition(OAuthErrorType.invalid_request, "request is empty.");
            }

            var tokenrequestmodel = new Model.OAuthTokenRequest();

            tokenrequestmodel.GrantType = formFields[OAuthConstants.FormField_GrantType];

            if (!IsGrantTypePermitted(tokenrequestmodel.GrantType))
            {
                return this.CreateErrorCondition(OAuthErrorType.unsupported_grant_type, $"grant type {tokenrequestmodel.GrantType} is not supported.");
            }

            //Always try to load up a client id and secret for the request.
            tokenrequestmodel.ClientId = formFields[OAuthConstants.FormField_ClientId];
            tokenrequestmodel.ClientSecret = formFields[OAuthConstants.FormField_ClientSecret];

            //HACK: Remove this when we figure out how to complete the refactor.
            if (!string.IsNullOrEmpty(tokenrequestmodel.ClientSecret))
            {
                RestOperationContext.Current.Data.Add("symm_secret", tokenrequestmodel.ClientSecret);
            }

            tokenrequestmodel.XDeviceAuthorizationHeader = RestOperationContext.Current?.IncomingRequest?.Headers?[OAuthConstants.Header_XDeviceAuthorization];
            tokenrequestmodel.TfaSecret = RestOperationContext.Current?.IncomingRequest?.Headers?[OAuthConstants.Header_TfaSecret];

            var hasdeviceidentity = TryGetDeviceIdentity(AuthenticationContext.Current, tokenrequestmodel);
            var hasappidentity = TryGetApplicationIdentity(AuthenticationContext.Current, tokenrequestmodel);

            var clientClaims = SanteDBClaimsUtil.ExtractClaims(RestOperationContext.Current.IncomingRequest.Headers);
            // Set the language claim?
            if (!String.IsNullOrEmpty(formFields["ui_locales"]) &&
                !clientClaims.Any(o => o.Type == SanteDBClaimTypes.Language))
                clientClaims.Add(new SanteDBClaim(SanteDBClaimTypes.Language, formFields["ui_locales"]));

            tokenrequestmodel.AdditionalClaims = clientClaims;


            switch (tokenrequestmodel.GrantType)
            {
                case OAuthConstants.GrantNameRefresh:
                    tokenrequestmodel.RefreshToken = formFields[OAuthConstants.FormField_RefreshToken];
                    return ProcessRefreshTokenGrantRequest(tokenrequestmodel);
                case OAuthConstants.GrantNameClientCredentials:
                    return ProcessClientCredentialsGrantRequest(tokenrequestmodel);
                case OAuthConstants.GrantNamePassword:
                    tokenrequestmodel.UserName = formFields[OAuthConstants.FormField_Username];
                    tokenrequestmodel.Password = formFields[OAuthConstants.FormField_Password];
                    return ProcessPasswordGrantRequest(tokenrequestmodel);
                default:
                    return this.CreateErrorCondition(OAuthErrorType.unsupported_grant_type, $"grant type {tokenrequestmodel.GrantType} is not supported");
            }


            //// Only password grants
            //if (tokenRequest["grant_type"] != OAuthConstants.GrantNamePassword &&
            //    tokenRequest["grant_type"] != OAuthConstants.GrantNameRefresh &&
            //    //tokenRequest["grant_type"] != OAuthConstants.GrantNameAuthorizationCode &&
            //    tokenRequest["grant_type"] != OAuthConstants.GrantNameClientCredentials &&
            //    tokenRequest["grant_type"] != OAuthConstants.GrantNameReset)
            //    return this.CreateErrorCondition(OAuthErrorType.unsupported_grant_type, "Only 'password', 'client_credentials' or 'refresh_token' grants supported");

            //// Password grant needs well formed scope which defaults to * or all permissions
            //if (tokenRequest["scope"] == null)
            //    tokenRequest.Add("scope", "*");
            //// Validate username and password

            try
            {
                //// Client principal
                //IPrincipal clientPrincipal = Core.Security.AuthenticationContext.Current.Principal;
                //// Client is not present so look in body
                //if (clientPrincipal == null || clientPrincipal == Core.Security.AuthenticationContext.AnonymousPrincipal ||
                //    !(clientPrincipal.Identity is IApplicationIdentity))
                //{
                //    string client_identity = tokenRequest["client_id"],
                //        client_secret = tokenRequest["client_secret"];
                //    if (string.IsNullOrEmpty(client_identity) || string.IsNullOrEmpty(client_secret))
                //        return this.CreateErrorCondition(OAuthErrorType.invalid_client, "Missing client credentials");

                //    try
                //    {
                //        clientPrincipal = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>().Authenticate(client_identity, client_secret);
                //        RestOperationContext.Current.Data.Add("symm_secret", client_secret);
                //    }
                //    catch (Exception e)
                //    {
                //        this.m_traceSource.TraceError("Error authenticating client: {0}", e.Message);
                //        return this.CreateErrorCondition(OAuthErrorType.unauthorized_client, e.Message);
                //    }
                //}
                //else if (!clientPrincipal.Identity.IsAuthenticated)
                //    return this.CreateErrorCondition(OAuthErrorType.unauthorized_client, "Unauthorized Client");

                //// Device principal?
                //IPrincipal devicePrincipal = null;
                //if (Core.Security.AuthenticationContext.Current.Principal.Identity is IDeviceIdentity)
                //    devicePrincipal = Core.Security.AuthenticationContext.Current.Principal;
                //else
                //{
                //    var authHead = RestOperationContext.Current.IncomingRequest.Headers["X-Device-Authorization"];

                //    // TODO: X509 Authentication
                //    //if (RestOperationContext.Current.ServiceSecurityContext.AuthorizationContext.ClaimSets != null)
                //    //{
                //    //    var claimSet = OperationContext.Current.ServiceSecurityContext.AuthorizationContext.ClaimSets.OfType<System.IdentityModel.Claims.X509CertificateClaimSet>().FirstOrDefault();
                //    //    if (claimSet != null) // device authenticated with X509 PKI Cert
                //    //        devicePrincipal = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>().Authenticate(claimSet.X509Certificate);
                //    //}
                //    if (devicePrincipal == null && !String.IsNullOrEmpty(authHead)) // Device is authenticated using basic auth
                //    {
                //        if (!authHead.ToLower().StartsWith("basic "))
                //            throw new InvalidCastException("X-Device-Authorization must be BASIC scheme");

                //        var authParts = Encoding.UTF8.GetString(Convert.FromBase64String(authHead.Substring(6).Trim())).Split(':');
                //        devicePrincipal = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>().Authenticate(authParts[0], authParts[1]);
                //    }
                //}

                //IPrincipal principal = null;

                //var clientClaims = SanteDBClaimsUtil.ExtractClaims(RestOperationContext.Current.IncomingRequest.Headers);
                //// Set the language claim?
                //if (!String.IsNullOrEmpty(formFields["ui_locales"]) &&
                //    !clientClaims.Any(o => o.Type == SanteDBClaimTypes.Language))
                //    clientClaims.Add(new SanteDBClaim(SanteDBClaimTypes.Language, formFields["ui_locales"]));

                //// perform auth
                //switch (formFields["grant_type"])
                //{
                //    case OAuthConstants.GrantNameReset: // password reset grant (special token which only allows a session to reset their password)

                //        formFields["scope"] = PermissionPolicyIdentifiers.LoginPasswordOnly;

                //        // Password grants allowed for this application? Becuase this grant is only for password grants
                //        this.m_policyEnforcementService.Demand(OAuth2.OAuthConstants.OAuthResetFlowPolicy, clientPrincipal);
                //        if (devicePrincipal != null)
                //            this.m_policyEnforcementService.Demand(OAuth2.OAuthConstants.OAuthResetFlowPolicy, devicePrincipal);

                //        // Validate
                //        if (String.IsNullOrWhiteSpace(formFields["username"]) || String.IsNullOrWhiteSpace(formFields["challenge"]) || String.IsNullOrWhiteSpace(formFields["response"]))
                //            return this.CreateErrorCondition(OAuthErrorType.invalid_request, "Invalid client grant message");

                //        // Authenticate the user
                //        var tfa = RestOperationContext.Current.IncomingRequest.Headers[OAuthConstants.Header_TfaSecret];
                //        principal = ApplicationServiceContext.Current.GetService<ISecurityChallengeIdentityService>().Authenticate(formFields["username"], Guid.Parse(formFields["challenge"]), formFields["response"], tfa);
                //        break;

                //    case OAuthConstants.GrantNameClientCredentials:
                //        this.m_policyEnforcementService.Demand(OAuth2.OAuthConstants.OAuthClientCredentialFlowPolicy, clientPrincipal);
                //        if (devicePrincipal != null)
                //            this.m_policyEnforcementService.Demand(OAuth2.OAuthConstants.OAuthPasswordFlowPolicy, devicePrincipal);

                //        if (devicePrincipal == null && !this.m_configuration.AllowClientOnlyGrant)
                //            throw new SecurityException("client_credentials grant requires device authentication either using X509 or X-Device-Authorization or enabling the DeviceAuthorizationAccessBehavior");
                //        else if (devicePrincipal == null)
                //            this.m_traceSource.TraceWarning("No device credential could be established, configuration allows for client only grant. Recommend disabling this in production environment");
                //        else
                //            this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.LoginAsService, devicePrincipal);

                //        principal = devicePrincipal ?? clientPrincipal;
                //        // Demand "Login As Service" permission
                //        break;

                //    case OAuthConstants.GrantNamePassword:

                //        // Password grants allowed for this application?
                //        this.m_policyEnforcementService.Demand(OAuth2.OAuthConstants.OAuthPasswordFlowPolicy, clientPrincipal);
                //        if (devicePrincipal != null)
                //            this.m_policyEnforcementService.Demand(OAuth2.OAuthConstants.OAuthPasswordFlowPolicy, devicePrincipal);

                //        // Validate
                //        if (String.IsNullOrWhiteSpace(formFields["username"]) && String.IsNullOrWhiteSpace(formFields["refresh_token"]))
                //            return this.CreateErrorCondition(OAuthErrorType.invalid_request, "Invalid client grant message");

                //        if (RestOperationContext.Current.IncomingRequest.Headers[OAuthConstants.Header_TfaSecret] != null)
                //            principal = m_IdentityProvider.Authenticate(formFields["username"], formFields["password"], RestOperationContext.Current.IncomingRequest.Headers[OAuthConstants.Header_TfaSecret]);
                //        else
                //            principal = m_IdentityProvider.Authenticate(formFields["username"], formFields["password"]);
                //        break;

                //    case OAuthConstants.GrantNameRefresh:
                //        var refreshToken = formFields["refresh_token"];

                //        //GetSessionFromRefreshToken is internally calling extend for us right now.
                //        principal = (m_SessionProvider as ISessionIdentityProviderService).Authenticate(m_SessionResolver.ExtendSessionWithRefreshToken(refreshToken));
                //        break;

                //    case OAuthConstants.GrantNameAuthorizationCode:

                //        // First, ensure the authenticated application has permission to use this grant
                //        this.m_policyEnforcementService.Demand(OAuthConstants.OAuthCodeFlowPolicy, clientPrincipal);
                //        if (devicePrincipal != null)
                //            this.m_policyEnforcementService.Demand(OAuth2.OAuthConstants.OAuthPasswordFlowPolicy, devicePrincipal);

                //        // We want to decode the token and verify ..
                //        var token = Enumerable.Range(0, formFields["code"].Length)
                //                    .Where(x => x % 2 == 0)
                //                    .Select(x => Convert.ToByte(formFields["code"].Substring(x, 2), 16))
                //                    .ToArray();

                //        // First we extract the token information
                //        var sid = new Guid(Enumerable.Range(0, 32).Where(x => x % 2 == 0).Select(o => token[o]).ToArray());
                //        var sec = new Guid(token.Take(16).ToArray());
                //        var aid = new Guid(Enumerable.Range(32, 32).Where(x => x % 2 == 0).Select(o => token[o]).ToArray());
                //        var scopeLength = BitConverter.ToInt32(token, 64);
                //        var scopeData = Enumerable.Range(68, scopeLength * 2).Where(x => x % 2 == 0).Select(o => token[o]).ToArray();
                //        var claimLength = BitConverter.ToInt32(token, 68 + scopeLength * 2);
                //        var claimData = Enumerable.Range(72 + scopeLength * 2, claimLength * 2).Where(x => x % 2 == 0).Select(o => token[o]).ToArray();
                //        var dsig = token.Skip(72 + 2 * (scopeLength + claimLength)).Take(32).ToArray();
                //        var expiry = new DateTime(BitConverter.ToInt64(token, 104 + 2 * (scopeLength + claimLength)));

                //        // Verify
                //        if (!ApplicationServiceContext.Current.GetService<IDataSigningService>().Verify(token.Take(token.Length - 40).ToArray(), dsig))
                //            throw new SecurityTokenValidationException("Authorization code failed signature verification");

                //        // Expiry?
                //        if (expiry < DateTime.Now)
                //            throw new SecurityTokenExpiredException("Authorization code is expired");

                //        // Verify the application is the same purported by the client
                //        if (aid.ToString() != (clientPrincipal.Identity as IClaimsIdentity).FindFirst(SanteDBClaimTypes.Sid).Value)
                //            throw new SecurityTokenValidationException("Authorization code was not issued to this client");

                //        // Fetch the principal information
                //        principal = m_IdentityProvider.Authenticate(m_IdentityProvider.GetIdentity(sid).Name, null, sec.ToString());

                //        // Add scopes
                //        int li = 0, idx;
                //        string scopes = "";
                //        if (scopeData.Length > 0)
                //        {
                //            do
                //            {
                //                idx = Array.IndexOf(scopeData, (byte)0, li);
                //                if (idx > 0) scopes += Encoding.UTF8.GetString(scopeData, li, idx - li) + " ";
                //                li += idx + 1;
                //            } while (idx > -1);
                //            formFields["scope"] = scopes.Substring(0, scopes.Length - 1);
                //        }
                //        // TODO: Claims

                //        break;

                //    default:
                //        throw new InvalidOperationException("Invalid grant type");
                //}

                //if (principal == null)
                //    return this.CreateErrorCondition(OAuthErrorType.invalid_grant, "Invalid username or password");
                //else
                //    return this.EstablishSession(principal, clientPrincipal, devicePrincipal, tokenRequest["scope"], this.ValidateClaims(principal, clientClaims.ToArray()));
            }
            catch (AuthenticationException e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, "Error generating token: {0}", e);
                return this.CreateErrorCondition(OAuthErrorType.invalid_grant, e.Message);
            }
            catch (SecurityException e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, "Error generating token: {0}", e);
                return this.CreateErrorCondition(OAuthErrorType.invalid_grant, e.Message);
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, "Error generating token: {0}", e);
                return this.CreateErrorCondition(OAuthErrorType.invalid_request, e.Message);
            }
        }

        /// <summary>
        /// Validate claims made by the requestor
        /// </summary>
        private IEnumerable<IClaim> ValidateClaims(IPrincipal userPrincipal, params IClaim[] claims)
        {
            List<IClaim> retVal = new List<IClaim>();

            // HACK: Find a better way to make claims
            // Claims are stored as X-SanteDBACS-Claim headers
            foreach (var itm in claims)
            {
                // Claim allowed
                if (this.m_configuration.AllowedClientClaims == null ||
                    !this.m_configuration.AllowedClientClaims.Contains(itm.Type))
                    throw new SecurityException($"Claim {itm.Type} is not permitted");
                else
                {
                    // Validate the claim
                    var handler = SanteDBClaimsUtil.GetHandler(itm.Type);
                    if (handler == null || handler.Validate(userPrincipal, itm.Value))
                        retVal.Add(itm);
                    else
                        throw new SecurityException($"Claim {itm.Type} failed validation");
                }
            }

            return retVal;
        }

        /// <summary>
        /// Create a descriptor that can be serialized into a JWT or other token format.
        /// </summary>
        protected virtual SecurityTokenDescriptor CreateTokenDescriptor(ISession session)
        {
            var descriptor = new SecurityTokenDescriptor();

            var claimsPrincipal = m_SessionIdentityProvider.Authenticate(session) as IClaimsPrincipal;

            // System claims
            var claims = new Dictionary<string, object>();

            foreach (var claim in claimsPrincipal.Claims)
            {
                if (null != claim?.Value)
                {
                    if (claims.ContainsKey(claim.Type))
                    {
                        var val = claims[claim.Type];

                        if (val is string originalstr && claim.Value != originalstr)
                        {
                            claims[claim.Type] = new List<string> { originalstr, claim.Value };
                        }
                        else if (val is List<string> lst && !lst.Contains(claim.Value))
                        {
                            lst.Add(claim.Value);
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
            descriptor.Claims.Add("jti", m_SessionResolver.GetEncodedIdToken(session));

            descriptor.NotBefore = session.NotBefore.UtcDateTime;
            descriptor.Expires = session.NotAfter.UtcDateTime;
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
                RestOperationContext.Current.Data.TryGetValue("symm_secret", out object clientsecret)) // OPENID States we should use the application client secret to sign the result , we can only do this if we actually have a symm_secret set
            {
                var secret = (clientsecret is byte[]) ? (byte[])clientsecret : Encoding.UTF8.GetBytes(clientsecret.ToString());
                while (secret.Length < 16)
                    secret = secret.Concat(secret).ToArray();
                descriptor.SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secret) { KeyId = appid }, SecurityAlgorithms.HmacSha256Signature);
            }

            if (null == descriptor.SigningCredentials)
            {
                throw new ApplicationException("No signing key found in configuration");
            }

            return descriptor;
        }

        /// <summary>
        /// Create a token response.
        /// </summary>
        /// <param name="token">The token descriptor that was generated by <see cref="CreateTokenDescriptor(ISession, IClaimsPrincipal)"/>.</param>
        /// <param name="session">A session instance to use for the <see cref="OAuthTokenResponse.RefreshToken"/> and <see cref="OAuthTokenResponse.AccessToken"/> if configured.</param>
        /// <returns></returns>
        protected virtual OAuthTokenResponse CreateTokenResponse(SecurityTokenDescriptor token, ISession session, HttpListenerResponse httpResponse)
        {
            var response = new OAuthTokenResponse();

            if (null != httpResponse)
            {
                httpResponse.ContentType = "application/json";
            }

            response.IdentityToken = m_JwtHandler.CreateToken(token);
            response.ExpiresIn = unchecked((int)Math.Floor(session.NotAfter.Subtract(DateTimeOffset.UtcNow).TotalMilliseconds));
            response.TokenType = m_configuration.TokenType;

            if (null != session.RefreshToken)
            {
                response.RefreshToken = m_SessionResolver.GetEncodedRefreshToken(session);
            }

            if (m_configuration.TokenType == OAuthConstants.BearerTokenType) //TODO: Bad name for this; both a JWT and signed session id will be used with a Bearer Authentication header.
            {
                response.AccessToken = m_SessionResolver.GetEncodedIdToken(session);
            }
            else
            {
                response.AccessToken = response.IdentityToken;
            }

            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="remoteIp"></param>
        /// <returns></returns>
        protected virtual bool TryGetRemoteIp(HttpListenerRequest request, out string remoteIp)
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
        /// Create a token response
        /// </summary>
        protected virtual ISession EstablishSession(IPrincipal primaryPrincipal, IPrincipal clientPrincipal, IPrincipal devicePrincipal, List<string> scopes, IEnumerable<IClaim> additionalClaims)
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
                claimsPrincipal.AddIdentity(clientPrincipal.Identity as IClaimsIdentity);
            if (devicePrincipal is IClaimsPrincipal && !claimsPrincipal.Identities.OfType<IDeviceIdentity>().Any(o => o.Name == devicePrincipal.Identity.Name))
                claimsPrincipal.AddIdentity(devicePrincipal.Identity as IClaimsIdentity);

            _ = TryGetRemoteIp(RestOperationContext.Current.IncomingRequest, out var remoteIp);

            // Establish the session

            string purposeOfUse = additionalClaims?.FirstOrDefault(o => o.Type == SanteDBClaimTypes.PurposeOfUse)?.Value;

            bool isOverride = additionalClaims?.Any(o => o.Type == SanteDBClaimTypes.SanteDBOverrideClaim) == true || scopes?.Any(o => o == PermissionPolicyIdentifiers.OverridePolicyPermission) == true;

            return m_SessionProvider.Establish(claimsPrincipal, remoteIp, isOverride, purposeOfUse, scopes?.ToArray(), additionalClaims.FirstOrDefault(o => o.Type == SanteDBClaimTypes.Language)?.Value);

        }

        /// <summary>
        /// Create error condition
        /// </summary>
        private OAuthError CreateErrorCondition(OAuthErrorType errorType, String message)
        {
            this.m_traceSource.TraceEvent(EventLevel.Error, message);
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
            return new OAuthError()
            {
                Error = errorType,
                ErrorDescription = message
            };
        }

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
                    return CreateTokenResponse(CreateTokenDescriptor(session), session, RestOperationContext.Current.OutgoingResponse);
                }
            }

            return new OAuthError()
            {
                Error = OAuthErrorType.invalid_request,
                ErrorDescription = "No Such Session"
            };
        }

        /// <summary>
        /// Post the authorization code to the application
        /// </summary>
        /// <param name="authorization">Authorization post results</param>
        public Stream SelfPost(String content, NameValueCollection authorization)
        {
            return Stream.Null;
            /*
            // Generate an authorization code for this user and redirect to their redirect URL
            var redirectUrl = RestOperationContext.Current.IncomingRequest.QueryString["redirect_uri"] ?? authorization["redirect_uri"];
            var client_id = RestOperationContext.Current.IncomingRequest.QueryString["client_id"] ?? authorization["client_id"];
            var scope = RestOperationContext.Current.IncomingRequest.QueryString.GetValues("scope") ?? authorization.GetValues("scope")?.SelectMany(o => o.Split(' '));
            var claims = RestOperationContext.Current.IncomingRequest.QueryString.GetValues("claim") ?? authorization.GetValues("claim")?.SelectMany(o => o.Split(' '));
            var state = RestOperationContext.Current.IncomingRequest.QueryString["state"] ?? authorization["state"];
            var signature = RestOperationContext.Current.IncomingRequest.QueryString["dsig"] ?? authorization["dsig"];
            var responseType = RestOperationContext.Current.IncomingRequest.QueryString["response_type"] ?? authorization["response_type"] ?? "code";
            var responseMode = RestOperationContext.Current.IncomingRequest.QueryString["response_mode"] ?? authorization["response_mode"] ?? "query";
            var nonce = RestOperationContext.Current.IncomingRequest.QueryString["nonce"] ?? authorization["nonce"];

            AuditEventData audit = new AuditEventData(DateTime.Now, ActionType.Execute, OutcomeIndicator.Success, EventIdentifierType.SecurityAlert, AuditUtil.CreateAuditActionCode(EventTypeCodes.UserAuthentication));
            AuditUtil.AddLocalDeviceActor(audit);

            try
            {
                if (signature == null)
                    throw new ArgumentException("Must provide the dsig parameter");
                if (client_id == null)
                    throw new ArgumentException("Must provide the client_id parameter");
                if (redirectUrl == null)
                    throw new ArgumentException("Must provide redirect_uri parameter");
                if (scope == null)
                    throw new ArgumentException("Must provide at least openid scope");

                // Verify signature
                var signing = ApplicationServiceContext.Current.GetService<IDataSigningService>();
                if (!signing.Verify(Encoding.UTF8.GetBytes(client_id + redirectUrl), signature.Split('-').Select(o => Convert.ToByte(o, 16)).ToArray()))
                    throw new SecurityTokenValidationException("Signature mismatch - client_id or redirect_uri has been tampered with");

                // Authenticate the user and client
                var idp = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
                var iadp = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>();
                var clientIdentity = iadp.GetIdentity(client_id) as IClaimsIdentity;
                var principal = idp.Authenticate(authorization["username"], authorization["password"]) as IClaimsPrincipal;
                var sid = Guid.Parse(principal.FindFirst(SanteDBClaimTypes.Sid).Value);
                var aid = Guid.Parse(clientIdentity.FindFirst(SanteDBClaimTypes.Sid).Value);

                // Are we executing a code flow?
                if (responseType.Contains("code"))
                {
                    var scopeData = scope.Where(o => o != "openid").SelectMany(o => Encoding.UTF8.GetBytes(o).Concat(new byte[] { 0 })).ToArray();
                    var claimData = claims?.SelectMany(o => Encoding.UTF8.GetBytes(o).Union(new byte[] { 0 })).ToArray() ?? new byte[0];
                    // Generate the appropriate authorization code
                    var sec = Guid.NewGuid();
                    byte[] salt = sec.ToByteArray();

                    AuditUtil.AddUserActor(audit, principal);

                    // Generate the token
                    // Token format is :
                    // SID w/SALT . AID w/SALT . SIG
                    byte[] authCode = new byte[112 + scopeData.Length * 2 + claimData.Length * 2];
                    var i = 0;
                    foreach (var b in sid.ToByteArray()) { authCode[i++] = b; authCode[i++] = salt[i % salt.Length]; }
                    foreach (var b in aid.ToByteArray()) { authCode[i++] = b; authCode[i++] = salt[i % salt.Length]; }

                    // Now register the auth code
                    idp.AddClaim(authorization["username"], new SanteDBClaim(SanteDBClaimTypes.SanteDBOTAuthCode, ApplicationServiceContext.Current.GetService<IPasswordHashingService>().ComputeHash(new Guid(authCode.Take(16).ToArray()).ToString())), Core.Security.AuthenticationContext.SystemPrincipal, new TimeSpan(0, 1, 0));
                    idp.AddClaim(authorization["username"], new SanteDBClaim(SanteDBClaimTypes.SanteDBCodeAuth, "true"), Core.Security.AuthenticationContext.SystemPrincipal, new TimeSpan(0, 1, 0));

                    Array.Copy(BitConverter.GetBytes(scopeData.Length), 0, authCode, i, 4);
                    i += 4; // 2 byte portion
                    foreach (var b in scopeData) { authCode[i++] = b; authCode[i++] = salt[i % salt.Length]; }
                    Array.Copy(BitConverter.GetBytes(claimData.Length), 0, authCode, i, 4);
                    i += 4; // 2 byte portion
                    foreach (var b in claimData) { authCode[i++] = b; authCode[i++] = salt[i % salt.Length]; }

                    // Sign the data
                    var dsig = signing.SignData(authCode.Take(authCode.Length - 40).ToArray());
                    Array.Copy(dsig, 0, authCode, i, dsig.Length);
                    i += 32;
                    Array.Copy(BitConverter.GetBytes(DateTime.Now.AddMinutes(1).Ticks), 0, authCode, i, 8);
                    // Encode
                    var tokenString = Base64UrlEncoder.Encode(authCode);

                    // Redirect or post?
                    if (responseMode == "form_post")
                        return this.RenderOAuthAutoPost(redirectUrl, $"code={tokenString}&state={state}");
                    else
                        RestOperationContext.Current.OutgoingResponse.Redirect($"{redirectUrl}?code={tokenString}&state={state}");
                }
                else if (responseType.Contains("token"))
                {
                    // Establish session
                    var claimList = claims?.Select(o => new SanteDBClaim(o.Split('=')[0], o.Split('=')[1])).ToList() ?? new List<SanteDBClaim>();
                    if (!String.IsNullOrEmpty(nonce)) // append nonce
                        claimList.Add(new SanteDBClaim("nonce", nonce));
                    var response = this.EstablishSession(principal, new SanteDBClaimsPrincipal(clientIdentity), null, String.Join(" ", scope.Where(o => !o.Equals("openid"))), claimList);
                    // Return id token?
                    String redirectString = "";
                    if (responseType.Split(' ').Contains("token"))
                        redirectString += $"access_token={response.AccessToken}&";
                    if (responseType.Split(' ').Contains("id_token"))
                        redirectString += $"id_token={response.IdentityToken}&";
                    redirectString += $"state={state}";

                    if (responseMode == "form_post")
                        return this.RenderOAuthAutoPost(redirectUrl, redirectString);
                    else
                        RestOperationContext.Current.OutgoingResponse.Redirect($"{redirectUrl}?{redirectString}");
                }
                return null;
            }
            catch (Exception e)
            {
                AuditUtil.AddUserActor(audit);
                audit.Outcome = OutcomeIndicator.SeriousFail;
                this.m_traceSource.TraceError("Could not create authentication token: {0}", e.Message);

                var bindingParms = RestOperationContext.Current.IncomingRequest.QueryString.AllKeys.ToDictionary(o => o, o => String.Join(" ", RestOperationContext.Current.IncomingRequest.QueryString.GetValues(o)));
                foreach (var itm in authorization.AllKeys)
                    if (itm != "password" && !bindingParms.ContainsKey(itm))
                        bindingParms.Add(itm, authorization[itm]);
                bindingParms.Add("auth_error", e.Message.Replace("\n", "").Replace("\r", ""));
                return this.RenderInternal(content, bindingParms);
            }
            finally
            {
                AuditUtil.SendAudit(audit);
            }
            */
        }

        /// <summary>
        /// Render a redirect oauth post
        /// </summary>
        private Stream RenderOAuthAutoPost(string redirectUri, string formData)
        {
            var ms = new MemoryStream();
            RestOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            using (var xw = XmlWriter.Create(ms, new XmlWriterSettings() { CloseOutput = false, OmitXmlDeclaration = true }))
            {
                xw.WriteDocType("html", "-//W3C//DTD XHTML 1.0 Transitional//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", null);
                xw.WriteStartElement("html", XS_HTML);

                xw.WriteStartElement("body", XS_HTML);
                xw.WriteAttributeString("onload", "javascript:document.forms[0].submit()");

                xw.WriteStartElement("form", XS_HTML);
                xw.WriteAttributeString("method", "POST");
                xw.WriteAttributeString("action", redirectUri);

                // Emit data
                foreach (var itm in formData.Split('&'))
                {
                    var data = itm.Split('=');
                    xw.WriteStartElement("input");
                    xw.WriteAttributeString("type", "hidden");
                    xw.WriteAttributeString("name", data[0]);

                    if (data.Length > 2)
                        data[1] = String.Join("=", data.Skip(1).ToArray());
                    xw.WriteAttributeString("value", data[1]);
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

        /// <summary>
        /// Render the specified login asset
        /// </summary>
        /// <returns>A stream of the rendered login asset</returns>
        public Stream RenderAsset(string content)
        {
            var client = RestOperationContext.Current.IncomingRequest.QueryString["client_id"];
            var redirectUri = RestOperationContext.Current.IncomingRequest.QueryString["redirect_uri"];
            var responseType = RestOperationContext.Current.IncomingRequest.QueryString["response_type"];
            var responseMode = RestOperationContext.Current.IncomingRequest.QueryString["response_mode"];
            var bindingParms = RestOperationContext.Current.IncomingRequest.QueryString.AllKeys.ToDictionary(o => o, o => String.Join(" ", RestOperationContext.Current.IncomingRequest.QueryString.GetValues(o)));
            // Now time to resolve the asset
            if (String.IsNullOrEmpty(content) || content == "index.html")
            {
                // Rule: scope, response_type and client_id and redirect_uri must be provided
                if (String.IsNullOrEmpty(redirectUri) ||
                    String.IsNullOrEmpty(client) ||
                    RestOperationContext.Current.IncomingRequest.QueryString.GetValues("scope")?.SelectMany(s => s.Split(' ')).Contains("openid") != true)
                    throw new InvalidOperationException("OpenID Violation: redirect_uri and client_id must be provided");
                else
                {
                    var applicationId = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>().GetIdentity(client);
                    if (applicationId == null)
                        throw new SecurityException($"Client {client} is not registered with this provider");

                    if (ApplicationServiceContext.Current.GetService<IPolicyDecisionService>().GetPolicyOutcome(new SanteDBClaimsPrincipal(applicationId), OAuthConstants.OAuthCodeFlowPolicy) != PolicyGrantType.Grant)
                        throw new SecurityException($"Client {client} is not allowed to execute this grant type");

                    // TODO: Get claim for application redirect URL
                    var signature = ApplicationServiceContext.Current.GetService<IDataSigningService>().SignData(Encoding.UTF8.GetBytes(client + redirectUri));
                    bindingParms.Add("dsig", BitConverter.ToString(signature));
                }
            }
            bindingParms.Add("auth_error", "&nbsp;");
            return this.RenderInternal(content, bindingParms);
        }

        /// <summary>
        /// Render the specified asset
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="bindingParms"></param>
        /// <returns></returns>
        private Stream RenderInternal(String assetPath, IDictionary<String, String> bindingParms)
        {
            // Get the asset object
            var lander = RestOperationContext.Current.IncomingRequest.QueryString["lander"];

            AppletManifest loginApplet = null;
            ReadonlyAppletCollection loginAppletAssets = null;
            var solutions = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>().Solutions.Select(o => o.Meta.Id).ToList();
            solutions.Add(String.Empty);
            foreach (var sln in solutions)
            {
                loginApplet = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>().GetApplets(sln).Where(o => o.Configuration?.AppSettings.Any(s => s.Name == "oauth2.login") == true && (lander == o.Info.Id || String.IsNullOrEmpty(lander))).FirstOrDefault();
                if (loginApplet != null)
                {
                    loginAppletAssets = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>().GetApplets(sln);
                    break;
                }
            }
            if (loginApplet == null)
                throw new KeyNotFoundException("No asset has been configured as oauth2.login.asset");

            var loginAssetPath = loginApplet.Configuration.AppSettings.FirstOrDefault(o => o.Name == "oauth2.login")?.Value;

            if (String.IsNullOrEmpty(assetPath))
                assetPath = "index.html";

            var assetName = loginAssetPath + assetPath;

            var asset = loginAppletAssets.ResolveAsset(assetName);
            if (asset == null)
                throw new KeyNotFoundException($"{assetName} not found");

            RestOperationContext.Current.OutgoingResponse.ContentType = DefaultContentTypeMapper.GetContentType(Path.GetExtension(assetPath));
            return new MemoryStream(loginAppletAssets.RenderAssetContent(asset, RestOperationContext.Current.IncomingRequest.QueryString["ui_locale"] ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, allowCache: false, bindingParameters: bindingParms));
        }

        /// <summary>
        /// Perform a ping
        /// </summary>
        public void Ping()
        {
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Gets the discovery object
        /// </summary>
        public OpenIdConfiguration GetDiscovery()
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
                retVal.AuthorizationEndpoint = $"{boundHostPort}/authorize/";
                retVal.UserInfoEndpoint = $"{boundHostPort}/userinfo";
                retVal.GrantTypesSupported = new List<string>() { "client_credentials", "password", "authorization_code" };
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
                        throw new SecurityException("Cannot find certificate to sign data!");

                    // Signature algorithm
                    string signingAlgorithm = SecurityAlgorithms.RsaSha256Signature;
                    if (configuration.Algorithm == SignatureAlgorithm.RS512)
                    {
                        signingAlgorithm = SecurityAlgorithms.RsaSha512Signature;
                    }
                    return new X509SigningCredentials(cert, signingAlgorithm);
                case SignatureAlgorithm.HS256:
                    byte[] secret = configuration.GetSecret().ToArray();
                    while (secret.Length < 16) //TODO: Why are we doing this?
                        secret = secret.Concat(secret).ToArray();

                    var key = new SymmetricSecurityKey(secret);

                    if (!string.IsNullOrEmpty(configuration.KeyName))
                    {
                        key.KeyId = configuration.KeyName;
                    }
                    else
                    {
                        key.KeyId = "0"; //Predefined default KID
                    }


                    return new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256Signature);
                default:
                    throw new SecurityException("Invalid signing configuration");
            }
        }


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

        /// <summary>
        /// Gets the keys associated with this service.
        /// </summary>
        /// <returns></returns>
        public object JsonWebKeySet()
        {
            var keyset = new Microsoft.IdentityModel.Tokens.JsonWebKeySet();

            keyset.SkipUnresolvedJsonWebKeys = true;

            foreach(var signkey in m_masterConfig.Signatures)
            {
                if (null == signkey)
                {
                    continue;
                }

                switch (signkey.Algorithm)
                {
                    case SignatureAlgorithm.RS256:
                    case SignatureAlgorithm.RS512:

                        if (null == signkey.Certificate)
                        {
                            continue;
                        }

                        var x509key = new X509SecurityKey(signkey.Certificate);

                        keyset.Keys.Add(JsonWebKeyConverter.ConvertFromX509SecurityKey(x509key));

                        break;
                    case SignatureAlgorithm.HS256:

                        var secret = signkey.GetSecret().ToArray();

                        if (null == secret)
                        {
                            continue;
                        }

                        
                        while (secret.Length < 16) //TODO: Why are we doing this?
                            secret = secret.Concat(secret).ToArray();
                        var hmackey = new SymmetricSecurityKey(secret);

                        if (!string.IsNullOrEmpty(signkey.KeyName))
                        {
                            hmackey.KeyId = signkey.KeyName;
                        }
                        else
                        {
                            hmackey.KeyId = "0"; //Predefined default KID
                        }

                        keyset.Keys.Add(JsonWebKeyConverter.ConvertFromSymmetricSecurityKey(hmackey));

                        break;
                    default:
                        break;
                }
            }


            return keyset;
        }
    }
}