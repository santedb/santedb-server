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
 * User: fyfej
 * Date: 2017-9-1
 */
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Exceptions;
using MARC.HI.EHRS.SVC.Core.Services;
using SanteDB.Core.Configuration;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IdentityModel.Configuration;
using System.IdentityModel.Policy;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Wcf.Security
{
    /// <summary>
    /// JwtToken SAM
    /// </summary>
    public class TokenServiceAuthorizationManager : ServiceAuthorizationManager
    {

        // Configuration from main SanteDB
        private SanteDBConfiguration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection(SanteDBConstants.SanteDBConfigurationName) as SanteDBConfiguration;

        // Trace source
        private TraceSource m_traceSource = new TraceSource(SanteDBConstants.SecurityTraceSourceName);

        /// <summary>
        /// Check access core
        /// </summary>
        protected override bool CheckAccessCore(OperationContext operationContext)
        {
            this.m_traceSource.TraceInformation("CheckAccessCore");
            this.m_traceSource.TraceInformation("User {0} already authenticated", Core.Security.AuthenticationContext.Current.Principal.Identity.Name);
            return base.CheckAccessCore(operationContext);
        }

        /// <summary>
        /// Check access
        /// </summary>
        public override bool CheckAccess(OperationContext operationContext)
        {
            RemoteEndpointMessageProperty remoteEndpoint = (RemoteEndpointMessageProperty)operationContext.IncomingMessageProperties[RemoteEndpointMessageProperty.Name];

            try
            {
                this.m_traceSource.TraceInformation("CheckAccess");

                // Http message inbound
                HttpRequestMessageProperty httpMessage = (HttpRequestMessageProperty)operationContext.IncomingMessageProperties[HttpRequestMessageProperty.Name];

                // Get the authorize header
                String authorization = httpMessage.Headers[System.Net.HttpRequestHeader.Authorization];
                if (authorization == null)
                {
                    if (httpMessage.Method == "OPTIONS" || httpMessage.Method == "PING")
                    {
                        //operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Identities"] = identities;
                        operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Principal"] = Core.Security.AuthenticationContext.AnonymousPrincipal;
                        Core.Security.AuthenticationContext.Current = new Core.Security.AuthenticationContext(Core.Security.AuthenticationContext.AnonymousPrincipal);

                        return true; // OPTIONS is non PHI infrastructural
                    }
                    else
                    {
                        throw new UnauthorizedRequestException("Missing Authorization header", "Bearer", this.m_configuration.Security.ClaimsAuth.Realm, this.m_configuration.Security.ClaimsAuth.Audiences.FirstOrDefault());
                    }
                }

                // Authorization method
                var auth = authorization.Split(' ').Select(o=>o.Trim()).ToArray();
                switch(auth[0].ToLowerInvariant())
                {
                    case "bearer":
                        this.CheckBearerAccess(operationContext, auth[1]);
                        break;
                    case "urn:ietf:params:oauth:token-type:jwt": // Will use JWT authorization
                        this.CheckJwtAccess(operationContext, auth[1]);
                        break;
                    default:
                        throw new UnauthorizedRequestException("Invalid authentication scheme", "Bearer", this.m_configuration.Security.ClaimsAuth.Realm, this.m_configuration.Security.ClaimsAuth.Audiences.FirstOrDefault());
                }

                if (OperationContext.Current != operationContext)
                    return base.CheckAccess(operationContext);
                else
                    return true;
            }
            catch(UnauthorizedAccessException e) {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "JWT Token Error (From: {0}) : {1}", remoteEndpoint?.Address, e);

                throw;
            }
            catch(UnauthorizedRequestException e) {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "JWT Token Error (From: {0}) : {1}", remoteEndpoint?.Address, e);

                throw;
            }
            catch(Exception e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "JWT Token Error (From: {0}) : {1}", remoteEndpoint?.Address, e);
                throw new SecurityTokenException(e.Message, e);
            }
        }

        /// <summary>
        /// Checks bearer access token
        /// </summary>
        /// <param name="operationContext">The operation context within which the access token should be validated</param>
        /// <param name="authorization">The authorization data </param>
        /// <returns>True if authorization is successful</returns>
        private void CheckBearerAccess(OperationContext operationContext, string authorization)
        {
            var session = ApplicationContext.Current.GetService<ISessionProviderService>().Get(
                Enumerable.Range(0, authorization.Length)
                                    .Where(x => x % 2 == 0)
                                    .Select(x => Convert.ToByte(authorization.Substring(x, 2), 16))
                                    .ToArray()
            );

            IPrincipal principal = ApplicationContext.Current.GetService<ISessionIdentityProviderService>().Authenticate(session);

            operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Identities"] = (principal as ClaimsPrincipal).Identities;
            operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Principal"] = principal;
            operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Session"] = session;
            Core.Security.AuthenticationContext.Current = new Core.Security.AuthenticationContext(principal);

            this.m_traceSource.TraceInformation("User {0} authenticated via SESSION BEARER", principal.Identity.Name);

        }

        /// <summary>
        /// Validates the authorization header as a JWT token
        /// </summary>
        /// <param name="operationContext">The operation context within which this should be checked</param>
        /// <param name="authorization">The authorization data</param>
        /// <returns>True when authorization is successful</returns>
        private void CheckJwtAccess(OperationContext operationContext, string authorization)
        {
            authorization = authorization.Trim();
            String authorizationToken = authorization.Substring(authorization.IndexOf(" ")).Trim();
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

            operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Identities"] = identities.Identities;
            operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Principal"] = identities;
            operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Session"] = token;

            Core.Security.AuthenticationContext.Current = new Core.Security.AuthenticationContext(identities);

            this.m_traceSource.TraceInformation("User {0} authenticated via JWT", identities.Identity.Name);

        }
    }
}
