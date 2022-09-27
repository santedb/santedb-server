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
    public interface IOAuthServiceContract 
    {

        /// <summary>
        /// Get discovery exchange 
        /// </summary>
        [Get("/.well-known/openid-configuration")]
        [return: MessageFormat(MessageFormatType.Json)]
        OpenIdConfiguration Discovery();

        /// <summary>
        /// OAuth2 Token Endpoint. Issues access tokens and/or id tokens and accepts a grant from the resource owner.
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
        /// OAuth2 Authorization Endpoint.
        /// </summary>
        /// <returns></returns>
        [Get("authorize")]
        [ServiceFault(400, typeof(OAuthError), "Request Parameters are invalid.")]
        [UrlParameter(OAuthConstants.FormField_ClientId, typeof(string), "The client id of the client which will accept this authorization grant.", Required = true)]
        [UrlParameter("redirect_uri", typeof(string), "The URI which responses should be redirected to. For response_mode of form_post, the action which the form post is sent.", Required = false)]
        [UrlParameter("response_type", typeof(string), "The type of response expected from the authorization service. Default is (code).", Required = false)]
        [UrlParameter("response_mode", typeof(string), "How the authorization server will return a response. Valid values are (query, fragment, form_post). Default is query for response_type code, and fragment for response_type id_token.", Required = false)]
        [UrlParameter("scope", typeof(string), "Space separated list of scopes to be included in the authorization. Default is *", Required = false)]
        [UrlParameter("login_hint", typeof(string), "When present, the authorization server will pre-populate the username with this value.", Required = false)]
        [UrlParameter("state", typeof(string), "State value that is returned with the response from the authorization server.", Required = false)]
        [UrlParameter("nonce", typeof(string), "Number ONCE that is returned when the authorization code is exchanged by the token service.", Required = false)]
        [return: MessageFormat (MessageFormatType.Json)]
        object Authorize();

        /// <summary>
        /// OAuth2 Authorization Endpoint.
        /// </summary>
        /// <param name="formFields"></param>
        /// <returns></returns>
        [Post("authorize")]
        [ServiceFault(400, typeof(OAuthError), "Request Parameters are invalid.")]
        [UrlParameter(OAuthConstants.FormField_ClientId, typeof(string), "The client id of the client which will accept this authorization grant.", Required = true)]
        [UrlParameter("redirect_uri", typeof(string), "The URI which responses should be redirected to. For response_mode of form_post, the action which the form post is sent.", Required = false)]
        [UrlParameter("response_type", typeof(string), "The type of response expected from the authorization service. Default is (code).", Required = false)]
        [UrlParameter("response_mode", typeof(string), "How the authorization server will return a response. Valid values are (query, fragment, form_post). Default is query for response_type code, and fragment for response_type id_token.", Required = false)]
        [UrlParameter("scope", typeof(string), "Space separated list of scopes to be included in the authorization. Default is *", Required = false)]
        [UrlParameter("login_hint", typeof(string), "When present, the authorization server will pre-populate the username with this value.", Required = false)]
        [UrlParameter("state", typeof(string), "State value that is returned with the response from the authorization server.", Required = false)]
        [UrlParameter("nonce", typeof(string), "Number ONCE that is returned when the authorization code is exchanged by the token service.", Required = false)]
        [return: MessageFormat(MessageFormatType.Json)]
        object Authorize_Post(NameValueCollection formFields);

        

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
        [return: MessageFormat(MessageFormatType.Json)]
        object JsonWebKeySet();

        /// <summary>
        /// Gets an asset that is used by the authorization service.
        /// </summary>
        /// <returns></returns>
        [Get("/{*assetPath}")]
        [return: MessageFormat(MessageFormatType.Json)]
        Stream Content(string assetPath);

    }
}
