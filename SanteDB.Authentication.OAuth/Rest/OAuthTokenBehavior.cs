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
using System.IdentityModel.Tokens;
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
using SanteDB.Core.Auditing;
using System.Net;
using SanteDB.Rest.Common;
using SanteDB.Server.Core.Rest.Behavior;
using SanteDB.Server.Core.Rest.Security;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Interop;
using System.Xml;
using SanteDB.Core.Exceptions;
using SanteDB.Server.Core.Configuration;
using SanteDB.Server.Core.Security.Attribute;
using SanteDB.Server.Core.Security;
using SanteDB.Server.Core;

namespace SanteDB.Authentication.OAuth2.Rest
{
    /// <summary>
    /// OAuth2 Access Control Service
    /// </summary>
    /// <remarks>An Access Control Service and Token Service implemented using OAUTH 2.0</remarks>
    [ServiceBehavior(Name = "OAuth2")]
    public class OAuthTokenBehavior : IOAuthTokenContract
    {


        // Trace source name
        private Tracer m_traceSource = new Tracer(OAuthConstants.TraceSourceName);

        // OAuth configuration
        private OAuthConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<OAuthConfigurationSection>();

        // Master configuration
        private SecurityConfigurationSection m_masterConfig = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SecurityConfigurationSection>();
        // XHTML
        private const string XS_HTML = "http://www.w3.org/1999/xhtml";

        /// <summary>
        /// OAuth token request
        /// </summary>
        public object Token(NameValueCollection tokenRequest)
        {

            // Get the client application 
            IApplicationIdentityProviderService clientIdentityService = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>();
            IIdentityProviderService identityProvider = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();

            // Only password grants
            if (tokenRequest["grant_type"] != OAuthConstants.GrantNamePassword &&
                tokenRequest["grant_type"] != OAuthConstants.GrantNameRefresh &&
                tokenRequest["grant_type"] != OAuthConstants.GrantNameAuthorizationCode &&
                tokenRequest["grant_type"] != OAuthConstants.GrantNameClientCredentials &&
                tokenRequest["grant_type"] != OAuthConstants.GrantNameReset)
                return this.CreateErrorCondition(OAuthErrorType.unsupported_grant_type, "Only 'password', 'client_credentials' or 'refresh_token' grants supported");

            // Password grant needs well formed scope which defaults to * or all permissions
            if (tokenRequest["scope"] == null)
                tokenRequest.Add("scope", "*");
            // Validate username and password

            try
            {
                // Client principal
                IPrincipal clientPrincipal = Core.Security.AuthenticationContext.Current.Principal;
                // Client is not present so look in body
                if (clientPrincipal == null || clientPrincipal == Core.Security.AuthenticationContext.AnonymousPrincipal ||
                    !(clientPrincipal.Identity is IApplicationIdentity))
                {
                    String client_identity = tokenRequest["client_id"],
                        client_secret = tokenRequest["client_secret"];
                    if (String.IsNullOrEmpty(client_identity) || String.IsNullOrEmpty(client_secret))
                        return this.CreateErrorCondition(OAuthErrorType.invalid_client, "Missing client credentials");

                    try
                    {
                        clientPrincipal = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>().Authenticate(client_identity, client_secret);
                        RestOperationContext.Current.Data.Add("symm_secret", client_secret);
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error authenticating client: {0}", e.Message);
                        return this.CreateErrorCondition(OAuthErrorType.unauthorized_client, e.Message);
                    }
                }
                else if (!clientPrincipal.Identity.IsAuthenticated)
                    return this.CreateErrorCondition(OAuthErrorType.unauthorized_client, "Unauthorized Client");

                // Device principal?
                IPrincipal devicePrincipal = null;
                if (Core.Security.AuthenticationContext.Current.Principal.Identity is IDeviceIdentity)
                    devicePrincipal = Core.Security.AuthenticationContext.Current.Principal;
                else
                {
                    var authHead = RestOperationContext.Current.IncomingRequest.Headers["X-Device-Authorization"];

                    // TODO: X509 Authentication 
                    //if (RestOperationContext.Current.ServiceSecurityContext.AuthorizationContext.ClaimSets != null)
                    //{
                    //    var claimSet = OperationContext.Current.ServiceSecurityContext.AuthorizationContext.ClaimSets.OfType<System.IdentityModel.Claims.X509CertificateClaimSet>().FirstOrDefault();
                    //    if (claimSet != null) // device authenticated with X509 PKI Cert
                    //        devicePrincipal = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>().Authenticate(claimSet.X509Certificate);
                    //}
                    if (devicePrincipal == null && !String.IsNullOrEmpty(authHead)) // Device is authenticated using basic auth
                    {
                        if (!authHead.ToLower().StartsWith("basic "))
                            throw new InvalidCastException("X-Device-Authorization must be BASIC scheme");

                        var authParts = Encoding.UTF8.GetString(Convert.FromBase64String(authHead.Substring(6).Trim())).Split(':');
                        devicePrincipal = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>().Authenticate(authParts[0], authParts[1]);
                    }
                }

                IPrincipal principal = null;

                var clientClaims = SanteDBClaimsUtil.ExtractClaims(RestOperationContext.Current.IncomingRequest.Headers);
                // Set the language claim?
                if (!String.IsNullOrEmpty(tokenRequest["ui_locales"]) &&
                    !clientClaims.Any(o => o.Type == SanteDBClaimTypes.Language))
                    clientClaims.Add(new SanteDBClaim(SanteDBClaimTypes.Language, tokenRequest["ui_locales"]));

                // perform auth
                switch (tokenRequest["grant_type"])
                {
                    case OAuthConstants.GrantNameReset: // password reset grant (special token which only allows a session to reset their password)

                        tokenRequest["scope"] = PermissionPolicyIdentifiers.LoginPasswordOnly;

                        // Password grants allowed for this application? Becuase this grant is only for password grants
                        new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, OAuth2.OAuthConstants.OAuthResetFlowPolicy, clientPrincipal).Demand();
                        if (devicePrincipal != null)
                            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, OAuth2.OAuthConstants.OAuthResetFlowPolicy, devicePrincipal).Demand();

