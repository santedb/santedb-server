using RestSrvr;
using SanteDB.Authentication.OAuth2.Model;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Authentication.OAuth2.TokenRequestHandlers
{
    public class DefaultRefreshTokenRequestHandler : Abstractions.ITokenRequestHandler
    {
        readonly ISessionTokenResolverService _SessionResolver;
        readonly ISessionIdentityProviderService _SessionIdentityProvider;
        readonly Tracer _Tracer;

        /// <summary>
        /// Constructs a new instance of the class.
        /// </summary>
        /// <param name="sessionResolver">Injected through dependency injection.</param>
        /// <param name="sessionIdentityProvider"></param>
        public DefaultRefreshTokenRequestHandler(ISessionTokenResolverService sessionResolver, ISessionIdentityProviderService sessionIdentityProvider)
        {
            _Tracer = new Tracer(nameof(DefaultRefreshTokenRequestHandler));
            _SessionResolver = sessionResolver;
            _SessionIdentityProvider = sessionIdentityProvider;
        }

        /// <inheritdoc/>
        public IEnumerable<string> SupportedGrantTypes => new[] { OAuthConstants.GrantNameRefresh };

        /// <inheritdoc/>
        public string ServiceName => "Default Refresh Token Request Handler";

        /// <inheritdoc/>
        public bool HandleRequest(OAuthTokenRequestContext context)
        {
            if (string.IsNullOrEmpty(context?.RefreshToken))
            {
                _Tracer.TraceVerbose("Context contains empty refresh token");
                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "missing refresh token";
                return false;
            }

            try
            {

                context.Session = _SessionResolver.ExtendSessionWithRefreshToken(context.RefreshToken);

                var principal = _SessionIdentityProvider.Authenticate(context.Session);

                AuditUtil.AuditSessionStart(context.Session, principal, true);

                if (null == context.Session)
                {
                    _Tracer.TraceInfo("Failed to initialize session from refresh token.");
                    context.ErrorType = OAuthErrorType.invalid_grant;
                    context.ErrorMessage = "invalid refresh token";
                    return false;
                }

                return true;
            }
            catch(SecuritySessionException ex)
            {
                _Tracer.TraceInfo("Failed to initialize session from refresh token. {0}", ex.ToString());

                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "invalid refresh token";
                return false;
            }
        }
    }
}
