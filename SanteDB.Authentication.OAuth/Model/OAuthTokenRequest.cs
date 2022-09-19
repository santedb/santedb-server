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
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;

namespace SanteDB.Authentication.OAuth2.Model
{
    /// <summary>
    /// A token request
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Serialization class
    public class OAuthTokenRequest
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
        /// Assertion
        /// </summary>
        public string Assertion { get; set; }

        /// <summary>
        /// The client id of the application. Valid when grant type is client credentials, and others that support multiple
        /// </summary>
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public string XDeviceAuthorizationHeader { get; set; }

        public IDeviceIdentity DeviceIdentity { get; set; }
        public IClaimsPrincipal DevicePrincipal { get; set; }
        public IApplicationIdentity ApplicationIdentity { get; set; }
        public IClaimsPrincipal ApplicationPrincipal { get; set; }
        public IIdentity UserIdentity { get; set; }
        public IClaimsPrincipal UserPrincipal { get; set; }
        public List<IClaim> AdditionalClaims { get; set; }
    }
}