                        // Validate 
                        if (String.IsNullOrWhiteSpace(tokenRequest["username"]) || String.IsNullOrWhiteSpace(tokenRequest["challenge"]) || String.IsNullOrWhiteSpace(tokenRequest["response"]))
                            return this.CreateErrorCondition(OAuthErrorType.invalid_request, "Invalid client grant message");

                        // Authenticate the user
                        var tfa = RestOperationContext.Current.IncomingRequest.Headers[OAuthConstants.TfaHeaderName];
                        principal = ApplicationServiceContext.Current.GetService<ISecurityChallengeIdentityService>().Authenticate(tokenRequest["username"], Guid.Parse(tokenRequest["challenge"]), tokenRequest["response"], tfa);
                        break;
                    case OAuthConstants.GrantNameClientCredentials:
                        new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, OAuth2.OAuthConstants.OAuthClientCredentialFlowPolicy, clientPrincipal).Demand();
                        if (devicePrincipal != null)
                            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, OAuth2.OAuthConstants.OAuthPasswordFlowPolicy, devicePrincipal).Demand();

                        if (devicePrincipal == null && !this.m_configuration.AllowClientOnlyGrant)
                            throw new SecurityException("client_credentials grant requires device authentication either using X509 or X-Device-Authorization or enabling the DeviceAuthorizationAccessBehavior");
                        else if (devicePrincipal == null)
                            this.m_traceSource.TraceWarning("No device credential could be established, configuration allows for client only grant. Recommend disabling this in production environment");
                        else
                            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.LoginAsService, devicePrincipal).Demand();

                        principal = devicePrincipal;
                        // Demand "Login As Service" permission
                        break;
                    case OAuthConstants.GrantNamePassword:

                        // Password grants allowed for this application?
                        new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, OAuth2.OAuthConstants.OAuthPasswordFlowPolicy, clientPrincipal).Demand();
                        if (devicePrincipal != null)
                            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, OAuth2.OAuthConstants.OAuthPasswordFlowPolicy, devicePrincipal).Demand();

                        // Validate 
                        if (String.IsNullOrWhiteSpace(tokenRequest["username"]) && String.IsNullOrWhiteSpace(tokenRequest["refresh_token"]))
                            return this.CreateErrorCondition(OAuthErrorType.invalid_request, "Invalid client grant message");

                        if (RestOperationContext.Current.IncomingRequest.Headers[OAuthConstants.TfaHeaderName] != null)
                            principal = identityProvider.Authenticate(tokenRequest["username"], tokenRequest["password"], RestOperationContext.Current.IncomingRequest.Headers[OAuthConstants.TfaHeaderName]);
                        else
                            principal = identityProvider.Authenticate(tokenRequest["username"], tokenRequest["password"]);
                        break;
                    case OAuthConstants.GrantNameRefresh:
                        var refreshToken = tokenRequest["refresh_token"];
                        var secret = Enumerable.Range(0, refreshToken.Length)
                                    .Where(x => x % 2 == 0)
                                    .Select(x => Convert.ToByte(refreshToken.Substring(x, 2), 16))
                                    .ToArray();
                        principal = (identityProvider as ISessionIdentityProviderService).Authenticate(ApplicationServiceContext.Current.GetService<ISessionProviderService>().Extend(secret));
                        break;
                    case OAuthConstants.GrantNameAuthorizationCode:

                        // First, ensure the authenticated application has permission to use this grant
                        new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, OAuthConstants.OAuthCodeFlowPolicy, clientPrincipal).Demand();
                        if (devicePrincipal != null)
                            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, OAuth2.OAuthConstants.OAuthPasswordFlowPolicy, devicePrincipal).Demand();

                        // We want to decode the token and verify ..
                        var token = Enumerable.Range(0, tokenRequest["code"].Length)
                                    .Where(x => x % 2 == 0)
                                    .Select(x => Convert.ToByte(tokenRequest["code"].Substring(x, 2), 16))
                                    .ToArray();

                        // First we extract the token information
                        var sid = new Guid(Enumerable.Range(0, 32).Where(x => x % 2 == 0).Select(o => token[o]).ToArray());
                        var sec = new Guid(token.Take(16).ToArray());
                        var aid = new Guid(Enumerable.Range(32, 32).Where(x => x % 2 == 0).Select(o => token[o]).ToArray());
                        var scopeLength = BitConverter.ToInt32(token, 64);
                        var scopeData = Enumerable.Range(68, scopeLength * 2).Where(x => x % 2 == 0).Select(o => token[o]).ToArray();
                        var claimLength = BitConverter.ToInt32(token, 68 + scopeLength * 2);
                        var claimData = Enumerable.Range(72 + scopeLength * 2, claimLength * 2).Where(x => x % 2 == 0).Select(o => token[o]).ToArray();
                        var dsig = token.Skip(72 + 2 * (scopeLength + claimLength)).Take(32).ToArray();
                        var expiry = new DateTime(BitConverter.ToInt64(token, 104 + 2 * (scopeLength + claimLength)));

                        // Verify 
                        if (!ApplicationServiceContext.Current.GetService<IDataSigningService>().Verify(token.Take(token.Length - 40).ToArray(), dsig))
                            throw new SecurityTokenValidationException("Authorization code failed signature verification");

                        // Expiry?
                        if (expiry < DateTime.Now)
                            throw new SecurityTokenExpiredException("Authorization code is expired");

                        // Verify the application is the same purported by the client
                        if (aid.ToString() != (clientPrincipal.Identity as IClaimsIdentity).FindFirst(SanteDBClaimTypes.Sid).Value)
                            throw new SecurityTokenValidationException("Authorization code was not issued to this client");

                        // Fetch the principal information
                        principal = identityProvider.Authenticate(identityProvider.GetIdentity(sid).Name, null, sec.ToString());

                        // Add scopes
                        int li = 0, idx;
                        string scopes = "";
                        if (scopeData.Length > 0)
                        {
                            do
                            {
                                idx = Array.IndexOf(scopeData, (byte)0, li);
                                if (idx > 0) scopes += Encoding.UTF8.GetString(scopeData, li, idx - li) + " ";
                                li += idx + 1;
                            } while (idx > -1);
                            tokenRequest["scope"] = scopes.Substring(0, scopes.Length - 1);
                        }
                        // TODO: Claims

                        break;
                    default:
                        throw new InvalidOperationException("Invalid grant type");
                }


                if (principal == null)
                    return this.CreateErrorCondition(OAuthErrorType.invalid_grant, "Invalid username or password");
                else
                    return this.EstablishSession(principal, clientPrincipal, devicePrincipal, tokenRequest["scope"], this.ValidateClaims(principal, clientClaims.ToArray()));
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
        /// Hydrate the JWT token
        /// </summary>
        private JwtSecurityToken HydrateToken(ISession session)
        {
            this.m_traceSource.TraceInfo("Will create new ClaimsPrincipal based on existing principal");


            // System claims
            List<IClaim> claims = session.Claims.ToList();

            // Add JTI
            claims.Add(new SanteDBClaim("jti", BitConverter.ToString(session.Id).Replace("-", "")));
            claims.Add(new SanteDBClaim("iat", (session.NotBefore - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds.ToString()));
            claims.RemoveAll(o => String.IsNullOrEmpty(o.Value));
            claims.Add(new SanteDBClaim("exp", (session.NotAfter - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds.ToString()));
            claims.RemoveAll(o => String.IsNullOrEmpty(o.Value));
            claims.Add(new SanteDBClaim("sub", session.Claims.First(o => o.Type == SanteDBClaimTypes.Sid).Value)); // Subject is the first security identifier
            claims.RemoveAll(o => o.Type == SanteDBClaimTypes.Sid);
            // Creates signing credentials for the specified application key
            var appid = claims.Find(o => o.Type == SanteDBClaimTypes.SanteDBApplicationIdentifierClaim).Value;

            // Signing credentials for the application
            // TODO: Expose this as a configuration option - which key to use other than default
            var signingCredentials = SecurityUtils.CreateSigningCredentials($"SA.{appid}");

            // Was there a signing credentials provided for this application? If so, then create for default
            if(signingCredentials == null)
                signingCredentials = SecurityUtils.CreateSigningCredentials(this.m_configuration.JwtSigningKey); // attempt to get default

            // Is the default an HMAC256 key? 
            if ((signingCredentials == null ||
                signingCredentials.SignatureAlgorithm == "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256") &&
                RestOperationContext.Current.Data.TryGetValue("symm_secret", out object clientSecret)) // OPENID States we should use the application client secret to sign the result , we can only do this if we actually have a symm_secret set
            {

                var secret = Encoding.UTF8.GetBytes(clientSecret.ToString());
                while (secret.Length < 16)
                    secret = secret.Concat(secret).ToArray();
                signingCredentials = new SigningCredentials(
                        new InMemorySymmetricSecurityKey((byte[])secret),
                        "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256",
                        "http://www.w3.org/2001/04/xmlenc#sha256",
                        new SecurityKeyIdentifier(new NamedKeySecurityKeyIdentifierClause("name", appid))
                    );
            }

            // Generate security token            
            var jwt = new JwtSecurityToken(
                signingCredentials: signingCredentials,
                claims: claims.Select(o => new System.Security.Claims.Claim(o.Type, o.Value)),
                issuer: this.m_configuration.IssuerName,
                notBefore: session.NotBefore.DateTime,
                expires: session.NotAfter.DateTime
           );

            return jwt;

        }

        /// <summary>
        /// Create a token response
        /// </summary>
        private OAuthTokenResponse EstablishSession(IPrincipal oizPrincipal, IPrincipal clientPrincipal, IPrincipal devicePrincipal, String scope, IEnumerable<IClaim> additionalClaims)
        {
            var claimsPrincipal = oizPrincipal as IClaimsPrincipal;
            if (clientPrincipal is IClaimsPrincipal && !claimsPrincipal.Identities.OfType<Server.Core.Security.ApplicationIdentity>().Any(o => o.Name == clientPrincipal.Identity.Name))
                claimsPrincipal.AddIdentity(clientPrincipal.Identity as IClaimsIdentity);
            if (devicePrincipal is IClaimsPrincipal && !claimsPrincipal.Identities.OfType<DeviceIdentity>().Any(o => o.Name == devicePrincipal.Identity.Name))
                claimsPrincipal.AddIdentity(devicePrincipal.Identity as IClaimsIdentity);

            String remoteIp =
                RestOperationContext.Current.IncomingRequest.Headers["X-Forwarded-For"] ??
                RestOperationContext.Current.IncomingRequest.RemoteEndPoint.Address.ToString();

          
            // Establish the session
            ISessionProviderService isp = ApplicationServiceContext.Current.GetService<ISessionProviderService>();
            var scopeList = scope == "*" || String.IsNullOrEmpty(scope) ? null : scope.Split(' ');
            string purposeOfUse = additionalClaims?.FirstOrDefault(o => o.Type == SanteDBClaimTypes.PurposeOfUse)?.Value;
            bool isOverride = additionalClaims?.Any(o => o.Type == SanteDBClaimTypes.SanteDBOverrideClaim) == true || scopeList?.Any(o => o == PermissionPolicyIdentifiers.OverridePolicyPermission) == true;

            var session = isp.Establish(new SanteDBClaimsPrincipal(claimsPrincipal.Identities), remoteIp, isOverride, purposeOfUse, scopeList, additionalClaims.FirstOrDefault(o=>o.Type == SanteDBClaimTypes.Language)?.Value);
            
            string refreshToken = null, sessionId = null;
            if (session != null)
            {
                sessionId = BitConverter.ToString(session.Id).Replace("-", "");
                (claimsPrincipal.Identity as IClaimsIdentity).AddClaim(new SanteDBClaim("jti", sessionId));
                refreshToken = BitConverter.ToString(session.RefreshToken).Replace("-", "");
            }
            if (scope == PermissionPolicyIdentifiers.LoginPasswordOnly)
                refreshToken = String.Empty;
            var jwt = this.HydrateToken(session);

            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            RestOperationContext.Current.OutgoingResponse.ContentType = "application/json";

            OAuthTokenResponse response = null;
            if (this.m_configuration.TokenType == OAuthConstants.BearerTokenType &&
                session != null)
                response = new OAuthTokenResponse()
                {
                    TokenType = OAuthConstants.BearerTokenType,
                    AccessToken = sessionId,
                    IdentityToken = handler.WriteToken(jwt),
                    ExpiresIn = (int)(session.NotAfter.Subtract(DateTime.Now)).TotalMilliseconds,
                    RefreshToken = refreshToken // TODO: Need to write a SessionProvider for this so we can keep track of refresh tokens 
                };
            else
                response = new OAuthTokenResponse()
                {
                    TokenType = OAuthConstants.JwtTokenType,
                    AccessToken = handler.WriteToken(jwt),
                    ExpiresIn = (int)(session.NotAfter.Subtract(DateTime.Now)).TotalMilliseconds,
                    RefreshToken = refreshToken // TODO: Need to write a SessionProvider for this so we can keep track of refresh tokens 
                };

            return response;
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
            new SanteDB.Rest.Common.Security.TokenAuthorizationAccessBehavior().Apply(new RestRequestMessage(RestOperationContext.Current.IncomingRequest)); ;
            var principal = Core.Security.AuthenticationContext.Current.Principal as IClaimsPrincipal;

            if (principal != null)
            {

                if (principal.Identity.Name == Core.Security.AuthenticationContext.AnonymousPrincipal.Identity.Name)
                    return new OAuthError()
                    {
                        Error = OAuthErrorType.invalid_request,
                        ErrorDescription = "No Such Session"
                    };
                else
                {
                    DateTime notBefore = DateTime.Parse(principal.FindFirst(SanteDBClaimTypes.AuthenticationInstant).Value), notAfter = DateTime.Parse(principal.FindFirst(SanteDBClaimTypes.Expiration).Value);

                    var jwt = this.HydrateToken(RestOperationContext.Current.Data[SanteDB.Rest.Common.Security.TokenAuthorizationAccessBehavior.RestPropertyNameSession] as ISession);
                    return new OAuthTokenResponse()
                    {
                        AccessToken = RestOperationContext.Current.IncomingRequest.Headers["Authorization"].Split(' ')[1],
                        IdentityToken = new JwtSecurityTokenHandler().WriteToken(jwt),
                        ExpiresIn = (int)(notAfter).Subtract(DateTime.Now).TotalMilliseconds,
                        TokenType = this.m_configuration.TokenType
                    };
                }
            }
            else
            {
                return new OAuthError()
                {
                    Error = OAuthErrorType.invalid_request,
                    ErrorDescription = "No Such Session"
                };
            }
        }

        /// <summary>
        /// Post the authorization code to the application
        /// </summary>
        /// <param name="authorization">Authorization post results</param>
        public Stream SelfPost(String content, NameValueCollection authorization)
        {
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

            AuditData audit = new AuditData(DateTime.Now, ActionType.Execute, OutcomeIndicator.Success, EventIdentifierType.SecurityAlert, AuditUtil.CreateAuditActionCode(EventTypeCodes.UserAuthentication));
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
                    var response = this.EstablishSession(principal, new SanteDBClaimsPrincipal(clientIdentity), null, String.Join(" ", scope.Where(o=>!o.Equals("openid"))), claimList);
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
                var securityConfiguration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SecurityConfigurationSection>();
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
        /// Get the specified session information
        /// </summary>
        public Stream UserInfo()
        {
            new SanteDB.Rest.Common.Security.TokenAuthorizationAccessBehavior().Apply(new RestRequestMessage(RestOperationContext.Current.IncomingRequest)); ;
            var principal = Core.Security.AuthenticationContext.Current.Principal as IClaimsPrincipal;

            if (principal != null)
            {
                if (principal.Identity.Name == Core.Security.AuthenticationContext.AnonymousPrincipal.Identity.Name)
                    throw new SecurityException("No Such Session");
                else
                {
                    var jwt = this.HydrateToken(RestOperationContext.Current.Data[SanteDB.Rest.Common.Security.TokenAuthorizationAccessBehavior.RestPropertyNameSession] as ISession);
                    return new MemoryStream(Encoding.UTF8.GetBytes(jwt.Payload.SerializeToJson()));
                }
            }
            else
                throw new SecurityException("No Such Session");
        }


    }

}
