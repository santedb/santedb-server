/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core.Configuration;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Services;
using System;
using System.Diagnostics;
using System.IdentityModel.Configuration;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Principal;

namespace SanteDB.Core.Rest.Security
{
    /// <summary>
    /// JwtToken SAM
    /// </summary>
    public class TokenAuthorizationAccessBehavior : IServicePolicy, IServiceBehavior
    {

        // Configuration from main SanteDB
        private ClaimsAuthorizationConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<ClaimsAuthorizationConfigurationSection>();

        // Trace source
        private TraceSource m_traceSource = new TraceSource(SanteDBConstants.SecurityTraceSourceName);

        /// <summary>
        /// Checks bearer access token
        /// </summary>
        /// <param name="operationContext">The operation context within which the access token should be validated</param>
        /// <param name="authorization">The authorization data </param>
        /// <returns>True if authorization is successful</returns>
        private void CheckBearerAccess(string authorizationToken)
        {
            var session = ApplicationServiceContext.Current.GetService<ISessionProviderService>().Get(
                Enumerable.Range(0, authorizationToken.Length)
                                    .Where(x => x % 2 == 0)
                                    .Select(x => Convert.ToByte(authorizationToken.Substring(x, 2), 16))
                                    .ToArray()
            );

            IPrincipal principal = ApplicationServiceContext.Current.GetService<ISessionIdentityProviderService>().Authenticate(session);
            if (principal == null)
                throw new SecurityTokenException("Invalid bearer token") ;

            Core.Security.AuthenticationContext.Current = new Core.Security.AuthenticationContext(principal);

            this.m_traceSource.TraceInformation("User {0} authenticated via SESSION BEARER", principal.Identity.Name);
        }

        /// <summary>
        /// Validates the authorization header as a JWT token
        /// </summary>
        /// <param name="operationContext">The operation context within which this should be checked</param>
        /// <param name="authorization">The authorization data</param>
        /// <returns>True when authorization is successful</returns>
        private void CheckJwtAccess(string authorizationToken)
        {
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(authorizationToken))
                throw new SecurityTokenException("Token is not in a valid format");

            SecurityToken token = null;
            var identities = handler.ValidateToken(authorizationToken, this.m_configuration?.ToConfigurationObject(), out token);

            // Validate token expiry
            if (token.ValidTo < DateTime.Now.ToUniversalTime())
                throw new SecurityTokenException("Token expired");
            else if (token.ValidFrom > DateTime.Now.ToUniversalTime())
                throw new SecurityTokenException("Token not yet valid");

            // Copy to a SanteDBClaimsId
            Core.Security.AuthenticationContext.Current = new Core.Security.AuthenticationContext(new SanteDBClaimsPrincipal(
                new SanteDBClaimsIdentity(identities.Identity.Name, identities.Identity.IsAuthenticated, identities.Identity.AuthenticationType, identities.Claims.Select(o=>new SanteDBClaim(o.Type, o.Value)))
            ));

            this.m_traceSource.TraceInformation("User {0} authenticated via JWT", identities.Identity.Name);
            
        }

        /// <summary>
        /// Apply the authorization policy rule
        /// </summary>
        public void Apply(RestRequestMessage request)
        {
            try
            {
                this.m_traceSource.TraceInformation("CheckAccess");

                // Http message inbound
                var httpMessage = RestOperationContext.Current.IncomingRequest;

                // Get the authorize header
                String authorization = httpMessage.Headers["Authorization"];
                if (authorization == null)
                {
                    if (httpMessage.HttpMethod == "OPTIONS" || httpMessage.HttpMethod == "PING")
                    {
                        Core.Security.AuthenticationContext.Current = new Core.Security.AuthenticationContext(Core.Security.AuthenticationContext.AnonymousPrincipal);
                        return;
                    }
                    else
                        throw new SecurityTokenException("Missing Authorization header");
                }

                // Authorization method
                var auth = authorization.Split(' ').Select(o => o.Trim()).ToArray();
                switch (auth[0].ToLowerInvariant())
                {
                    case "bearer":
                        this.CheckBearerAccess(auth[1]);
                        break;
                    case "urn:ietf:params:oauth:token-type:jwt": // Will use JWT authorization
                        this.CheckJwtAccess(auth[1]);
                        break;
                    default:
                        throw new SecurityTokenException("Invalid authentication scheme");
                }

            }
            catch (UnauthorizedAccessException e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "Token Error (From: {0}) : {1}", RestOperationContext.Current.IncomingRequest.RemoteEndPoint, e);

                throw;
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "Token Error (From: {0}) : {1}", RestOperationContext.Current.IncomingRequest.RemoteEndPoint, e);
                throw new SecurityTokenException(e.Message, e);
            }
            finally
            {
                // Disposed context so reset the auth
                RestOperationContext.Current.Disposed += (o, e) => Core.Security.AuthenticationContext.Current = new Core.Security.AuthenticationContext(Core.Security.AuthenticationContext.AnonymousPrincipal);
            }
        }
        /// <summary>
        /// Apply the service behavior
        /// </summary>
        public void ApplyServiceBehavior(RestService service, ServiceDispatcher dispatcher)
        {
            dispatcher.AddServiceDispatcherPolicy(this);
        }
    }
}
