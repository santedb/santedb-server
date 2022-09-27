﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Authentication.OAuth2.Model
{
    internal class AuthorizationCode
    {
        /// <summary>
        /// Time <see cref="AuthorizationCode"/> was generated by the server.
        /// </summary>
        public DateTimeOffset iat { get; set; }
        /// <summary>
        /// Device  sid.
        /// </summary>
        public string dev { get; set; }
        /// <summary>
        /// Application or client sid.
        /// </summary>
        public string app { get; set; }
        /// <summary>
        /// User sid.
        /// </summary>
        public string usr { get; set; }
        /// <summary>
        /// Nonce provided in request and returned in token response.
        /// </summary>
        public string nonce { get; set; }
        /// <summary>
        /// Scopes authorized.
        /// </summary>
        public string scp { get; set; }
        /// <summary>
        /// code_verifier parameter for PKCE based flows.
        /// </summary>
        public string cv { get; set; }
        /// <summary>
        /// code_verifier method for PKCE based flows.
        /// </summary>
        public string cvm { get; set; }
    }
}
