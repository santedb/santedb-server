using SanteDB.Authentication.OAuth2.Model;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using System.Collections.Generic;
using System.Security.Authentication;

namespace SanteDB.Authentication.OAuth2.TokenRequestHandlers
{
    public class DefaultPasswordTokenRequestHandler : Abstractions.ITokenRequestHandler
    {
        readonly IPolicyEnforcementService _PolicyEnforcementService;
        readonly IIdentityProviderService _IdentityProvider;
        readonly Tracer _Tracer;

        public DefaultPasswordTokenRequestHandler(IPolicyEnforcementService policyEnforcementService, IIdentityProviderService identityProvider)
        {
            _Tracer = new Tracer(nameof(DefaultPasswordTokenRequestHandler));
            _PolicyEnforcementService = policyEnforcementService;
            _IdentityProvider = identityProvider;

        }

        /// <inheritdoc />
        public IEnumerable<string> SupportedGrantTypes => new[] { OAuthConstants.GrantNamePassword };
        /// <inheritdoc />
        public string ServiceName => "Default Password Token Request Handler";
        /// <inheritdoc />
        public bool HandleRequest(OAuthTokenRequestContext context)
        {
            if (string.IsNullOrEmpty(context?.Username))
            {
                _Tracer.TraceInfo("Missing username in Token request.");
                context.ErrorType = OAuthErrorType.invalid_request;
                context.ErrorMessage = "missing username in the request";
                return false;
                //return CreateErrorCondition(OAuthErrorType.invalid_request, "missing username in the request.");
            }

            _PolicyEnforcementService?.Demand(OAuthConstants.OAuthPasswordFlowPolicy, context.ApplicationPrincipal);

            if (null != context.DevicePrincipal)
            {
                _PolicyEnforcementService?.Demand(OAuthConstants.OAuthPasswordFlowPolicy, context.DevicePrincipal);
            }
            else
            {
                _PolicyEnforcementService?.Demand(OAuthConstants.OAuthPasswordFlowPolicyWithoutDevice, context.ApplicationPrincipal);
            }

            try
            {
                if (!string.IsNullOrEmpty(context.TfaSecret))
                {
                    context.UserPrincipal = _IdentityProvider.Authenticate(context.Username, context.Password, context.TfaSecret) as IClaimsPrincipal;
                }
                else
                {
                    context.UserPrincipal = _IdentityProvider.Authenticate(context.Username, context.Password) as IClaimsPrincipal;
                }
            }
            catch (AuthenticationException authnex)
            {
                _Tracer.TraceWarning("Exception during IIdentityProvider.Authenticate(): {0}", authnex.ToString());
                context.ErrorMessage = authnex.Message;
                context.ErrorType = OAuthErrorType.invalid_grant;
                return false;
            }

            if (null == context.UserPrincipal)
            {
                _Tracer.TraceInfo("Authentication failed in Token request.");
                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "invalid password";
            }

            context.Session = null; //Setting this to null will let the OAuthTokenBehavior establish the session for us.
            return true;
        }
    }
}
