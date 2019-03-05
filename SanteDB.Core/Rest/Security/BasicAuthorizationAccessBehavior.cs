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
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Security.Authentication;

using System.Text;

namespace SanteDB.Core.Rest.Security
{
    /// <summary>
    /// Basic authorization policy
    /// </summary>
    public class BasicAuthorizationAccessBehavior : IServicePolicy, IServiceBehavior
    {

        // Configuration from main SanteDB
        private BasicAuthorizationConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<BasicAuthorizationConfigurationSection>();

        // Trace source
        private TraceSource m_traceSource = new TraceSource(SanteDBConstants.SecurityTraceSourceName);

        /// <summary>
        /// Apply the policy to the request
        /// </summary>
        public void Apply(RestRequestMessage request)
        {
            try
            {
                this.m_traceSource.TraceInformation("Entering BasicAuthorizationAccessPolicy");

                // Role service
                var roleService = ApplicationServiceContext.Current.GetService<IRoleProviderService>();
                var identityService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
                var pipService = ApplicationServiceContext.Current.GetService<IPolicyInformationService>();

                var httpRequest = RestOperationContext.Current.IncomingRequest;

                var authHeader = httpRequest.Headers["Authorization"];
                if (String.IsNullOrEmpty(authHeader) ||
                    !authHeader.ToLowerInvariant().StartsWith("basic"))
                    throw new AuthenticationException("Invalid authentication scheme");
                authHeader = authHeader.Substring(6);
                var b64Data = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader)).Split(':');
                if (b64Data.Length != 2)
                    throw new SecurityException("Malformed HTTP Basic Header");

                var principal = identityService.Authenticate(b64Data[0], b64Data[1]);
                if (principal == null)
                    throw new AuthenticationException("Invalid username/password");

                // Add claims made by the client
                var claims = new List<IClaim>();
                if (principal is IClaimsPrincipal)
                    claims.AddRange((principal as IClaimsPrincipal).Claims);

                var clientClaims = SanteDBClaimsUtil.ExtractClaims(httpRequest.Headers);
                foreach (var claim in clientClaims)
                {
                    if (this.m_configuration?.AllowedClientClaims?.Contains(claim.Type) == false)
                        throw new SecurityException("Claim not allowed");
                    else
                    {
                        var handler = SanteDBClaimsUtil.GetHandler(claim.Type);
                        if (handler == null ||
                            handler.Validate(principal, claim.Value))
                            claims.Add(claim);
                        else
                            throw new SecurityException("Claim validation failed");
                    }
                }

                // Claim headers built in
                if (pipService != null)
                    claims.AddRange(pipService.GetActivePolicies(principal).Where(o => o.Rule == PolicyGrantType.Grant).Select(o => new SanteDBClaim(SanteDBClaimTypes.SanteDBGrantedPolicyClaim, o.Policy.Oid)));

                // Finally validate the client 
                var claimsPrincipal = new SanteDBClaimsPrincipal(new SanteDBClaimsIdentity(principal.Identity, claims));

                if (this.m_configuration?.RequireClientAuth == true)
                {
                    var clientAuth = httpRequest.Headers[SanteDBConstants.BasicHttpClientCredentialHeaderName];
                    if (clientAuth == null ||
                        !clientAuth.StartsWith("basic", StringComparison.InvariantCultureIgnoreCase))
                        throw new SecurityException("Client credentials invalid");
                    else
                    {
                        String clientAuthString = clientAuth.Substring(clientAuth.IndexOf("basic", StringComparison.InvariantCultureIgnoreCase) + 5).Trim();
                        String[] authComps = Encoding.UTF8.GetString(Convert.FromBase64String(clientAuthString)).Split(':');
                        var applicationPrincipal = ApplicationServiceContext.Current.GetApplicationProviderService().Authenticate(authComps[0], authComps[1]);
                        claimsPrincipal.AddIdentity(applicationPrincipal.Identity as IClaimsIdentity);
                    }
                }

                AuthenticationContext.Current = new AuthenticationContext(principal); // Set Authentication context
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
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
