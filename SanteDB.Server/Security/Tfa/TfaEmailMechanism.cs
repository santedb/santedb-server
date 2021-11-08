/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * Date: 2021-8-27
 */

using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Notifications;
using SanteDB.Core.Services;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using SanteDB.Core.Model;
using SanteDB.Core.Security.Claims;
using System.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core;
using SanteDB.Core.Security;
using SanteDB.Server.Core.Services;
using System.Security.Principal;

namespace SanteDB.Server.Security.Tfa.Email
{
    /// <summary>
    /// Represents a TFA mechanism which can send/receive TFA requests via e-mail
    /// </summary>
    public class TfaEmailMechanism : ITfaMechanism
    {
        private ITwoFactorSecretGenerator m_twoFactorSecretGenerator;

        private IIdentityProviderService m_identityProvider;

        private IPasswordHashingService m_hashingProvider;

        /// <summary>
        /// TFA Mechanism via e-mail
        /// </summary>
        public TfaEmailMechanism(ITwoFactorSecretGenerator secretGenerator, IPasswordHashingService passwordHashingService, IIdentityProviderService identityProvider)
        {
            this.m_twoFactorSecretGenerator = secretGenerator;
            this.m_identityProvider = identityProvider;
            this.m_hashingProvider = passwordHashingService;
        }

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(TfaEmailMechanism));

        /// <summary>
        /// Get the identifier for the challenge
        /// </summary>
        public Guid Id
        {
            get
            {
                return Guid.Parse("D919457D-E015-435C-BD35-42E425E2C60C");
            }
        }

        /// <summary>
        /// Gets the name of the mechanism
        /// </summary>
        public string Name
        {
            get
            {
                return "org.santedb.tfa.email";
            }
        }

        /// <summary>
        /// Send the mechanism
        /// </summary>
        public string Send(IIdentity user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            // Gather the e-mail
            var filler = ApplicationServiceContext.Current.GetService<INotificationTemplateFiller>();
            if (filler == null)
                throw new InvalidOperationException("Cannot find notification filler service");

            // We want to send the data in which language?
            String language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            // Generate a TFA secret and add it as a claim on the user
            var secret = this.m_twoFactorSecretGenerator.GenerateTfaSecret();

            if (user is IClaimsIdentity ci)
            {
                // Get user's e-mail
                var email = ci.FindFirst(SanteDBClaimTypes.Email)?.Value;
                if (email == null)
                {
                    throw new InvalidOperationException("E-Mail TFA requires e-mail address registered");
                }

                // Save secret
                this.m_identityProvider.AddClaim(user.Name, new SanteDBClaim(SanteDBClaimTypes.SanteDBOTAuthCode, this.m_hashingProvider.ComputeHash(secret)), AuthenticationContext.SystemPrincipal, new TimeSpan(0, 5, 0));
                // Send
                var template = filler.FillTemplate("tfa.email", language, new
                {
                    user = user,
                    tfa = secret,
                    principal = AuthenticationContext.Current.Principal
                });

                try
                {
                    ApplicationServiceContext.Current.GetService<INotificationService>().Send(new string[] { email }, template.Subject, template.Body, null, false, null);
                    var censoredEmail = email.Split('@')[1];
                    return $"Code sent to *****@{censoredEmail}";
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error, $"Error sending TFA secret: {e.Message}\r\n{e.ToString()}");
                    throw new Exception($"Error dispatching OTP TFA code", e);
                }
            }
            else
            {
                throw new InvalidOperationException("TFA E-mail not compatible with this mechanism");
            }
        }

        /// <summary>
        /// Validate the secret
        /// </summary>
        public bool Validate(IIdentity user, String tfaSecret)
        {
            if (user is IClaimsIdentity ci)
            {
                return ci.FindFirst(SanteDBClaimTypes.SanteDBOTAuthCode)?.Value == this.m_hashingProvider.ComputeHash(tfaSecret);
            }
            else
            {
                // TODO: When a non-CI is provided
                return false;
            }
        }
    }
}