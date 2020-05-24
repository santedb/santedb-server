/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using Newtonsoft.Json;
using System;

namespace SanteDB.Authentication.OAuth2.Model
{
    /// <summary>
    /// OAuth token response
    /// </summary>
    [JsonObject]
    public class OAuthTokenResponse
    {

        /// <summary>
        /// Access token
        /// </summary>
        [JsonProperty("access_token")]
        public String AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the identity token
        /// </summary>
        [JsonProperty("id_token")]
        public String IdentityToken { get; set; }

        /// <summary>
        /// Token type
        /// </summary>
        [JsonProperty("token_type")]
        public String TokenType { get; set; }

        /// <summary>
        /// Expires in
        /// </summary>
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Refresh token
        /// </summary>
        [JsonProperty("refresh_token")]
        public String RefreshToken { get; set; }


    }
}
