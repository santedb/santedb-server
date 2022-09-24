using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Authentication.OAuth2.Model
{
    internal class AuthorizationCode
    {
        public DateTimeOffset iat { get; set; }
        public string dev { get; set; }
        public string app { get; set; }
        public string usr { get; set; }
        public string nonce { get; set; }
        public string scp { get; set; }
    }
}
