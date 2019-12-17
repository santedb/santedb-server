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
 * Date: 2018-11-23
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
using SanteDB.Core.Rest.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
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

        /// <summary>
        /// OAuth token request
        /// </summary>
        public Stream Token(NameValueCollection tokenRequest)
        {

            // Get the client application 
            IApplicationIdentityProviderService clientIdentityService = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>();
            IIdentityProviderService identityProvider = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();

            // Only password grants
            if (tokenRequest["grant_type"] != OAuthConstants.GrantNamePassword &&
                tokenRequest["grant_type"] != OAuthConstants.GrantNameRefresh &&
                tokenRequest["grant_type"] != OAuthConstants.GrantNameAuthorizationCode &&
                tokenRequest["grant_type"] != OAuthConstants.GrantNameClientCredentials)
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
                if (clientPrincipal == null || clientPrincipal == Core.Security.AuthenticationContext.AnonymousPrincipal)
                {
                    String client_identity = tokenRequest["client_id"],
                        client_secret = tokenRequest["client_secret"];
                    if (String.IsNullOrEmpty(client_identity) || String.IsNullOrEmpty(client_secret))
                        return this.CreateErrorCondition(OAuthErrorType.invalid_client, "Missing client credentials");

                    try
                    {
                        clientPrincipal = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>().Authenticate(client_identity, client_secret);
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


                IPrincipal principal = null;

                var clientClaims = SanteDBClaimsUtil.ExtractClaims(RestOperationContext.Current.IncomingRequest.Headers);

                // perform auth
                switch (tokenRequest["grant_type"])
                {
                    case OAuthConstants.GrantNameClientCredentials:
                        new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, OAuth2.OAuthConstants.OAuthClientCredentialFlowPolicy, clientPrincipal).Demand();
                        if (devicePrincipal != null)
                            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, OAuth2.OAuthConstants.OAuthPasswordFlowPolicy, devicePrincipal).Demand();

                        if (devicePrincipal == null)
                            throw new SecurityException("client_credentials grant requires device authentication either using X509 or X-Device-Authorization");
                        principal = clientPrincipal;
                        // Demand "Login As Service" permission
                        new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.LoginAsService, devicePrincipal).Demand();
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
                        var token = Convert.FromBase64String(tokenRequest["code"].Replace(" ", "+"));

                        // First we extract the token information
                        var sid = new Guid(Enumerable.Range(0, 32).Where(x => x % 2 == 0).Select(o => token[o]).ToArray());
                        var aid = new Guid(Enumerable.Range(32, 32).Where(x => x % 2 == 0).Select(o => token[o]).ToArray());
                        var scopeLength = BitConverter.ToInt32(token, 64);
                        var scopeData = Enumerable.Range(68, scopeLength * 2).Where(x => x % 2 == 0).Select(o => token[o]).ToArray();
                        var claimLength = BitConverter.ToInt32(token, 68 + scopeLength * 2);
                        var claimData = Enumerable.Range(72 + scopeLength * 2, claimLength * 2).Where(x => x % 2 == 0).Select(o => token[o]).ToArray();
                        var dsig = token.Skip(72 + 2 * (scopeLength + claimLength)).Take(32).ToArray();
                        var expiry = new DateTime(BitConverter.ToInt64(token, 104 + 2* (scopeLength + claimLength)));

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
                        principal = new SanteDBClaimsPrincipal(identityProvider.GetIdentity(sid));

                        // Add scopes
                        int li = 0, idx;
                        string scopes = "";
                        do
                        {
                            idx = Array.IndexOf(scopeData, (byte)0, li);
                            if(idx > 0) scopes += Encoding.UTF8.GetString(scopeData, li, idx - li) + ";";
                            li += idx + 1;
                        } while (idx > -1);
                        tokenRequest["scope"] = scopes.Substring(0, scopes.Length - 1);

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
            IPolicyDecisionService pdp = ApplicationServiceContext.Current.GetService<IPolicyDecisionService>();

            List<IClaim> retVal = new List<IClaim>();

            // HACK: Find a better way to make claims
            // Claims are stored as X-SanteDBACS-Claim headers
            foreach (var itm in claims)
            {

                // Claim allowed
                if (this.m_configuration.AllowedClientClaims == null ||
                    !this.m_configuration.AllowedClientClaims.Contains(itm.Type))
                    throw new SecurityException("Claim is not permitted");
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
        private JwtSecurityToken HydrateToken(IClaimsPrincipal claimsPrincipal, String scope, IEnumerable<IClaim> additionalClaims, DateTime issued, DateTime expires)
        {
            this.m_traceSource.TraceInfo("Will create new ClaimsPrincipal based on existing principal");

            IRoleProviderService roleProvider = ApplicationServiceContext.Current.GetService<IRoleProviderService>();
            IPolicyInformationService pip = ApplicationServiceContext.Current.GetService<IPolicyInformationService>();

            // System claims
            List<IClaim> claims = new List<IClaim>()
            {
                new SanteDBClaim("iss", this.m_configuration.IssuerName),
                new SanteDBClaim(SanteDBClaimTypes.Name, claimsPrincipal.Identity.Name),
                new SanteDBClaim("typ", claimsPrincipal.GetType().Name)
            };

            try
            {
                claims.AddRange(roleProvider.GetAllRoles(claimsPrincipal.Identity.Name).Select(r => new SanteDBClaim(SanteDBClaimTypes.DefaultRoleClaimType, r)));
            }
            catch { }

            // Additional claims
            claims.AddRange(additionalClaims ?? new List<IClaim>());

            // Get policies
            List<IPolicyInstance> oizPrincipalPolicies = new List<IPolicyInstance>();
            foreach (var pol in pip.GetActivePolicies(claimsPrincipal).GroupBy(o => o.Policy.Oid))
                oizPrincipalPolicies.Add(pol.FirstOrDefault(o => (int)o.Rule == pol.Min(r => (int)r.Rule)));

            // Scopes user is allowed to access
            claims.AddRange(oizPrincipalPolicies.Where(o => o.Rule == PolicyGrantType.Grant).Select(o => new SanteDBClaim(SanteDBClaimTypes.SanteDBScopeClaim, o.Policy.Oid)));

            // Add grant if not exists
            if ((claimsPrincipal)?.FindFirst(SanteDBClaimTypes.Actor)?.Value == UserClassKeys.HumanUser.ToString())
            {
                claims.AddRange(new IClaim[]
                    {
                    //new SanteDBClaim(ClaimTypes.AuthenticationInstant, issued.ToString("o")), 
                    new SanteDBClaim(SanteDBClaimTypes.AuthenticationMethod, "OAuth2"),
                    new SanteDBClaim(SanteDBClaimTypes.SanteDBApplicationIdentifierClaim, claimsPrincipal?.Identities.OfType<Core.Security.ApplicationIdentity>().FirstOrDefault()?.FindFirst(SanteDBClaimTypes.Sid)?.Value)
                    });

                if (claimsPrincipal.Identities.OfType<DeviceIdentity>().Any())
                    claims.Add(new SanteDBClaim(SanteDBClaimTypes.SanteDBDeviceIdentifierClaim, claimsPrincipal.Identities.OfType<DeviceIdentity>().FirstOrDefault().FindFirst(SanteDBClaimTypes.Sid)?.Value));


                // Is the user elevated? If so, add claims for those policies in scope
                if (claims.FirstOrDefault(o => o.Type == SanteDBClaimTypes.SanteDBOverrideClaim)?.Value?.ToLower() == "true")
                {
                    try
                    {
                        // 1. SCOPE must exist 
                        if (String.IsNullOrEmpty(scope))
                            throw new InvalidOperationException("Override requires scope");
                        // 2. POU must exist
                        if (!claims.Exists(c => c.Type == SanteDBClaimTypes.XspaPurposeOfUseClaim))
                            throw new InvalidOperationException("Override required purpose of use");
                        // 3. Person must have override permission
                        new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.OverridePolicyPermission, claimsPrincipal).Demand();
                        // Add elevation objects as GRANT to this session
                        claims.AddRange(oizPrincipalPolicies.Where(o => o.Rule == PolicyGrantType.Elevate).Select(o => new SanteDBClaim(SanteDBClaimTypes.SanteDBScopeClaim, o.Policy.Oid)));

                        // Audit override
                        AuditUtil.AuditOverride(claimsPrincipal, claims.Where(c => c.Type == SanteDBClaimTypes.XspaPurposeOfUseClaim).FirstOrDefault().Value, scope.Split(';'), true);
                    }
                    catch (Exception e)
                    {
                        AuditUtil.AuditOverride(claimsPrincipal, claims.Where(c => c.Type == SanteDBClaimTypes.XspaPurposeOfUseClaim).FirstOrDefault().Value, scope.Split(';'), false);
                        throw;
                    }
                }

                // Now restrict down to claimed scope
                if (scope != "*")
                {
                    var scopes = scope.Split(';');
                    foreach (var s in scopes)
                        new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, s, claimsPrincipal).Demand(); // Ensure that scope is granted
                    claims.RemoveAll(o => o.Type == SanteDBClaimTypes.SanteDBScopeClaim && !scopes.Contains(o.Value));
                }

                // Add Email address from idp
                claims.AddRange(claimsPrincipal.Claims.Where(o => o.Type == SanteDBClaimTypes.Email));
                var tel = claimsPrincipal.Claims.FirstOrDefault(o => o.Type == SanteDBClaimTypes.Telephone)?.Value;
                if (!String.IsNullOrEmpty(tel))
                    claims.Add(new SanteDBClaim("tel", tel));
            }
            else
            {
                claims.AddRange(new IClaim[]
                   {
                    //new SanteDBClaim(ClaimTypes.AuthenticationInstant, issued.ToString("o")), 
                    new SanteDBClaim(SanteDBClaimTypes.AuthenticationMethod, "OAuth2"),
                    new SanteDBClaim(SanteDBClaimTypes.SanteDBApplicationIdentifierClaim, claimsPrincipal.Identities.OfType<Core.Security.ApplicationIdentity>().FirstOrDefault()?.FindFirst(SanteDBClaimTypes.Sid).Value),
                    new SanteDBClaim(SanteDBClaimTypes.SanteDBDeviceIdentifierClaim, claimsPrincipal.Identities.OfType<Core.Security.DeviceIdentity>().FirstOrDefault()?.FindFirst(SanteDBClaimTypes.Sid)?.Value)
                   });
            }

            // Add audience claim bsaed on application identity
            claims.Add(new SanteDBClaim("aud", claimsPrincipal.Identities.OfType<Core.Security.ApplicationIdentity>().First().Name));

            // Name identifier
            claims.AddRange((claimsPrincipal).Claims.Where(o => o.Type == SanteDBClaimTypes.NameIdentifier));

            // Find the nameid
            var nameId = claims.Find(o => o.Type == SanteDBClaimTypes.NameIdentifier);
            if (nameId != null)
            {
                claims.Remove(nameId);
                claims.Add(new SanteDBClaim("sub", nameId.Value));
            }

            claims.RemoveAll(o => String.IsNullOrEmpty(o.Value));

            SigningCredentials credentials = SecurityUtils.CreateSigningCredentials(this.m_masterConfig.Signatures.FirstOrDefault());

            // Generate security token            
            var jwt = new JwtSecurityToken(
                signingCredentials: credentials,
                claims: claims.Select(o => new System.Security.Claims.Claim(o.Type, o.Value)),
                issuer: this.m_configuration.IssuerName,
                notBefore: issued,
                expires: expires
            );

            return jwt;

        }

        /// <summary>
        /// Create a token response
        /// </summary>
        private Stream EstablishSession(IPrincipal oizPrincipal, IPrincipal clientPrincipal, IPrincipal devicePrincipal, String scope, IEnumerable<IClaim> additionalClaims)
        {
            var claimsPrincipal = oizPrincipal as IClaimsPrincipal;
            if (clientPrincipal is IClaimsPrincipal && !claimsPrincipal.Identities.OfType<Core.Security.ApplicationIdentity>().Any(o => o.Name == clientPrincipal.Identity.Name))
                claimsPrincipal.AddIdentity(clientPrincipal.Identity as IClaimsIdentity);
            if (devicePrincipal is IClaimsPrincipal && !claimsPrincipal.Identities.OfType<DeviceIdentity>().Any(o => o.Name == devicePrincipal.Identity.Name))
                claimsPrincipal.AddIdentity(devicePrincipal.Identity as IClaimsIdentity);

            // TODO: Add configuration for expiry
            DateTime issued = DateTime.Parse((claimsPrincipal)?.FindFirst(SanteDBClaimTypes.AuthenticationInstant)?.Value ?? DateTime.Now.ToString("o")),
                expires = DateTime.Now.Add(this.m_configuration.ValidityTime);
            String remoteIp =
                RestOperationContext.Current.IncomingRequest.Headers["X-Forwarded-For"] ??
                RestOperationContext.Current.IncomingRequest.RemoteEndPoint.Address.ToString();

            // Establish the session
            ISessionProviderService isp = ApplicationServiceContext.Current.GetService<ISessionProviderService>();
            var session = isp.Establish(new SanteDBClaimsPrincipal(claimsPrincipal.Identities), expires, remoteIp);

            string refreshToken = null, sessionId = null;
            if (session != null)
            {
                sessionId = BitConverter.ToString(session.Id).Replace("-", "");
                (claimsPrincipal.Identity as IClaimsIdentity).AddClaim(new SanteDBClaim("jti", sessionId));
                refreshToken = BitConverter.ToString(session.RefreshToken).Replace("-", "");
            }

            var jwt = this.HydrateToken(claimsPrincipal, scope, additionalClaims, issued, expires);

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
                    ExpiresIn = (int)(expires.Subtract(DateTime.Now)).TotalMilliseconds,
                    RefreshToken = refreshToken // TODO: Need to write a SessionProvider for this so we can keep track of refresh tokens 
                };
            else
                response = new OAuthTokenResponse()
                {
                    TokenType = OAuthConstants.JwtTokenType,
                    AccessToken = handler.WriteToken(jwt),
                    ExpiresIn = (int)(expires.Subtract(DateTime.Now)).TotalMilliseconds,
                    RefreshToken = refreshToken // TODO: Need to write a SessionProvider for this so we can keep track of refresh tokens 
                };

            return this.CreateResponse(response);
        }

        /// <summary>
        /// Create error condition
        /// </summary>
        private Stream CreateErrorCondition(OAuthErrorType errorType, String message)
        {
            this.m_traceSource.TraceEvent(EventLevel.Error, message);
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
            OAuthError err = new OAuthError()
            {
                Error = errorType,
                ErrorDescription = message
            };
            return this.CreateResponse(err);
        }

        /// <summary>
        /// Create response
        /// </summary>
        private Stream CreateResponse(Object response)
        {
            var settings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            settings.Converters.Add(new StringEnumConverter());
            String result = JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented, settings);
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(result));
            RestOperationContext.Current.OutgoingResponse.ContentType = "application/json";
            return ms;
        }

        /// <summary>
        /// Get the specified session information
        /// </summary>
        public Stream Session()
        {
            new TokenAuthorizationAccessBehavior().Apply(new RestRequestMessage(RestOperationContext.Current.IncomingRequest)); ;
            var principal = Core.Security.AuthenticationContext.Current.Principal as IClaimsPrincipal;

            if (principal != null)
            {

                if (principal.Identity.Name == Core.Security.AuthenticationContext.AnonymousPrincipal.Identity.Name)
                    return this.CreateResponse(new OAuthError()
                    {
                        Error = OAuthErrorType.invalid_request,
                        ErrorDescription = "No Such Session"
                    });
                else
                {
                    DateTime notBefore = DateTime.Parse(principal.FindFirst(SanteDBClaimTypes.AuthenticationInstant).Value), notAfter = DateTime.Parse(principal.FindFirst(SanteDBClaimTypes.Expiration).Value);
                    var jwt = this.HydrateToken(principal, principal.FindFirst(SanteDBClaimTypes.SanteDBScopeClaim)?.Value ?? "*", null, notBefore, notAfter);
                    return this.CreateResponse(new OAuthTokenResponse()
                    {
                        AccessToken = RestOperationContext.Current.IncomingRequest.Headers["Authorization"].Split(' ')[1],
                        IdentityToken = new JwtSecurityTokenHandler().WriteToken(jwt),
                        ExpiresIn = (int)(notAfter).Subtract(DateTime.Now).TotalMilliseconds,
                        TokenType = this.m_configuration.TokenType
                    });
                }
            }
            else
            {
                return this.CreateResponse(new OAuthError()
                {
                    Error = OAuthErrorType.invalid_request,
                    ErrorDescription = "No Such Session"
                });
            }
        }

        /// <summary>
        /// Post the authorization code to the application
        /// </summary>
        /// <param name="authorization">Authorization post results</param>
        public void SelfPost(NameValueCollection authorization)
        {
            // Generate an authorization code for this user and redirect to their redirect URL
            var redirectUrl = RestOperationContext.Current.IncomingRequest.QueryString["redirect_uri"] ?? authorization["redirect_uri"];
            var client_id = RestOperationContext.Current.IncomingRequest.QueryString["client_id"] ?? authorization["client_id"];
            var scope = RestOperationContext.Current.IncomingRequest.QueryString.GetValues("scope") ?? authorization.GetValues("scope")?.SelectMany(o=>o.Split(';'));
            var claims = RestOperationContext.Current.IncomingRequest.QueryString.GetValues("claim") ?? authorization.GetValues("claim")?.SelectMany(o=>o.Split(';'));
            var state = RestOperationContext.Current.IncomingRequest.QueryString["state"] ?? authorization["state"];
            var signature = RestOperationContext.Current.IncomingRequest.QueryString["dsig"]  ?? authorization["dsig"];

            AuditData audit = new AuditData(DateTime.Now, ActionType.Execute, OutcomeIndicator.Success, EventIdentifierType.SecurityAlert, AuditUtil.CreateAuditActionCode(EventTypeCodes.UserAuthentication));
            AuditUtil.AddLocalDeviceActor(audit);
            AuditUtil.AddRemoteDeviceActor(audit);

            try
            {
                if (signature == null)
                    throw new ArgumentException("Must provide the dsig parameter");
                if(client_id == null)
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
                var scopeData = scope.Where(o => o != "openid").SelectMany(o => Encoding.UTF8.GetBytes(o).Concat(new byte[] { 0 })).ToArray();
                var claimData = claims?.SelectMany(o => Encoding.UTF8.GetBytes(o).Union(new byte[] { 0 })).ToArray() ?? new byte[0];
                // Generate the appropriate authorization code
                byte[] salt = Guid.NewGuid().ToByteArray();

                // Generate the token 
                // Token format is :
                // SID w/SALT . AID w/SALT . SIG
                byte[] authCode = new byte[112 + scopeData.Length * 2 + claimData.Length * 2];
                var i = 0;
                foreach (var b in sid.ToByteArray()) { authCode[i++] = b; authCode[i++] = salt[i % salt.Length]; }
                foreach (var b in aid.ToByteArray()) { authCode[i++] = b; authCode[i++] = salt[i % salt.Length]; }
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
                var tokenString = Convert.ToBase64String(authCode);

                // Redirect
                RestOperationContext.Current.OutgoingResponse.Redirect($"{redirectUrl}?code={tokenString}&state={state}");
                return;
            }
            catch (Exception e)
            {
                audit.Outcome = OutcomeIndicator.SeriousFail;
                this.m_traceSource.TraceError("Could not create authentication token: {0}", e.Message);
                RestOperationContext.Current.OutgoingResponse.Redirect($"{redirectUrl}?error=invalid_request&error_description={e.Message}&state={state}");
            }
            finally
            {
                AuditUtil.SendAudit(audit);
            }

        }

        /// <summary>
        /// Render the specified login asset
        /// </summary>
        /// <returns>A stream of the rendered login asset</returns>
        public Stream RenderAsset(string content)
        {
            // Get the asset object
            var loadedApplets = ApplicationServiceContext.Current.GetService<IAppletManagerService>().Applets;


            var lander = RestOperationContext.Current.IncomingRequest.QueryString["lander"];
            var client = RestOperationContext.Current.IncomingRequest.QueryString["client_id"];
            var redirectUri = RestOperationContext.Current.IncomingRequest.QueryString["redirect_uri"];

            var loginApplet = loadedApplets.Where(o => o.Configuration?.AppSettings.Any(s => s.Name == "oauth2.login") == true && (lander == o.Info.Id || String.IsNullOrEmpty(lander))).FirstOrDefault();
            if (loginApplet == null)
                throw new KeyNotFoundException("No asset has been configured as oauth2.login.asset");

            var loginAssetPath = loginApplet.Configuration.AppSettings.FirstOrDefault(o => o.Name == "oauth2.login")?.Value;

            if (String.IsNullOrEmpty(content))
                content = "index.html";

            var assetName = loginAssetPath + content;

            var asset = loadedApplets.ResolveAsset(assetName);
            if (asset == null)
                throw new KeyNotFoundException($"{assetName} not found");

            // Now time to resolve the asset
            var bindingParms = RestOperationContext.Current.IncomingRequest.QueryString.AllKeys.ToDictionary(o => o, o => String.Join(";", RestOperationContext.Current.IncomingRequest.QueryString.GetValues(o)));
            if (String.IsNullOrEmpty(content) || content == "index.html")
            {

                // Rule: scope, response_type and client_id and redirect_uri must be provided
                if (String.IsNullOrEmpty(redirectUri) || 
                    String.IsNullOrEmpty(client) ||
                    RestOperationContext.Current.IncomingRequest.QueryString.GetValues("scope")?.Contains("openid") != true ||
                    RestOperationContext.Current.IncomingRequest.QueryString["response_type"] != "code")
                    throw new InvalidOperationException("OpenID Violation: redirect_uri and client_id must be provided");
                else
                {
                    var applicationId = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>().GetIdentity(client);
                    if (applicationId == null)
                        throw new SecurityException($"Client {client} is not registered with this provider");
                    var pdp = ApplicationServiceContext.Current.GetService<IPolicyDecisionService>();
                    if(pdp.GetPolicyOutcome(new SanteDBClaimsPrincipal(applicationId as IClaimsIdentity), OAuthConstants.OAuthCodeFlowPolicy) != PolicyGrantType.Grant)
                        throw new SecurityException($"Client {client} is not permitted to execute this authorization grant flow");
                    // TODO: Get claim for application redirect URL

                    var signature = ApplicationServiceContext.Current.GetService<IDataSigningService>().SignData(Encoding.UTF8.GetBytes(client + redirectUri));
                    bindingParms.Add("dsig", BitConverter.ToString(signature));

                }
            }

            RestOperationContext.Current.OutgoingResponse.ContentType = DefaultContentTypeMapper.GetContentType(Path.GetExtension(content));
            return new MemoryStream(loadedApplets.RenderAssetContent(asset, RestOperationContext.Current.IncomingRequest.QueryString["ui_locale"] ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, bindingParameters: bindingParms));
        }

        /// <summary>
        /// Perform a ping
        /// </summary>
        public void Ping()
        {
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.NoContent;
        }

    }
}
