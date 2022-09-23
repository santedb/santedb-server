﻿/*
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
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Security.Principal;

namespace SanteDB.Authentication.OAuth2.Model
{
    /// <summary>
    /// Context class for a token request that is processed by an <see cref="Abstractions.ITokenRequestHandler"/> and handled as part of a flow.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Serialization class
    public class OAuthTokenRequestContext : OAuthRequestContextBase
    {

        public OAuthTokenRequestContext(RestOperationContext operationContext) : base(operationContext)
        {

        }

        public OAuthTokenRequestContext(RestOperationContext operationContext, NameValueCollection formFields)
            : base(operationContext, formFields)
        {

        }


        #region Form Field Values
        ///<summary>Grant type</summary> 
        public string GrantType => FormFields?[OAuthConstants.FormField_GrantType]?.Trim()?.ToLowerInvariant();
        ///<summary>User name when the grant type is password</summary>
        public string UserName => FormFields?[OAuthConstants.FormField_Username];
        ///<summary>Password when the grant type is password.</summary>
        public string Password => FormFields?[OAuthConstants.FormField_Password];
        /// <summary>
        /// The client id of the application. Valid when grant type is client credentials, and others that support multiple
        /// </summary>
        public override string ClientId => FormFields?[OAuthConstants.FormField_ClientId];
        /// <summary>
        /// The client secret of the application. 
        /// </summary>
        public override string ClientSecret => FormFields?[OAuthConstants.FormField_ClientSecret];
        /// <summary>
        /// Refreshing token when grant type is refresh_token
        /// </summary>
        public string RefreshToken => FormFields?[OAuthConstants.FormField_RefreshToken];
        ///<summary>Auth code when grant type is Authorization code.</summary>
        public string AuthorizationCode => FormFields?[OAuthConstants.FormField_AuthorizationCode]; 
        #endregion

        ///<summary>Scope of the grant</summary>
        public List<string> Scopes { get; set; }
        
        /// <summary>
        /// Any additional claims that were part of the request. Handlers are free to ignore these additional claims when they do not make sense as part of their request.
        /// </summary>
        public List<IClaim> AdditionalClaims { get; set; }
        /// <summary>
        /// The session that is established as part of this request. Typically, an <see cref="Abstractions.ITokenRequestHandler"/> will set this during processing.
        /// </summary>
        public ISession Session { get; set; }
        

        
    }
}
