using RestSrvr;
using SanteDB.Authentication.OAuth2.Model;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Authentication.OAuth2.TokenRequestHandlers
{
    public class DefaultClientCredentialsTokenRequestHandler : Abstractions.ITokenRequestHandler
    {
        readonly Tracer _Tracer;
        readonly IPolicyEnforcementService _PolicyEnforcementService;

        public DefaultClientCredentialsTokenRequestHandler(IPolicyEnforcementService policyEnforcementService)
        {
            _Tracer = new Tracer(nameof(DefaultClientCredentialsTokenRequestHandler));
            _PolicyEnforcementService = policyEnforcementService;

            if (null == _PolicyEnforcementService)
            {
                _Tracer.TraceWarning("No policy enforcement service is defined and no policy validation will take place.");
            }
        }

        public IEnumerable<string> SupportedGrantTypes => new[] { OAuthConstants.GrantNameClientCredentials };

        public string ServiceName => "Default Client Credentials Token Request Handler";

        public bool HandleRequest(OAuthTokenRequestContext context)
        {
            if (string.IsNullOrEmpty(context?.ClientId))
            {
                _Tracer.TraceInfo("Missing client id in Token request.");
                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "invalid client_id";
                return false;
                //return this.CreateErrorCondition(OAuthErrorType.invalid_grant, "invalid client id");
            }

            if (null == context.ApplicationPrincipal)
            {
                _Tracer.TraceInfo("Wrong or missing client secret in Token request.");
                context.ErrorType = OAuthErrorType.invalid_client;
                context.ErrorMessage = "invalid client_secret";
                return false;
                //return this.CreateErrorCondition(OAuthErrorType.invalid_client, "invalid client secret");
            }

            _PolicyEnforcementService?.Demand(OAuthConstants.OAuthClientCredentialFlowPolicy);

            if (null != context.DevicePrincipal)
            {
                _PolicyEnforcementService?.Demand(OAuthConstants.OAuthClientCredentialFlowPolicy, context.DevicePrincipal);
            }

            if (null == context.DevicePrincipal && context.Configuration?.AllowClientOnlyGrant != true)
            {
                _Tracer.TraceError("No device principal was authenticated and AllowClientOnlyGrant is not enabled.");
                context.ErrorType = OAuthErrorType.unauthorized_client;
                context.ErrorMessage = $"{OAuthConstants.GrantNameClientCredentials} grant type requires device authentication either using X509 or X-Device-Authorization or enabling the DeviceAuthorizationAccessBehavior in the configuration.";
                return false;
                //return this.CreateErrorCondition(OAuthErrorType.unauthorized_client, $"{OAuthConstants.GrantNameClientCredentials} grant type requires device authentication either using X509 or X-Device-Authorization or enabling the DeviceAuthorizationAccessBehavior in the configuration.");
            }

            context.Session = null; //Setting this to null will let the OAuthTokenBehavior establish the session for us.
            return true;
        }
    }
}
