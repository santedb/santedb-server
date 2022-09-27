using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using RestSrvr;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace SanteDB.Authentication.OAuth2.Model
{
    public class OAuthSessionRequestContext : OAuthRequestContextBase
    {
        public OAuthSessionRequestContext(RestOperationContext operationContext) : base(operationContext)
        {
        }

        public OAuthSessionRequestContext(RestOperationContext operationContext, NameValueCollection formFields) : base(operationContext, formFields)
        {
            
        }

        
    }
}
