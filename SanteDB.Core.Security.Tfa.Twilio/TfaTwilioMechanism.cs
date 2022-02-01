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
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Security.Tfa.Twilio.Configuration;
using SanteDB.Core.Security.Tfa.Twilio.Resources;
using SanteDB.Core.Services;
using SanteDB.Server.Core.Services;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Linq;
using TW = Twilio;

namespace SanteDB.Core.Security.Tfa.Twilio
{
    /// <summary>
    /// Represents a TFA mechanism that uses TWILIO
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class TfaTwilioMechanism : ITfaMechanism
    {
        // Configuration
        private TwilioTfaMechanismConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<TwilioTfaMechanismConfigurationSection>();

        private Tracer m_tracer = new Tracer("SanteDB.Core.Security.Tfa.Twilio");


        /// <summary>
        /// Identifier of the mechanism
        /// </summary>
        public Guid Id
        {
            get
            {
                return Guid.Parse("08124835-6C24-43C9-8650-9D605F6B5BD6");
            }
        }

        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name
        {
            get
            {
                return Strings.mechanism_name;
            }
        }

        /// <summary>
        /// Send the secret
        /// </summary>
        public String Send(SecurityUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            // First, does this user have a phone number
            string toNumber = user.PhoneNumber;
            if (toNumber == null)
            {
                // Get preferred language for the user
                var securityService = ApplicationServiceContext.Current.GetService<IRepositoryService<UserEntity>>();
                var userEntity = securityService?.Find(o => o.SecurityUserKey == user.Key).FirstOrDefault();
                if (userEntity != null)
                    toNumber = userEntity.Telecoms.FirstOrDefault(o => o.AddressUseKey == TelecomAddressUseKeys.MobileContact)?.Value;
            }

            try
            {
                // Generate a TFA secret and add it as a claim on the user
                var secret = ApplicationServiceContext.Current.GetService<ITwoFactorSecretGenerator>().GenerateTfaSecret();
                ApplicationServiceContext.Current.GetService<IIdentityProviderService>().AddClaim(user.UserName, new SanteDBClaim(SanteDBClaimTypes.SanteDBOTAuthCode, secret), AuthenticationContext.SystemPrincipal, new TimeSpan(0, 5, 0));

                var client = new TW.TwilioRestClient(this.m_configuration.Sid, this.m_configuration.Auth);
                var response = client.SendMessage(this.m_configuration.From, toNumber, String.Format(Strings.default_body, secret));

                if (response.RestException != null)
                    throw new Exception(response.RestException.Message ?? "" + " " + (response.RestException.Code ?? "") + " " + (response.RestException.MoreInfo ?? "") + " " + (response.RestException.Status ?? ""));

                return $"Code sent to ******{user.PhoneNumber.Substring(user.PhoneNumber.Length - 4, 4)}";
            }
            catch (Exception ex)
            {
                this.m_tracer.TraceEvent(EventLevel.Error, "Error sending SMS: {0}", ex);
                throw new Exception($"Could not dispatch SMS code to user", ex);
            }
        }
    }
}