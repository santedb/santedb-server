using RestSrvr;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
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
    internal class OAuthAuthorizeRequestContext : OAuthRequestContextBase
    {
        public OAuthAuthorizeRequestContext(RestOperationContext operationContext, NameValueCollection formFields)
             : base(operationContext, formFields)
        {
        }

        public OAuthAuthorizeRequestContext(RestOperationContext operationContext) : base(operationContext)
        {
        }

        private string GetValue(string key) => null == FormFields ? IncomingRequest?.QueryString?[key] : FormFields[key];


        public override string ClientId => GetValue("client_id");
        public string LoginHint => GetValue("login_hint");
        public string Nonce => GetValue("nonce");
        public string Scope => GetValue("scope");
        public string Prompt => GetValue("prompt");
        public string State => GetValue("state");
        public string ResponseType => GetValue("response_type");

        private string _ResponseMode;
        public string ResponseMode
        {
            get
            {
                return _ResponseMode ?? GetValue("response_mode");
            }
            set
            {
                _ResponseMode = value;
            }
        }
        public string RedirectUri => GetValue("redirect_uri");

        public string Username => FormFields?["username"];
        public string Password => FormFields?["password"];
                
        
        /// <summary>
        /// The code that was generated for the response.
        /// </summary>
        public string Code { get; set; }
    }
}
