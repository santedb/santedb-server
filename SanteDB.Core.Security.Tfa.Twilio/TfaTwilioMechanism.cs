/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using SanteDB.Core.Diagnostics;
using SanteDB.Core.i18n;
using SanteDB.Core.Notifications;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Security.Tfa.Twilio.Configuration;
using SanteDB.Core.Services;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Security.Principal;
using TW = Twilio;

// TODO: Re-Implement this to be a generic message sender
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

        public const string TFA_TEMPLATE_ID = "org.santedb.notifications.mfa.sms";

        private readonly Tracer m_tracer = new Tracer("SanteDB.Core.Security.Tfa.Twilio");
        private readonly ITfaCodeProvider m_CodeProvider;
        private readonly IPasswordHashingService m_passwordHasher;
        private readonly INotificationTemplateFiller m_templateFiller;

        /// <summary>
        /// DI constructor
        /// </summary>
        public TfaTwilioMechanism(ITfaCodeProvider codeProvider, IPasswordHashingService hashingService, 
            INotificationTemplateFiller templateFiller)
        {
            this.m_CodeProvider = codeProvider;
            this.m_passwordHasher = hashingService;
            this.m_templateFiller = templateFiller;
        }

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
                return "SMS";
            }
        }

        /// <summary>
        /// Send the secret
        /// </summary>
        public String Send(IIdentity user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            else if (user is IClaimsIdentity icid)
            {
                try
                {
                    // Generate a TFA secret and add it as a claim on the user
                    var secret = this.m_CodeProvider.GenerateTfaCode(user);
                    //ApplicationServiceContext.Current.GetService<IIdentityProviderService>().AddClaim(user.Name, new SanteDBClaim(SanteDBClaimTypes.SanteDBOTAuthCode, secret), AuthenticationContext.SystemPrincipal, new TimeSpan(0, 5, 0));
                    //TW.TwilioClient.Init(this.m_configuration.Sid, this.m_configuration.Auth);

                    var toNumber = icid.FindFirst(SanteDBClaimTypes.Telephone).Value;
                    var message = this.m_templateFiller.FillTemplate(TFA_TEMPLATE_ID, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, new { code = secret });
                    var response = TW.Rest.Api.V2010.Account.MessageResource.Create(
                        body: message.Body,
                        from: new TW.Types.PhoneNumber(this.m_configuration.From),
                        to: new TW.Types.PhoneNumber(toNumber));

                    if (response.ErrorCode.HasValue)
                    {
                        throw new Exception(response.ErrorMessage ?? "" + " " + (response.ErrorCode ?? 0));
                    }

                    return $"Code sent to ******{toNumber.Substring(toNumber.Length - 4, 4)}";
                }
                catch (Exception ex)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error, "Error sending SMS: {0}", ex);
                    throw new Exception($"Could not dispatch SMS code to user", ex);
                }
            }
            else
            {
                throw new InvalidOperationException(String.Format(ErrorMessages.ARGUMENT_INVALID_TYPE, typeof(IClaimsIdentity), user.GetType()));
            }
        }

        /// <summary>
        /// Validate the secret
        /// </summary>
        public bool Validate(IIdentity user, String tfaSecret)
        {
            return m_CodeProvider.VerifyTfaCode(user, tfaSecret, DateTimeOffset.UtcNow);
        }
    }
}