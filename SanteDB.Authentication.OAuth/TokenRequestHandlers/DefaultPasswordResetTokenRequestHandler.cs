using SanteDB.Authentication.OAuth2.Model;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Text;

namespace SanteDB.Authentication.OAuth2.TokenRequestHandlers
{
    /// <summary>
    /// Default implementation of the x_challenge grant token request handler
    /// </summary>
    public class DefaultPasswordResetTokenRequestHandler : Abstractions.ITokenRequestHandler
    {
        readonly Tracer _Tracer;
        readonly IPolicyEnforcementService _PolicyService;
        readonly ISecurityChallengeIdentityService _SecurityChallengeService;

        public DefaultPasswordResetTokenRequestHandler(IPolicyEnforcementService policyService, ISecurityChallengeIdentityService securityChallengeService)
        {
            _Tracer = new Tracer(nameof(DefaultPasswordResetTokenRequestHandler));
            _PolicyService = policyService;
            _SecurityChallengeService = securityChallengeService;
        }   

        public IEnumerable<string> SupportedGrantTypes => new[] { OAuthConstants.GrantNameReset };

        public string ServiceName => "Default Password Reset Token Request Handler";

        public bool HandleRequest(OAuthTokenRequestContext context)
        {
            //Validate the request.
            if (string.IsNullOrEmpty(context?.Username))
            {
                context.ErrorType = OAuthErrorType.invalid_request;
                context.ErrorMessage = "invalid username";
                return false;
            }

            if (string.IsNullOrEmpty(context?.SecurityChallenge) || !Guid.TryParse(context?.SecurityChallenge, out var securitychallengeguid))
            {
                context.ErrorType = OAuthErrorType.invalid_request;
                context.ErrorMessage = "invalid challenge";
                return false;
            }

            if (string.IsNullOrEmpty(context?.SecurityChallengeResponse))
            {
                context.ErrorType = OAuthErrorType.invalid_request;
                context.ErrorMessage = "invalid response";
                return false;
            }

            //Forcibly set the scope to be login only. This prevents the user from making any modifications to the system after they have updated the password.
            context.Scopes = context.Scopes ?? new List<string>();
            context.Scopes.Clear();
            context.Scopes.Add(PermissionPolicyIdentifiers.LoginPasswordOnly);

            _PolicyService?.Demand(OAuthConstants.OAuthResetFlowPolicy, context.ApplicationPrincipal);
            
            if (null != context.DevicePrincipal)
            {
                _PolicyService?.Demand(OAuthConstants.OAuthResetFlowPolicy, context.DevicePrincipal);
            }
            else
            {
                _PolicyService?.Demand(OAuthConstants.OAuthResetFlowPolicyWithoutDevice, context.ApplicationPrincipal);
            }

            try
            {
                context.UserPrincipal = _SecurityChallengeService.Authenticate(context.Username, securitychallengeguid, context.SecurityChallengeResponse, context.TfaSecret) as IClaimsPrincipal;
            }
            catch (AuthenticationException authnex)
            {
                _Tracer.TraceWarning("Exception during ISecurityChallengeIdentityService.Authenticate(): {0}", authnex.ToString());
                context.ErrorMessage = authnex.Message;
                context.ErrorType = OAuthErrorType.invalid_grant;
                return false;
            }

            if (null == context.UserPrincipal)
            {
                _Tracer.TraceInfo("Authentication failed in Token request.");
                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "invalid challenge response";
                return false;
            }

            context.Session = null; //Setting this to null will let the OAuthTokenBehavior establish the session for us.
            return true;

        }

    }
}
