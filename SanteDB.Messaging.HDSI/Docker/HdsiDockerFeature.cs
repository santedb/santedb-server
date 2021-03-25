using SanteDB.Core.Configuration;
using SanteDB.Core.Exceptions;
using SanteDB.Docker.Core;
using SanteDB.Messaging.HDSI;
using SanteDB.Rest.Common.Behavior;
using SanteDB.Rest.Common.Configuration;
using SanteDB.Rest.Common.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.Docker
{
    /// <summary>
    /// HDSI docker feature
    /// </summary>
    public class HdsiFeature : IDockerFeature
    {

        /// <summary>
        /// Setting ID for listen address
        /// </summary>
        public const string ListenUriSetting = "LISTEN";
        /// <summary>
        /// Setting ID for CORS enable
        /// </summary>
        public const string CorsSetting = "CORS";
        /// <summary>
        /// Set ID for authentication
        /// </summary>
        public const string AuthenticationSetting = "AUTH";

        /// <summary>
        /// Map the settings to the authentication behavior
        /// </summary>
        private readonly IDictionary<String, Type> authSettings = new Dictionary<String, Type>()
        {
            { "TOKEN", typeof(TokenAuthorizationAccessBehavior) },
            { "BASIC", typeof(BasicAuthorizationAccessBehavior) },
            { "NONE", null }
        };

        /// <summary>
        /// Get the id of the docker feature
        /// </summary>
        public string Id => "HDSI";

        /// <summary>
        /// Get the settings
        /// </summary>
        public IEnumerable<string> Settings => new String[] { AuthenticationSetting, ListenUriSetting, CorsSetting };

        /// <summary>
        /// Create an endpoint config
        /// </summary>
        private RestEndpointConfiguration CreateEndpoint(String endpointUrl) => new Common.Configuration.RestEndpointConfiguration()
        {
            Address = endpointUrl,
            Contract = typeof(IHdsiServiceContract),
            Behaviors = new List<Common.Configuration.RestEndpointBehaviorConfiguration>()
                            {
                                new Common.Configuration.RestEndpointBehaviorConfiguration(typeof(MessageLoggingEndpointBehavior)),
                                new Common.Configuration.RestEndpointBehaviorConfiguration(typeof(MessageCompressionEndpointBehavior)),
                                new Common.Configuration.RestEndpointBehaviorConfiguration(typeof(MessageDispatchFormatterBehavior)),
                                new RestEndpointBehaviorConfiguration(typeof(AcceptLanguageEndpointBehavior))
                            }
        };

        /// <summary>
        /// Configure the feature
        /// </summary>
        public void Configure(SanteDBConfiguration configuration, IDictionary<string, string> settings)
        {
            var restConfiguration = configuration.GetSection<SanteDB.Rest.Common.Configuration.RestConfigurationSection>();
            if (restConfiguration == null)
            {
                throw new ConfigurationException("Error retrieving REST configuration", configuration);
            }

            var hdsiRestConfiguration = restConfiguration.Services.FirstOrDefault(o => o.ServiceType == typeof(IHdsiServiceContract));
            if (hdsiRestConfiguration == null) // add fhir rest config
            {
                hdsiRestConfiguration = new Common.Configuration.RestServiceConfiguration()
                {
                    Behaviors = new List<Common.Configuration.RestServiceBehaviorConfiguration>()
                    {
                        new Common.Configuration.RestServiceBehaviorConfiguration(typeof(TokenAuthorizationAccessBehavior))
                    },
                    Name = "HDSI",
                    Endpoints = new List<Common.Configuration.RestEndpointConfiguration>()
                    {
                        this.CreateEndpoint("http://0.0.0.0:8080/hdsi")
                    }
                };
                restConfiguration.Services.Add(hdsiRestConfiguration);
            }
            

            // Listen address
            if (settings.TryGetValue(ListenUriSetting, out string listen))
            {
                if (!Uri.TryCreate(listen, UriKind.Absolute, out Uri listenUri))
                {
                    throw new ArgumentOutOfRangeException($"{listen} is not a valid URL");
                }

                // Setup the endpoint
                hdsiRestConfiguration.Endpoints.Clear();
                hdsiRestConfiguration.Endpoints.Add(this.CreateEndpoint(listen));
            }

            // Authentication
            if (settings.TryGetValue(AuthenticationSetting, out string auth))
            {
                if (!this.authSettings.TryGetValue(auth.ToUpperInvariant(), out Type authType))
                {
                    throw new ArgumentOutOfRangeException($"Don't understand auth option {auth} allowed values {String.Join(",", this.authSettings.Keys)}");
                }

                // Add behavior
                if (authType != null)
                {
                    hdsiRestConfiguration.Behaviors.Add(new RestServiceBehaviorConfiguration() { Type = authType });
                }
                else
                {
                    hdsiRestConfiguration.Behaviors.RemoveAll(o => this.authSettings.Values.Any(v => v == o.Type));
                }
            }

            // Has the user set CORS?
            if (settings.TryGetValue(CorsSetting, out string cors))
            {
                if (!Boolean.TryParse(cors, out bool enabled))
                {
                    throw new ArgumentOutOfRangeException($"{cors} is not a valid boolean value");
                }

                // Cors is disabled?
                if (!enabled)
                {
                    hdsiRestConfiguration.Endpoints.ForEach(ep => ep.Behaviors.RemoveAll(o => o.Type == typeof(CorsEndpointBehavior)));
                }
                else
                {
                    hdsiRestConfiguration.Endpoints.ForEach(ep => ep.Behaviors.RemoveAll(o => o.Type == typeof(CorsEndpointBehavior)));
                    hdsiRestConfiguration.Endpoints.ForEach(ep => ep.Behaviors.Add(new RestEndpointBehaviorConfiguration()
                    {
                        Type = typeof(CorsEndpointBehavior)
                    }));
                }
            }

            // Add services
            var serviceConfiguration = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders;
            if (!serviceConfiguration.Any(s => s.Type == typeof(HdsiMessageHandler)))
            {
                serviceConfiguration.Add(new TypeReferenceConfiguration(typeof(HdsiMessageHandler)));
            }
        }
    }
}
