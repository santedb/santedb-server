using RestSrvr;
using System.Collections.Specialized;

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
