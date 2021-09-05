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
using SanteDB.Core.Security;

namespace SanteDB.Authentication.OAuth2
{
    /// <summary>
    /// OAuth constants
    /// </summary>
    public static class OAuthConstants
    {

        /// <summary>
        /// ACS trace source name
        /// </summary>
        public const string TraceSourceName = "SanteDB.Authentication.OAuth2";


        /// <summary>
        /// Grant name for the authorization code
        /// </summary>
        public const string GrantNameReset = "x_challenge";

        /// <summary>
        /// Grant name for the authorization code
        /// </summary>
        public const string GrantNameAuthorizationCode = "authorization_code";

        /// <summary>
        /// Grant name for password grant
        /// </summary>
        public const string GrantNamePassword = "password";

        /// <summary>
        /// Grant name for password grant
        /// </summary>
        public const string GrantNameRefresh = "refresh_token";

        /// <summary>
        /// Grant name for client credentials
        /// </summary>
        public const string GrantNameClientCredentials = "client_credentials";
        
        /// <summary>
        /// JWT token type
        /// </summary>
        public const string JwtTokenType = "urn:ietf:params:oauth:token-type:jwt";

        /// <summary>
        /// Bearer token type
        /// </summary>
        public const string BearerTokenType = "bearer";

        /// <summary>
        /// Configuration section name
        /// </summary>
        public const string ConfigurationName = "santedb.authentication.oauth2";

        /// <summary>
        /// TFA header name
        /// </summary>
        public const string TfaHeaderName = "X-SanteDB-TfaSecret";

        /// <summary>
        /// Gets the client credential policy
        /// </summary>
        public const string OAuthLoginPolicy = PermissionPolicyIdentifiers.LoginAsService + ".0";

        /// <summary>
        /// Client credentials policy
        /// </summary>
        public const string OAuthClientCredentialFlowPolicy = OAuthLoginPolicy + ".1";

        /// <summary>
        /// Password credentials policy
        /// </summary>
        public const string OAuthPasswordFlowPolicy = OAuthLoginPolicy + ".2";

        /// <summary>
        /// Code token policy
        /// </summary>
        public const string OAuthCodeFlowPolicy = OAuthLoginPolicy + ".3";

        /// <summary>
        /// Code token policy
        /// </summary>
        public const string OAuthResetFlowPolicy = OAuthLoginPolicy + ".4";
    }
}
