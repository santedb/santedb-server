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
using SanteDB.Authentication.OAuth2.Configuration;
using SanteDB.Authentication.OAuth2.Rest;
using SanteDB.Core.Configuration;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Security.Configuration;
using SanteDB.Docker.Core;
using SanteDB.Rest.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Authentication.OAuth2.Docker
{
    /// <summary>
    /// OAUTH feature for docker
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class OauthDockerFeature : IDockerFeature
    {
        /// <summary>
        /// Claims which can be set by the client
        /// </summary>
        public const string ClientClaimSetting = "CLAIMS";
        /// <summary>
        /// Token type
        /// </summary>
        public const string TokenTypeSetting = "TOKEN";
        /// <summary>
        /// Allows insecure client authentication
        /// </summary>
        public const string NodelessClientAuthSetting = "INSECURE_CLIENT_AUTH";
        /// <summary>
        /// Set the issuer
        /// </summary>
        public const string IssuerIdSetting = "ISSUER";
        /// <summary>
        /// Set the signature algorithm
        /// </summary>
        public const string JwtSigningKey = "JWT_SIG_KEY";
        /// <summary>
        /// IDP listen
        /// </summary>
        public const string ListenSetting = "LISTEN";

        /// <summary>
        /// OAuth feature
        /// </summary>
        public string Id => "OPENID";

        /// <summary>
        /// Get the settings
        /// </summary>
        public IEnumerable<string> Settings => new String[]
        {
            ClientClaimSetting ,
            TokenTypeSetting ,
            NodelessClientAuthSetting ,
            IssuerIdSetting ,
            JwtSigningKey,
            ListenSetting
        };

        /// <summary>
        /// Configure 
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="settings"></param>
        public void Configure(SanteDBConfiguration configuration, IDictionary<string, string> settings)
        {

            var oauthConf = configuration.GetSection<OAuthConfigurationSection>();
            if(oauthConf == null)
            {
                oauthConf = DockerFeatureUtils.LoadConfigurationResource<OAuthConfigurationSection>("SanteDB.Authentication.OAuth2.Docker.OauthFeature.xml");
                configuration.AddSection(oauthConf);
            }

            var restConfiguration = configuration.GetSection<SanteDB.Rest.Common.Configuration.RestConfigurationSection>();
            if (restConfiguration == null)
            {
                throw new ConfigurationException("Error retrieving REST configuration", configuration);
            }

            var oauthRestConfiguration = restConfiguration.Services.FirstOrDefault(o => o.ServiceType == typeof(IOAuthServiceContract));
            if (oauthRestConfiguration == null) // add fhir rest config
            {
                oauthRestConfiguration = new RestServiceConfiguration()
                {
                    ConfigurationName = OAuthMessageHandler.ConfigurationName,
                    Endpoints = new List<RestEndpointConfiguration>()
                    {
                        new RestEndpointConfiguration()
                        {
                            Address = "http://0.0.0.0:8080/auth",
                            ContractXml = typeof(IOAuthServiceContract).AssemblyQualifiedName,
                        }
                    }
                };
                restConfiguration.Services.Add(oauthRestConfiguration);
            }

            if(settings.TryGetValue(ListenSetting, out string listenStr))
            {
                if(!Uri.TryCreate(listenStr, UriKind.Absolute, out Uri listenUri))
                {
                    throw new ArgumentException($"{listenStr} is not a valid URI");
                }
                oauthRestConfiguration.Endpoints.ForEach(ep => ep.Address = listenStr);
            }

            // Client claims?
            if (settings.TryGetValue(ClientClaimSetting, out String claim))
            {
                oauthConf.AllowedClientClaims = claim.Split(';').ToList();
            }

            // Token type?
            if(settings.TryGetValue(TokenTypeSetting, out String tokenType))
            {
                oauthConf.TokenType = tokenType;
            }

            // Allow insecure authentication?
            if(settings.TryGetValue(NodelessClientAuthSetting, out string insecureSetting))
            {
                if(!bool.TryParse(insecureSetting, out bool insecureBool))
                {
                    throw new ArgumentOutOfRangeException($"{insecureSetting} is not a valid boolean value");
                }
                oauthConf.AllowClientOnlyGrant = insecureBool;
            }

            // Issuer (used for client claims auth and oauth)
            if(settings.TryGetValue(IssuerIdSetting, out String issuer))
            {
                oauthConf.IssuerName = issuer;
            }

            if(settings.TryGetValue(JwtSigningKey, out String jwtSinging))
            {
                oauthConf.JwtSigningKey = jwtSinging;
            }

            // Add services
            var serviceConfiguration = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders;
            if (!serviceConfiguration.Any(s => s.Type == typeof(OAuthMessageHandler)))
            {
                serviceConfiguration.Add(new TypeReferenceConfiguration(typeof(OAuthMessageHandler)));
            }
        }
    }
}
