﻿/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * Date: 2017-9-1
 */
using System;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;

namespace SanteDB.Core.Configuration
{
    /// <summary>
    /// SanteDB Security configuration
    /// </summary>
    public class SanteDBSecurityConfiguration 
    {

        /// <summary>
        /// Password regex
        /// </summary>
        public string PasswordRegex { get; set; }

        /// <summary>
        /// Allow unsigned applets to be installed
        /// </summary>
        public bool AllowUnsignedApplets { get; set; }

        /// <summary>
        /// Basic authentication configuration
        /// </summary>
        public SanteDBBasicAuthorization BasicAuth { get; set; }

        /// <summary>
        /// Gets or sets the claims auth
        /// </summary>
        public SanteDBClaimsAuthorization ClaimsAuth { get; set; }

        /// <summary>
        /// Trusted publishers
        /// </summary>
        public ObservableCollection<string> TrustedPublishers { get; set; }

        /// <summary>
        /// Signing certificate
        /// </summary>
        public X509Certificate2 SigningCertificate { get; set; }

        /// <summary>
        /// When using HMAC256 signing this represents the server's secret
        /// </summary>
        public String ServerSigningSecret { get; set; }

        /// <summary>
        /// Raw server key
        /// </summary>
        public byte[] ServerSigningKey { get; internal set; }
    }
}