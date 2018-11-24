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
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Exceptions;
using MARC.HI.EHRS.SVC.Core.Services;
using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core.Configuration;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IdentityModel.Configuration;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Rest.Security
{
    /// <summary>
    /// JwtToken SAM
    /// </summary>
    public class TokenAuthorizationAccessBehavior : IServicePolicy, IServiceBehavior
    {

        // Configuration from main SanteDB
        private SanteDBConfiguration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection(SanteDBConstants.SanteDBConfigurationName) as SanteDBConfiguration;

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
            var session = ApplicationContext.Current.GetService<ISessionProviderService>().Get(
                Enumerable.Range(0, authorizationToken.Length)
                                    .Where(x => x % 2 == 0)
                                    .Select(x => Convert.ToByte(authorizationToken.Substring(x, 2), 16))
                                    .ToArray()
            );

            IPrincipal principal = ApplicationContext.Current.GetService<ISessionIdentityProviderService>().Authenticate(session);
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

            var identityModelConfig = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection("system.identityModel") as SystemIdentityModelSection;

            if (!handler.CanReadToken(authorizationToken))
                throw new SecurityTokenException("Token is not in a valid format");

            SecurityToken token = null;
            var identities = handler.ValidateToken(authorizationToken, this.m_configuration?.Security?.ClaimsAuth?.ToConfigurationObject(), out token);

            // Validate token expiry
            if (token.ValidTo < DateTime.Now.ToUniversalTime())
                throw new SecurityTokenException("Token expired");
            else if (token.ValidFrom > DateTime.Now.ToUniversalTime())
                throw new SecurityTokenException("Token not yet valid");

            Core.Security.AuthenticationContext.Current = new Core.Security.AuthenticationContext(identities);

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
                        throw new UnauthorizedRequestException("Missing Authorization header", "Bearer", this.m_configuration.Security.ClaimsAuth.Realm, PermissionPolicyIdentifiers.Login);
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
                        throw new UnauthorizedRequestException("Invalid authentication scheme", "Bearer", this.m_configuration.Security.ClaimsAuth.Realm, this.m_configuration.Security.ClaimsAuth.Audiences.FirstOrDefault());
                }

            }
            catch (UnauthorizedAccessException e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "Token Error (From: {0}) : {1}", RestOperationContext.Current.IncomingRequest.RemoteEndPoint, e);

                throw;
            }
            catch (UnauthorizedRequestException e)
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
                RestOperationContext.Current.Disposed += (o, e) => SanteDB.Core.Security.AuthenticationContext.Current = new SanteDB.Core.Security.AuthenticationContext(SanteDB.Core.Security.AuthenticationContext.AnonymousPrincipal);
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
