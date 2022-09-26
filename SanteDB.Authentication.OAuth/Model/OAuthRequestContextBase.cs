using RestSrvr;
using SanteDB.Authentication.OAuth2.Configuration;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Authentication.OAuth2.Model
{
    /// <summary>
    /// Base class for OAuth requests.
    /// </summary>
    public abstract class OAuthRequestContextBase
    {
        public OAuthRequestContextBase(RestOperationContext operationContext, NameValueCollection formFields)
            : this(operationContext)
        {
            FormFields = formFields;
        }

        public OAuthRequestContextBase(RestOperationContext operationContext)
        {
            OperationContext = operationContext;
        }

        #region Core Properties
        public NameValueCollection FormFields { get; }

        public RestOperationContext OperationContext { get; }

        public HttpListenerRequest IncomingRequest => OperationContext?.IncomingRequest;
        public HttpListenerResponse OutgoingResponse => OperationContext?.OutgoingResponse; 
        #endregion

        #region Request Header Values
        /// <summary>
        /// A secret code used as a second factor in an authentication flow.
        /// </summary>
        [Obsolete("Use of this is discouraged.")]
        public string TfaSecret => IncomingRequest?.Headers?[ExtendedHttpHeaderNames.TfaSecret];
        /// <summary>
        /// The X-Device-Authorization header value if present. This is a custom header in SanteDb as part of a proxy configuration.
        /// </summary>
        public string XDeviceAuthorizationHeader => IncomingRequest?.Headers?[ExtendedHttpHeaderNames.HttpDeviceCredentialHeaderName];
        #endregion

        /// <summary>
        /// When a request fails, this should contain the type of error that was encountered.
        /// </summary>
        public OAuthErrorType? ErrorType { get; set; }

        /// <summary>
        /// When a request fails, this should contain a textual description that will be returned in the response.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The config section that is applicable during processing of the request.
        /// </summary>
        public OAuthConfigurationSection Configuration { get; set; }

        /// <summary>
        /// Gets or sets the authentication context at the time the handler is processing a request.
        /// </summary>
        public AuthenticationContext AuthenticationContext { get; set; }
        /// <summary>
        /// The authenticated device identity.
        /// </summary>
        public IClaimsIdentity DeviceIdentity { get; set; }
        /// <summary>
        /// The authenticated device principal.
        /// </summary>
        public IClaimsPrincipal DevicePrincipal { get; set; }
        /// <summary>
        /// The authenticated application identity.
        /// </summary>
        public IClaimsIdentity ApplicationIdentity { get; set; }
        /// <summary>
        /// The authenticated application principal.
        /// </summary>
        public IClaimsPrincipal ApplicationPrincipal { get; set; }
        /// <summary>
        /// The authenticated user identity.
        /// </summary>
        public IIdentity UserIdentity { get; set; }
        /// <summary>
        /// The authenticated user principal.
        /// </summary>
        public IClaimsPrincipal UserPrincipal { get; set; }

        public virtual string ClientId => null;
        public virtual string ClientSecret => null;
    }
}
