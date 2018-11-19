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
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.Services.Policy;
using MARC.HI.EHRS.SVC.Core.Services.Security;
using SanteDB.Core.Configuration;
using SanteDB.Core.Security.Claims;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Claims;
using System.IdentityModel.Policy;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Security.Principal;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core;
using System.ServiceModel;
using SanteDB.Core.Services;
using SanteDB.Core.Security;
using MARC.HI.EHRS.SVC.Core.Wcf;
using RestSrvr;
using RestSrvr.Message;
using MARC.HI.EHRS.SVC.Core.Exceptions;

namespace SanteDB.Core.Rest.Security
{
    /// <summary>
    /// Basic authorization policy
    /// </summary>
    [AuthenticationSchemeDescription(AuthenticationScheme.Basic)]
    public class BasicAuthorizationAccessBehavior : IServicePolicy, IServiceBehavior
    {

        // Configuration from main SanteDB
        private SanteDBConfiguration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection(SanteDBConstants.SanteDBConfigurationName) as SanteDBConfiguration;

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
                var roleService = ApplicationContext.Current.GetService<IRoleProviderService>();
                var identityService = ApplicationContext.Current.GetService<IIdentityProviderService>();
                var pipService = ApplicationContext.Current.GetService<IPolicyInformationService>();

                var httpRequest = RestOperationContext.Current.IncomingRequest;

                var authHeader = httpRequest.Headers["Authorization"];
                if(String.IsNullOrEmpty(authHeader) ||
                    !authHeader.ToLowerInvariant().StartsWith("basic"))
                    throw new UnauthorizedRequestException("Invalid authentication scheme", "BASIC", this.m_configuration.Security.ClaimsAuth.Realm, this.m_configuration.Security.BasicAuth.Realm);
                authHeader = authHeader.Substring(6);
                var b64Data = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader)).Split(':');
                if (b64Data.Length != 2)
                    throw new SecurityException("Malformed HTTP Basic Header");

                var principal = identityService.Authenticate(b64Data[0], b64Data[1]);
                if (principal == null)
                    throw new UnauthorizedRequestException("Invalid username/password", "Basic", this.m_configuration.Security.BasicAuth.Realm, null);

                // Add claims made by the client
                var claims = new List<System.Security.Claims.Claim>();
                if (principal is ClaimsPrincipal)
                    claims.AddRange((principal as ClaimsPrincipal).Claims);

                var clientClaims = SanteDBClaimTypes.ExtractClaims(httpRequest.Headers);
                foreach (var claim in clientClaims)
                {
                    if (this.m_configuration?.Security?.BasicAuth?.AllowedClientClaims?.Contains(claim.Type) == false)
                        throw new SecurityException(ApplicationContext.Current.GetLocaleString("SECE001"));
                    else
                    {
                        var handler = SanteDBClaimTypes.GetHandler(claim.Type);
                        if (handler == null ||
                            handler.Validate(principal, claim.Value))
                            claims.Add(claim);
                        else
                            throw new SecurityException(ApplicationContext.Current.GetLocaleString("SECE002"));
                    }
                }

                // Claim headers built in
                if (pipService != null)
                    claims.AddRange(pipService.GetActivePolicies(principal).Where(o => o.Rule == PolicyDecisionOutcomeType.Grant).Select(o => new System.Security.Claims.Claim(SanteDBClaimTypes.SanteDBGrantedPolicyClaim, o.Policy.Oid)));

                // Finally validate the client 
                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(principal.Identity, claims));

                if (this.m_configuration?.Security?.BasicAuth?.RequireClientAuth == true)
                {
                    var clientAuth = httpRequest.Headers[SanteDBConstants.BasicHttpClientCredentialHeaderName];
                    if (clientAuth == null ||
                        !clientAuth.StartsWith("basic", StringComparison.InvariantCultureIgnoreCase))
                        throw new SecurityException("Client credentials invalid");
                    else
                    {
                        String clientAuthString = clientAuth.Substring(clientAuth.IndexOf("basic", StringComparison.InvariantCultureIgnoreCase) + 5).Trim();
                        String[] authComps = Encoding.UTF8.GetString(Convert.FromBase64String(clientAuthString)).Split(':');
                        var applicationPrincipal = ApplicationContext.Current.GetApplicationProviderService().Authenticate(authComps[0], authComps[1]);
                        claimsPrincipal.AddIdentity(applicationPrincipal.Identity as ClaimsIdentity);
                    }
                }

                AuthenticationContext.Current = new AuthenticationContext(principal); // Set Authentication context
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
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
