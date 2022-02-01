/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */
using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Security;
using System.Security.Authentication;

using System.Text;

namespace SanteDB.Authentication.OAuth2.Wcf
{
    /// <summary>
    /// Basic authorization policy
    /// </summary>
    [DisplayName("OAUTH: HTTP BASIC Client-Credentials")]
    [ExcludeFromCodeCoverage]
    public class ClientAuthorizationAccessBehavior : IServicePolicy, IServiceBehavior
    {

        // Configuration from main SanteDB
        private ApplicationServiceContextConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<ApplicationServiceContextConfigurationSection>();

        // Trace source
        private Tracer m_traceSource = new Tracer(OAuthConstants.TraceSourceName);

        /// <summary>
        /// Apply the policy to the request
        /// </summary>
        public void Apply(RestRequestMessage request)
        {
            try
            {
                this.m_traceSource.TraceInfo("Entering OAuth BasicAuthorizationAccessPolicy");

                // Role service
                var identityService = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>();

                var httpRequest = RestOperationContext.Current.IncomingRequest;

                var authHeader = httpRequest.Headers["Authorization"];
                if(String.IsNullOrEmpty(authHeader) ||
                    !authHeader.ToLowerInvariant().StartsWith("basic"))
                    throw new AuthenticationException("Invalid authentication scheme");
                authHeader = authHeader.Substring(6);
                var b64Data = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader)).Split(':');
                if (b64Data.Length != 2)
                    throw new SecurityException("Malformed HTTP Basic Header");

                var principal = identityService.Authenticate(b64Data[0], b64Data[1]);
                if (principal == null)
                    throw new AuthenticationException("Invalid client credentials");

                // Client secret
                RestOperationContext.Current.Data.Add("symm_secret", b64Data[1]);

                // If the current principal is set-up then add the identity if not then don't
                if(AuthenticationContext.Current.Principal == AuthenticationContext.AnonymousPrincipal)
                {
                    var contextToken = AuthenticationContext.EnterContext(principal);
                    RestOperationContext.Current.Disposed += (o, e) => contextToken.Dispose();
                }
                else
                {
                    (AuthenticationContext.Current.Principal as IClaimsPrincipal).AddIdentity(principal.Identity);
                }

                // Disposed context so reset the auth
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error,  e.ToString());
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
