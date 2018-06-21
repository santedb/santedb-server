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
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Linq;
using SanteDB.Core.Security;

namespace SanteDB.Authentication.OAuth2.Configuration
{
    /// <summary>
    /// Configuration section handler
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// <SanteDB.authentication.oauth2>
    ///     <token expiry="" issuerName="" type="jwt|bearer"/>
    ///     <claims>
    ///         <add claimType="claimName"/>
    ///     </claims>
    ///     <scopes>
    ///         <add name="scopeName"/>
    ///     </scopes>
    /// </SanteDB.authentication.oauth2>
    /// ]]>
    /// </remarks>
    public class ConfigurationSectionHandler : IConfigurationSectionHandler
    {
        /// <summary>
        /// Create the configuration handler
        /// </summary>
        public object Create(object parent, object configContext, XmlNode section)
        {
            OAuthConfiguration retVal = new OAuthConfiguration();

            // Nodes
            XmlElement tokenElement = section.SelectSingleNode("./token") as XmlElement;
            XmlNodeList claims = section.SelectNodes("./claims/add/@claimType"),
                scopes = section.SelectNodes("./scopes/add/@name") ;

            retVal.ValidityTime = TimeSpan.Parse(tokenElement?.Attributes["expiry"]?.Value ?? "00:00:10:00");
            retVal.IssuerName = tokenElement?.Attributes["issuerName"]?.Value ?? "http://localhost/oauth2_token";
            retVal.TokenType = tokenElement?.Attributes["tokenType"]?.Value ?? OAuthConstants.JwtTokenType;
            // Claims
            if (claims != null)
                foreach (XmlNode itm in claims)
                    retVal.AllowedClientClaims.Add(itm.Value);
            if (scopes != null)
                foreach (XmlNode itm in scopes)
                    retVal.AllowedScopes.Add(itm.Value);

            return retVal;

        }
    }
}
