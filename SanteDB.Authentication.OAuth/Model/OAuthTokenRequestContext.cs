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
using Microsoft.SqlServer.Server;
using RestSrvr;
using SanteDB.Authentication.OAuth2.Configuration;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Principal;

namespace SanteDB.Authentication.OAuth2.Model
{
    /// <summary>
    /// Context class for a token request that is processed by an <see cref="Abstractions.ITokenRequestHandler"/> and handled as part of a flow.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Serialization class
    public class OAuthTokenRequestContext
    {
        ///<summary>Grant type</summary> 
        public string GrantType { get; set; }
        ///<summary>Scope of the grant</summary>
        public List<string> Scopes { get; set; }
        ///<summary>User name when the grant type is password</summary>
        public string UserName { get; set; }
        ///<summary>Password when the grant type is password.</summary>
        public string Password { get; set; }
        /// <summary>
        /// A secret code used as a second factor in an authentication flow.
        /// </summary>
        [Obsolete("Use of this is discouraged.")]
        public string TfaSecret { get; set; }
        ///<summary>Auth code when grant type is Authorization code.</summary>
        public string Code { get; set; }
        /// <summary>
        /// Refreshing token when grant type is refresh_token
        /// </summary>
        public string RefreshToken{ get; set; }
        /// <summary>
        /// The client id of the application. Valid when grant type is client credentials, and others that support multiple
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// The client secret of the application. 
        /// </summary>
        public string ClientSecret { get; set; }
        /// <summary>
        /// The X-Device-Authorization header value if present. This is a custom header in SanteDb as part of a proxy configuration.
        /// </summary>
        public string XDeviceAuthorizationHeader { get; set; }
        /// <summary>
        /// The authenticated device identity.
        /// </summary>
        public IDeviceIdentity DeviceIdentity { get; set; }
        /// <summary>
        /// The authenticated device principal.
        /// </summary>
        public IClaimsPrincipal DevicePrincipal { get; set; }
        /// <summary>
        /// The authenticated application identity.
        /// </summary>
        public IApplicationIdentity ApplicationIdentity { get; set; }
        /// <summary>
        /// The authenticated application principal.
        /// </summary>
        public IClaimsPrincipal ApplicationPrincipal { get; set; }
        /// <summary>
        /// The authenticated user identity.
        /// </summary>
        public IIdentity UserIdentity { get; set; }
        /// <summary>
        /// The authenticated user principal.
        /// </summary>
        public IClaimsPrincipal UserPrincipal { get; set; }
        /// <summary>
        /// Any additional claims that were part of the request. Handlers are free to ignore these additional claims when they do not make sense as part of their request.
        /// </summary>
        public List<IClaim> AdditionalClaims { get; set; }
        /// <summary>
        /// The session that is established as part of this request. Typically, an <see cref="Abstractions.ITokenRequestHandler"/> will set this during processing.
        /// </summary>
        public ISession Session { get; set; }
        /// <summary>
        /// When a request fails, this should contain the type of error that was encountered.
        /// </summary>
        public OAuthErrorType? ErrorType { get; set; }
        /// <summary>
        /// The rest context that this token request is part of.
        /// </summary>
        public RestOperationContext RestContext { get; set; }
        /// <summary>
        /// Shortcut for <c>RestContext.IncomingRequest</c>.
        /// </summary>
        public HttpListenerRequest IncomingRequest => RestContext?.IncomingRequest;
        /// <summary>
        /// Shortcut for <c>RestContext.OutgoingRequest</c>.
        /// </summary>
        public HttpListenerResponse OutgoingResponse => RestContext?.OutgoingResponse;
        /// <summary>
        /// Gets or sets the authentication context at the time the handler is processing a request.
        /// </summary>
        public AuthenticationContext AuthenticationContext { get; set; }
        /// <summary>
        /// When a request fails, this should contain a textual description that will be returned in the response.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The config section that is applicable during processing of the request.
        /// </summary>
        public OAuthConfigurationSection Configuration { get; set; }
    }
}
