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
using RestSrvr.Attributes;
using SanteDB.Authentication.OAuth2.Model;
using System.Collections.Specialized;
using System.IO;

namespace SanteDB.Authentication.OAuth2.Rest
{
    /// <summary>
    /// OAuth2.0 Contract
    /// </summary>
    [ServiceContract(Name = "OAuth2")]
    [ServiceProduces("application/json")]
    [ServiceConsumes("application/x-www-form-urlencoded")]
    public interface IOAuthTokenContract 
    {

        /// <summary>
        /// Get discovery exchange 
        /// </summary>
        [Get("/.well-known/openid-configuration")]
        [return: MessageFormat(MessageFormatType.Json)]
        OpenIdConfiguration GetDiscovery();

        /// <summary>
        /// Token request
        /// </summary>
        [Post("oauth2_token")]
        [return: MessageFormat(MessageFormatType.Json)]
        object Token(NameValueCollection formFields);
        
        /// <summary>
        /// Get the session from the authenticated bearer or JWT token
        /// </summary>
        [Get("session")]
        [return: MessageFormat(MessageFormatType.Json)]
        object Session();

        /// <summary>
        /// Gets the user information related to the current session (very similar to the session parameter only this is not an OAUTH response format)
        /// </summary>
        [Get("userinfo")]
        [return: MessageFormat(MessageFormatType.Json)]
        object UserInfo();

        /// <summary>
        /// Post to the authorization handler
        /// </summary>
        [Post("/authorize/{*content}")]
        Stream SelfPost(string content, NameValueCollection authorization);

        /// <summary>
        /// Gets the 
        /// </summary>
        /// <returns></returns>
        [Get("/authorize/{*content}")]
        Stream RenderAsset(string content);

        /// <summary>
        /// Invoke a ping
        /// </summary>
        [RestInvoke(Method = "PING", UriTemplate = "/")]
        void Ping();

        /// <summary>
        /// Get the keys associated with this instance.
        /// </summary>
        /// <returns></returns>
        [Get("jwks")]
        object JsonWebKeySet();

    }
}
